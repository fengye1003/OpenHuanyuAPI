using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TencentCloud.Bsca.V20210811.Models;
using PlaysisServer.Essencial_Repos;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public static NetDataWriter Auth(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);
            var uid = reader.GetString();
            var passwordhash = reader.GetString();
            HttpClient client = new();
            var url = (string)Program.Config["huanyuApiHost"]! + $"/PlaysisService/Login?uid={uid}&passwordhash={passwordhash}";
            var response = client.GetAsync(url);
            var content = response.Result.Content.ReadAsStringAsync();
            string result = content.Result;
            if (result == "NotFoundUID" || result == "InvalidPasswordHash" || !response.Result.IsSuccessStatusCode)
            {
                writer.Put(0);
                switch (result)
                {
                    case "NotFoundUID":
                        writer.Put(0);
                        break;
                    case "InvalidPasswordHash":
                        writer.Put(1);
                        break;
                    default:
                        writer.Put(2);
                        break;
                }
            }
            else
            {
                Program.LoggedInUsers.Add(peer, new()
                {
                    { "secret", result },
                    { "uid", uid }
                });
                writer.Put(1);
                writer.Put(result);
            }
            return writer;
        }


        public static NetDataWriter AuthBySecret(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);

            var uid = reader.GetString();
            var secret = reader.GetString();

            HttpClient client = new();
            var url = (string)Program.Config["huanyuApiHost"]! + $"/PlaysisService/IsValidSecret?uid={uid}&secret={secret}";
            var response = client.GetAsync(url);
            var content = response.Result.Content.ReadAsStringAsync();
            string result = content.Result;

            Log.SaveLog($"[Auth] AuthBySecret uid={uid} api={result}");
            if (result != "1" || !response.Result.IsSuccessStatusCode)
            {
                writer.Put(0);
                return writer;
            }

            if (Program.LoggedInUsers.ContainsKey(peer)) Program.LoggedInUsers.Remove(peer);
            Program.LoggedInUsers.Add(peer, new()
            {
                { "secret", secret },
                { "uid", uid }
            });
            writer.Put(1);
            return writer;
        }
        public static NetDataWriter CheckUidWhetherAdmin(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);
            //Receive Stage
            var uid = reader.GetString();
            if (!Program.LoggedInUsers.TryGetValue(peer, out var userInfo))
            {
                writer.Put(0);
                Program.ConnectedPeers.Remove(peer);
                peer.Disconnect();
                return writer;
            }
            else if ((string)userInfo["uid"] != uid)
            {
                writer.Put(0);
                return writer;
            }
            writer.Put(1);

            var secret = reader.GetString();
            if (ApiIsValidAdmin(uid, secret))
            {
                writer.Put(1);
            }
            else
            {
                writer.Put(0);
            }
            
            return writer;
        }

        public static NetDataWriter AdminRegister(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);
            //Receive Stage
            var uid = reader.GetString();
            if (!Program.LoggedInUsers.TryGetValue(peer, out var userInfo))
            {
                writer.Put(0);
                Program.ConnectedPeers.Remove(peer);
                peer.Disconnect();
                return writer;
            }
            else if ((string)userInfo["uid"] != uid)
            {
                writer.Put(0);
                return writer;
            }
            writer.Put(1);

            var secret = reader.GetString();
            if (ApiIsValidAdmin(uid, secret))
            {
                var regUID = reader.GetString();
                var regUsername = reader.GetString();
                var regPasswordHash = reader.GetString();
                var registerResult = ApiRegisterByAdmin(
                    uid, 
                    secret, 
                    regUID, 
                    regUsername, 
                    regPasswordHash);
                if (registerResult.Success)
                {
                    writer.Put(1);
                }
                else
                {
                    writer.Put(0);
                    writer.Put(registerResult.Info);
                }
            }
            else
            {
                writer.Put(0);
                writer.Put("Invalid admin account access.");
            }
            return writer;
        }


        public static bool ApiIsValidAdmin(string uid, string secret)
        {
            HttpClient client = new();
            var url = (string)Program.Config["huanyuApiHost"]! + $"/PlaysisService/IsValidSecret?uid={uid}&secret={secret}";
            var response = client.GetAsync(url);
            var content = response.Result.Content.ReadAsStringAsync();
            string result = content.Result;
            if (result == "0" || result == "InternalError" || !response.Result.IsSuccessStatusCode)
            {
                return false;
            }
            else
            {
                url = (string)Program.Config["huanyuApiHost"]! + $"/PlaysisService/IsAdmin?uid={uid}";
                response = client.GetAsync(url);
                content = response.Result.Content.ReadAsStringAsync();
                result = content.Result;
                if (result == "0" || !response.Result.IsSuccessStatusCode)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public static AuthApiResult ApiRegisterByAdmin(string uid, string secret, string regUID, string regUsername, string regPasswordHash)
        {
            HttpClient client = new();
            string url = (string)Program.Config["huanyuApiHost"]! + 
                $"/PlaysisService/RegisterByAdmin?uid={uid}" +
                $"&secret={secret}" +
                $"&regUsername={regUsername}" +
                $"&regUID={regUID}" +
                $"&regPasswordHash={regPasswordHash}";
            var response = client.GetAsync(url);
            var content = response.Result.Content.ReadAsStringAsync();
            var result = content.Result;
            if (result == "InvalidOperation" || result == "InternalError" || !response.Result.IsSuccessStatusCode)
            {
                return new(false, result);
            }
            else
            {
                return new(true, null);
            }
        }

        public class AuthApiResult
        {
            public bool Success { get; set; }
            public string? Info { get; set; }

            public AuthApiResult(bool success, string? info)
            {
                Success = success;
                Info = info;
            }
        }
    
        public static async Task AuthAsync(NetPeer peer, string uid, string passwordhash, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);

            string result = "";
            bool ok = false;
            try
            {
                var url = (string)Program.Config["huanyuApiHost"]! + $"/PlaysisService/Login?uid={uid}&passwordhash={passwordhash}";
                using var resp = await Program.Http.GetAsync(url).ConfigureAwait(false);
                result = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                ok = resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Log.SaveLog("AuthAsync HTTP 失败: " + ex.GetType().Name + " " + ex.Message);
            }

            if (!ok || result == "NotFoundUID" || result == "InvalidPasswordHash")
            {
                writer.Put(0);
                writer.Put(result == "NotFoundUID" ? 0 : result == "InvalidPasswordHash" ? 1 : 2);
            }
            else
            {
                lock (Program.LoggedInUsers)
                {
                    Program.LoggedInUsers[peer] = new()
                    {
                        { "secret", result },
                        { "uid", uid },
                        { "sessionStart", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                    };
                }
                writer.Put(1);
                writer.Put(result);
                PlaysisServer.Essentials.UsageCollector.Record(uid, 0, "login");
            }

            Program.PendingSends.Enqueue(() =>
            {
                if (peer.ConnectionState == ConnectionState.Connected)
                    peer.SendWithDeliveryEvent(writer, DeliveryMethod.ReliableOrdered, null);
            });
        }

        public static async Task AuthBySecretAsync(NetPeer peer, string uid, string secret, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);

            string result = "";
            bool ok = false;
            try
            {
                var url = (string)Program.Config["huanyuApiHost"]! + $"/PlaysisService/IsValidSecret?uid={uid}&secret={secret}";
                using var resp = await Program.Http.GetAsync(url).ConfigureAwait(false);
                result = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                ok = resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Log.SaveLog("AuthBySecretAsync HTTP 失败: " + ex.GetType().Name + " " + ex.Message);
            }

            if (!ok || result != "1")
            {
                writer.Put(0);
            }
            else
            {
                lock (Program.LoggedInUsers)
                {
                    Program.LoggedInUsers[peer] = new()
                    {
                        { "secret", secret },
                        { "uid", uid }
                    };
                }
                writer.Put(1);
            }

            Program.PendingSends.Enqueue(() =>
            {
                if (peer.ConnectionState == ConnectionState.Connected)
                    peer.SendWithDeliveryEvent(writer, DeliveryMethod.ReliableOrdered, null);
            });
        }
    
        public static async Task CheckUidWhetherAdminAsync(NetPeer peer, string uid, string secret, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);

            Dictionary<string, object>? info = null;
            lock (Program.LoggedInUsers) Program.LoggedInUsers.TryGetValue(peer, out info);

            if (info == null || (string)info["uid"] != uid)
            {
                writer.Put(0);
                Program.PendingSends.Enqueue(() =>
                {
                    if (peer.ConnectionState == ConnectionState.Connected)
                        peer.SendWithDeliveryEvent(writer, DeliveryMethod.ReliableOrdered, null);
                });
                return;
            }

            writer.Put(1);

            var isAdmin = false;
            try
            {
                var baseUrl = (string)Program.Config["huanyuApiHost"]!;
                var valid = await Program.Http.GetStringAsync(
                    $"{baseUrl}/PlaysisService/IsValidSecret?uid={uid}&secret={secret}").ConfigureAwait(false);

                if (valid != "0" && valid != "InternalError")
                {
                    var admin = await Program.Http.GetStringAsync(
                        $"{baseUrl}/PlaysisService/IsAdmin?uid={uid}").ConfigureAwait(false);
                    isAdmin = admin != "0";
                }
            }
            catch (Exception ex)
            {
                Log.SaveLog("CheckUidWhetherAdminAsync HTTP 失败: " + ex.GetType().Name + " " + ex.Message);
            }

            writer.Put(isAdmin ? 1 : 0);

            Program.PendingSends.Enqueue(() =>
            {
                if (peer.ConnectionState == ConnectionState.Connected)
                    peer.SendWithDeliveryEvent(writer, DeliveryMethod.ReliableOrdered, null);
            });
        }
    }}











