using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Essencial_Repos;
using PlaysisServer.Essentials;
using PlaysisServer.Objects;
using PlaysisServer.PacketModels;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Diagnostics;

namespace PlaysisServer
{
    internal class Program
    {
        const string ConfigPath = "./Properties/ServerConfig.properties";
        static Hashtable htStandard = new()
        {
            { "type", "Playsis.ServerConfig" },
            { "huanyuApiHost", "0.0.0.0:5003" },
            { "tencentApiSecretId", "null" },
            { "tencentApiSecretKey", "null" },
            { "bucket", "null" },
            { "appId", "null" },
            { "region", "null" },
            { "expireTime", "1800" },
            { "limitUploadMB", "20" },
            { "maintainHallOnReload", "true" },
            { "hallSavePath", "./publichall.json" }
        };
        public static Hashtable Config = new();
        public static List<NetPeer> ConnectedPeers = new();
        public static readonly ConcurrentQueue<Action> PendingSends = new();
        public static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };
        public static List<PlaysisRoom> Rooms = new();
        public static string? LastError = null;
        public static bool AutoPlaceModels = false;
        public static float MainCityHalfSize = 10f;
        public static float ModelGroundOffset = 0.02f;

        public static Essentials.KeepOutRect MainCityRect()
        {
            return new Essentials.KeepOutRect(-MainCityHalfSize, -MainCityHalfSize, MainCityHalfSize, MainCityHalfSize);
        }
        public static PlaysisRoom PublicHall = new(0, "Public Hall", null);
        public static Dictionary<NetPeer, Dictionary<string, object>> LoggedInUsers = new();
        public static bool DoMaintainHallOnReload = true;
        public static string HallSavePath = "./publichall.json";

        public class AppInfo
        {
            public const string serverVersion = "v.1.0.1.0";
            public const string availableClientVersion = "v.1.1.0.0";
            public const int protocolLevel = 9;
        }
        

        static void Main(string[] args)
        {
            Log.SaveLog("欢迎使用ImgHorizon.Playsis服务端。");
            if (!Directory.Exists("./Properties/"))
            {
                Directory.CreateDirectory("./Properties/");
            }
            Config = PropertiesHelper.AutoCheck(htStandard, ConfigPath);
            try
            {
                DoMaintainHallOnReload = (string)Config["maintainHallOnReload"]! == "true";
                HallSavePath = (string)Config["hallSavePath"]!;
            }
            catch (Exception ex)
            {
                Log.SaveLog(ex.ToString());
                throw;
            }

            if (DoMaintainHallOnReload)
            {
                try
                {
                    Program.PublicHall = PublicHallHelper.Load();
                    Log.SaveLog("从本地恢复了存档。");
                }
                catch (Exception ex)
                {
                    Log.SaveLog($"从本地恢复存档时出现问题，重新创建房间。异常：{ex}");
                }
            }
            
            var listener = new EventBasedNetListener();
            var server = new NetManager(listener);
            server.ChannelsCount = 3;
            server.DisconnectTimeout = 30000;
            server.Start(1270);
            Rooms.Add(PublicHall);
            AnnotationStore.Load(PublicHall);
            ScoreStore.Load(PublicHall);

            foreach (var record in RoomStore.Load())
            {
                var owner = string.IsNullOrEmpty(record.OwnerUid) ? null : new PlayerObject(record.OwnerUid, null, null);
                var retentionDays = 7;
                var rawDays = Program.Config["roomRetentionDays"] as string;
                if (!string.IsNullOrEmpty(rawDays) && int.TryParse(rawDays, out var d) && d > 0) retentionDays = d;

                var ageDays = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - record.CreatedAt) / 86400000.0;
                if (ageDays > retentionDays)
                {
                    Log.SaveLog($"房间 #{record.RoomID} {record.Name} 已超过 {retentionDays} 天，自动清理");
                    continue;
                }

                var restored = new PlaysisRoom(record.RoomID, record.Name, owner, record.Description);
                Rooms.Add(restored);
                Models.RegisterCode(record.RoomID, record.Code);
                Log.SaveLog($"已恢复房间 #{record.RoomID} {record.Name}（邀请码 {record.Code}）");
            }
            AutoPlaceModels = (Program.Config["autoPlaceModels"] as string) == "true";
            var rawCity = Program.Config["mainCitySize"] as string;
            if (!string.IsNullOrEmpty(rawCity) && float.TryParse(rawCity, out var citySize) && citySize > 0f)
                MainCityHalfSize = citySize * 0.5f;

