using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Essencial_Repos;
using System.Collections;
using PlaysisServer.PacketModels;
using PlaysisServer.Objects;

namespace PlaysisServer
{
    internal class Program
    {
        const string ConfigPath = "./Properties/ServerConfig.properties";
        static Hashtable htStandard = new()
        {
            { "type", "Playsis.ServerConfig" },
            { "huanyuApiHost", "0.0.0.0:5003" },
        };
        public static Hashtable Config = new();
        public static List<NetPeer> ConnectedPeers = new();
        public static List<PlaysisRoom> Rooms = new();
        public static PlaysisRoom PublicHall = new(0, "Public Hall");
        public static Dictionary<NetPeer, Dictionary<string, object>> LoggedInUsers = new();
        public class AppInfo
        {
            public const string serverVersion = "v.1.0.0.0";
            public const string availableClientVersion = "v.1.0.0.0";
            public const int protocolLevel = 0;
        }
        

        static void Main(string[] args)
        {
            Log.SaveLog("欢迎使用ImgHorizon.Playsis服务端。");
            Config = PropertiesHelper.AutoCheck(htStandard, ConfigPath);
            var listener = new EventBasedNetListener();
            var server = new NetManager(listener);
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
                var op = (CommonObjects.OpCode)reader.GetByte();

                switch (op)
                {
                    case CommonObjects.OpCode.ApiAvailabilityAuth:

                        peer.SendWithDeliveryEvent(Models.ApiAvailabilityAuth(peer, reader), DeliveryMethod.ReliableUnordered, null);
                        Log.SaveLog("Sent");
                        break;
                    case CommonObjects.OpCode.Auth:
                        peer.SendWithDeliveryEvent(Models.Auth(peer, reader), DeliveryMethod.ReliableOrdered, null);
                        break;
                    case CommonObjects.OpCode.PlayerSpawn:
                        peer.SendWithDeliveryEvent(Models.PlayerSpawn(peer, reader), DeliveryMethod.ReliableOrdered, null);
                        break;
                    case CommonObjects.OpCode.PlayerMove:
                        break;
                }
                if (!ConnectedPeers.Contains(peer))
                {
                    peer.Disconnect();
                }
                reader.Recycle();
            });

            listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {

            };

            while (true)
            {
                server.PollEvents();
                Thread.Sleep(15);
            }

        }

    }
}
