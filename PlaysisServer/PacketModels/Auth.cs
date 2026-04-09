using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TencentCloud.Bsca.V20210811.Models;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public static NetDataWriter Auth(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);
            var uid = reader.GetInt();
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

        public static NetDataWriter CheckUidWhetherAdmin(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);
            //Receive Stage
            var uid = reader.GetInt();
            if (!Program.LoggedInUsers.TryGetValue(peer, out var userInfo))
            {
                writer.Put(0);
                Program.ConnectedPeers.Remove(peer);
                peer.Disconnect();
                return writer;
            }
            else if ((int)userInfo["uid"] != uid)
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
            var uid = reader.GetInt();
            if (!Program.LoggedInUsers.TryGetValue(peer, out var userInfo))
            {
                writer.Put(0);
                Program.ConnectedPeers.Remove(peer);
                peer.Disconnect();
                return writer;
            }
            else if ((int)userInfo["uid"] != uid)
            {
                writer.Put(0);
                return writer;
            }
            writer.Put(1);

            var secret = reader.GetString();
            if (ApiIsValidAdmin(uid, secret))
            {
                var regUID = reader.GetInt();
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


        public static bool ApiIsValidAdmin(int uid, string secret)
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

        public static AuthApiResult ApiRegisterByAdmin(int uid, string secret, int regUID, string regUsername, string regPasswordHash)
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
    }
}
