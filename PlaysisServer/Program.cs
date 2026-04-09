using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Essencial_Repos;
using PlaysisServer.Objects;
using PlaysisServer.PacketModels;
using System.Collections;
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
            { "limitUploadMB", "20" }
        };
        public static Hashtable Config = new();
        public static List<NetPeer> ConnectedPeers = new();
        public static List<PlaysisRoom> Rooms = new();
        public static PlaysisRoom PublicHall = new(0, "Public Hall", null);
        public static Dictionary<NetPeer, Dictionary<string, object>> LoggedInUsers = new();
        public class AppInfo
        {
            public const string serverVersion = "v.1.0.0.0";
            public const string availableClientVersion = "v.1.0.0.0";
            public const int protocolLevel = 7;
        }
        

        static void Main(string[] args)
        {
            Log.SaveLog("欢迎使用ImgHorizon.Playsis服务端。");
            if (!Directory.Exists("./Properties/"))
            {
                Directory.CreateDirectory("./Properties/");
            }
            Config = PropertiesHelper.AutoCheck(htStandard, ConfigPath);
            var listener = new EventBasedNetListener();
            var server = new NetManager(listener);
            server.ChannelsCount = 3;
            server.Start(1270);
            Rooms.Add(PublicHall);
            Log.SaveLog("网络服务端已在1270端口上开启。");

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
                        case CommonObjects.OpCode.Auth:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.Auth(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
                            }
                            break;
                        case CommonObjects.OpCode.PlayerSpawn:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.PlayerSpawn(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
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
                        case CommonObjects.OpCode.FetchModelInfo:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.FetchModelInfo(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
                            }
                            break;
                        case CommonObjects.OpCode.CheckUidWhetherAdmin:
                            try
                            {
                                peer.SendWithDeliveryEvent(Models.CheckUidWhetherAdmin(peer, reader, requestId), DeliveryMethod.ReliableOrdered, null);
                            }
                            catch (Exception ex)
                            {
                                Log.SaveLog(ex.ToString());
                                //throw;
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
                    ((PlayerObject)LoggedInUsers[peer]["playerObj"]).Room.Players.Remove((PlayerObject)LoggedInUsers[peer]["playerObj"]);
                    
                }
                catch (Exception ex)
                {
                    Log.SaveLog(ex.ToString());
                }
                
            };

            while (true)
            {
                server.PollEvents();
                Thread.Sleep(15);
            }

        }

    }
}