            var rawOffset = Program.Config["modelGroundOffset"] as string;
            if (!string.IsNullOrEmpty(rawOffset) && float.TryParse(rawOffset, out var groundOffset) && groundOffset >= 0f)
                ModelGroundOffset = groundOffset;
            UsageCollector.Init((Program.Config["usageCollectEnabled"] as string) == "true",
                                Program.Config["usageAnonSecret"] as string ?? "");

            var gcDaysRaw = Program.Config["storageGcDays"] as string;
            if (int.TryParse(gcDaysRaw, out var gcDays) && gcDays > 0)
            {
                RunStorageGc(gcDays);
            }
            Log.SaveLog("网络服务端已在1270端口上开启。");
            StartStatusServer(1271);

            listener.ConnectionRequestEvent += request =>
            {
                var peer = request.AcceptIfKey("TekoDotIO.ImgHorizon.Playsis.ProtocolType");
                Log.SaveLog($"有新的客户端{peer.Address}:{peer.Port}传入连接！");
                if (peer != null)
                {
                    ConnectedPeers.Add(peer);
                    Log.SaveLog($"已为新的连入客户端{peer.Address}:{peer.Port}注册到库。");
                }
            };

            listener.NetworkReceiveEvent += ((peer, reader, channel, deliveryMethod) =>
            {
                
                if (true)
                {
                    var requestId = reader.GetInt();
            var debugOp = reader.PeekByte();
            if (debugOp == 8 || debugOp == 19 || debugOp == 2 || debugOp == 6 || debugOp == 10 || debugOp == 1 || debugOp == 13 || debugOp == 12)
                Log.SaveLog($"[Op] {debugOp} rid={requestId}");
                    var op = (CommonObjects.OpCode)reader.GetByte();
                    //if (op != CommonObjects.OpCode.SyncRoomPlayerState && op != CommonObjects.OpCode.PlayerMove) Console.WriteLine((int)op);
                    //Log.SaveLog(((int)op).ToString());
                    switch (op)
                    {
                        case CommonObjects.OpCode.ReSyncPackets:
                            try
                            {
                                
                                NetDataWriter writer = new();
                                writer.Put(reader.GetInt());
                                peer.SendWithDeliveryEvent(writer, DeliveryMethod.ReliableOrdered, null);
                                //Log.SaveLog("Sent");
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
                            }
                            break;
                        case CommonObjects.OpCode.ApiAvailabilityAuth:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.ApiAvailabilityAuth(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                                //Log.SaveLog("Sent");
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
                            }
                            break;
                        case CommonObjects.OpCode.AuthBySecret:
                            try
                            {
                                var sUid = reader.GetString();
                                var sTok = reader.GetString();
                                var sPeer = peer;
                                var sId = requestId;
                                _ = Task.Run(() => Models.AuthBySecretAsync(sPeer, sUid, sTok, sId));
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.Auth:
                            try
                            {
                                var aUid = reader.GetString();
                                var aHash = reader.GetString();
                                var aPeer = peer;
                                var aId = requestId;
                                _ = Task.Run(() => Models.AuthAsync(aPeer, aUid, aHash, aId));
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.PlayerSpawn:
                            try
                            {
                                var spUid = reader.GetString();
                                var spLoc = CommonObjects.GetVector3(reader);
                                var spScale = CommonObjects.GetVector3(reader);
                                var spRot = CommonObjects.GetVector3(reader);
                                var spPeer = peer;
                                var spId = requestId;
                                _ = Task.Run(() => Models.PlayerSpawnAsync(spPeer, spUid, spLoc, spScale, spRot, spId));
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.PlayerMove:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.PlayerMove(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
                            }
                            break;
                        case CommonObjects.OpCode.SyncRoomPlayerState:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.SyncRoomStatus(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
                            }
                            break;
                        case CommonObjects.OpCode.SyncRoomModelsState:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.SyncRoomModelStatus(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
                            }
                            break;
                        case CommonObjects.OpCode.GetPlayerNameByUid:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.GetPlayerNameByUid(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
                            }
                            break;
                        case CommonObjects.OpCode.RequestUploadModel:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.RequestUploadModel(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
                            }
                            break;
                        case CommonObjects.OpCode.AddModelUrl:
                            try
                            {
                                // Log.SaveLog("111");
                                peer.SendWithDeliveryEvent(Models.SendAddModelUrl(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
                            }
                            break;
                        case CommonObjects.OpCode.FetchRoomList:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.FetchRoomList(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.SwitchRoom:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.SwitchRoom(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.FetchModelInfo:
                            try
                            {
                                var mUid = reader.GetString();
                                var mHash = reader.GetString();
                                var mPeer = peer;
                                var mId = requestId;
                                _ = Task.Run(() => Models.FetchModelInfoAsync(mPeer, mUid, mHash, mId));
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.CheckUidWhetherAdmin:
                            try
                            {
                                var adUid = reader.GetString();
                                var adSecret = reader.GetString();
                                var adPeer = peer;
                                var adId = requestId;
                                _ = Task.Run(() => Models.CheckUidWhetherAdminAsync(adPeer, adUid, adSecret, adId));
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.RoomCreate:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.RoomCreate(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.RoomDelete:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.RoomDelete(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.RoomJoinByCode:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.RoomJoinByCode(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.KickPlayer:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.KickPlayer(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.RemoveModel:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.RemoveModel(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.SetScore:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.ScoreSync(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.AnnotationSync:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.AnnotationSync(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                            }
                            break;
                        case CommonObjects.OpCode.AdminRegister:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.AdminRegister(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
                            }
                            break;
                    }
                    if (!ConnectedPeers.Contains(peer))
                    {
                        peer.Disconnect();
                    }
                    
                }
                reader.Recycle();
            });

            listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                try
                {
                    Log.SaveLog(disconnectInfo.Reason.ToString());
                    ConnectedPeers.Remove(peer);
                    lock (LoggedInUsers)
                    {
                        if (LoggedInUsers.TryGetValue(peer, out var session))
                        {
                            var sessionUid = (string)session["uid"];
                            var dwell = 0;
                            if (session.TryGetValue("sessionStart", out var startObj) && startObj is long startMs)
                                dwell = (int)Math.Max(0, (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startMs) / 1000);

                            UsageCollector.Record(sessionUid, 0, "disconnect", dwellSeconds: dwell);

                            if (session.TryGetValue("playerObj", out var po) && po is PlayerObject player)
                                player.Room?.Players.Remove(player);

                            LoggedInUsers.Remove(peer);
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    Log.SaveLog(ex.ToString());
                }
                
            };

            var lastPing = DateTime.UtcNow;
            while (true)
            {
                server.PollEvents();
                while (PendingSends.TryDequeue(out var send))
                {
                    try { send(); } catch (Exception ex) { Log.SaveLog(ex.ToString()); }
                }

                if ((DateTime.UtcNow - lastPing).TotalSeconds >= 5)
                {
                    lastPing = DateTime.UtcNow;
                    List<NetPeer> snapshot;
                    lock (ConnectedPeers) snapshot = new List<NetPeer>(ConnectedPeers);
                    foreach (var cp in snapshot) SendPush(cp, CommonObjects.OpCode.ServerPing, "");
                }
                Thread.Sleep(15);
            }

        }

    
        static void StartStatusServer(int port)
        {
            var listener = new System.Net.HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            try { listener.Start(); }
            catch (Exception ex) { Log.SaveLog("状态端口启动失败: " + ex.Message); return; }

            var thread = new Thread(() =>
            {
                var started = DateTime.UtcNow;
                while (listener.IsListening)
                {
                    HttpListenerContext ctx;
                    try { ctx = listener.GetContext(); }
                    catch { break; }

                        if (ctx.Request.Url.AbsolutePath.StartsWith("/__models/"))
                        {
                            HandleModelFile(ctx);
                            continue;
                        }


                    try
                    {
                        int peers, rooms, models;
                        lock (LoggedInUsers) peers = LoggedInUsers.Count;
                        lock (Rooms) rooms = Rooms.Count;
                        models = Rooms.Sum(r => r.Models.Count);

                        int storageFiles = 0;
                        try { if (Directory.Exists("./ModelStorage")) storageFiles = Directory.GetFiles("./ModelStorage").Length; } catch { }

                        var build = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(typeof(Program).Assembly)?.InformationalVersion ?? "unknown";

                        var json = $"{{\"ok\":true,\"build\":\"{build}\",\"peers\":{peers},\"rooms\":{rooms},\"models\":{models},\"storageFiles\":{storageFiles}," +
                                   $"\"uptimeSeconds\":{(int)(DateTime.UtcNow - started).TotalSeconds}," +
                                   $"\"lastError\":{System.Text.Json.JsonSerializer.Serialize(LastError ?? "")}}}";

                        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                        ctx.Response.ContentType = "application/json; charset=utf-8";
                        ctx.Response.ContentLength64 = bytes.Length;
                        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                        ctx.Response.OutputStream.Close();
                    }
                    catch (Exception ex)
                    {
                        Log.SaveLog("状态响应失败: " + ex.Message);
                    }
                }
            }) { IsBackground = true };
            thread.Start();
            Log.SaveLog($"状态端口已在 127.0.0.1:{port} 上开启。");
        }
    
        public static void SendPush(NetPeer peer, CommonObjects.OpCode op, string uid, NetDataWriter? payload = null)
        {
            if (peer == null || peer.ConnectionState != ConnectionState.Connected) return;

            var w = new NetDataWriter();
            w.Put(0);
            w.Put((byte)op);
            w.Put(uid);
            if (payload != null) w.Put(payload.CopyData());

            PendingSends.Enqueue(() =>
            {
                if (peer.ConnectionState == ConnectionState.Connected)
                    peer.SendWithDeliveryEvent(w, DeliveryMethod.ReliableOrdered, null);
            });
        }
    
        public static void PlaysisRoomChanged()
        {
            List<NetPeer> peers;
            lock (LoggedInUsers) peers = new List<NetPeer>(LoggedInUsers.Keys);
            foreach (var p in peers) SendPush(p, CommonObjects.OpCode.RoomListChanged, "");
        }
    
        public static void RunStorageGc(int minAgeDays)
        {
            var referenced = new List<string>();
            lock (Rooms)
            {
                foreach (var room in Rooms)
                    lock (room)
                        foreach (var model in room.Models.Values)
                            referenced.Add(model.ModelHash);
            }

            try
            {
                var hallPath = Program.Config["hallSavePath"] as string;
                if (!string.IsNullOrEmpty(hallPath) && System.IO.File.Exists(hallPath))
                {
                    var saves = System.Text.Json.JsonSerializer.Deserialize<List<Objects.PlaysisRoomSave>>(System.IO.File.ReadAllText(hallPath));
                    if (saves != null)
                        foreach (var save in saves)
                            foreach (var m in save.Models)
                                if (!string.IsNullOrEmpty(m.ModelHash)) referenced.Add(m.ModelHash);
                }
            }
            catch (Exception ex)
            {
                Log.SaveLog("读取大厅存档用于存储清理失败（本次不清理存档引用）: " + ex.Message);
            }

            var (scanned, removed, kept) = Essentials.StorageJanitor.Run(referenced, minAgeDays);
            Log.SaveLog($"模型存储清理：扫描 {scanned}，删除 {removed}，保留 {kept}（最小年龄 {minAgeDays} 天）");
        }
    
        static void HandleModelFile(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var name = ctx.Request.Url.AbsolutePath.Substring("/__models/".Length);
                name = System.IO.Path.GetFileName(name);

                if (string.IsNullOrEmpty(name) || name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.Close();
                    return;
                }

                var dir = "./ModelStorage/uploads";
                System.IO.Directory.CreateDirectory(dir);
                var path = System.IO.Path.Combine(dir, name + ".zip");

                if (ctx.Request.HttpMethod == "PUT" || ctx.Request.HttpMethod == "POST")
                {
                    using (var fs = System.IO.File.Create(path))
                        ctx.Request.InputStream.CopyTo(fs);

                    Log.SaveLog($"本地存储：已接收模型 {name}（{new System.IO.FileInfo(path).Length} 字节）");
                    ctx.Response.StatusCode = 200;
                    ctx.Response.Close();
                    return;
                }

                if (ctx.Request.HttpMethod == "GET")
                {
                    if (!System.IO.File.Exists(path))
                    {
                        ctx.Response.StatusCode = 404;
                        ctx.Response.Close();
                        return;
                    }

                    ctx.Response.ContentType = "application/octet-stream";
                    ctx.Response.ContentLength64 = new System.IO.FileInfo(path).Length;
                    using (var fs = System.IO.File.OpenRead(path))
                        fs.CopyTo(ctx.Response.OutputStream);

                    ctx.Response.OutputStream.Close();
                    return;
                }

                ctx.Response.StatusCode = 405;
                ctx.Response.Close();
            }
            catch (Exception ex)
            {
                Log.SaveLog("本地模型存储失败: " + ex);
                try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
            }
        }
    }}

























