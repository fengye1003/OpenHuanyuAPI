using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Essencial_Repos;

namespace PlaysisServer
{
    internal class Program
    {
        static List<NetPeer> ConnectedPeers = new();
        sealed class AppInfo
        {
            public const string serverVersion = "v.1.0.0.0";
            public const string availableClientVersion = "v.1.0.0.0";
            public const int protocolLevel = 0;
        }
        enum OpCode : byte
        {
            JoinRoom = 1,
            PlayerSpawn,
            PlayerMove,
            SpawnModel,
            SyncRoomState,
            Auth,
            ApiAvailabilityAuth,
            FetchModelUpload
        }

        static void Main(string[] args)
        {
            Log.SaveLog("欢迎使用ImgHorizon.Playsis服务端。");
            var listener = new EventBasedNetListener();
            var server = new NetManager(listener);
            server.Start(1270);
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
                var op = (OpCode)reader.GetByte();

                switch (op)
                {
                    case OpCode.ApiAvailabilityAuth:
                        ApiAvailabilityAuth(peer, reader);
                        break;
                }

                reader.Recycle();
            });

            static void ApiAvailabilityAuth(NetPeer peer, NetPacketReader reader)
            {
                var writer = new NetDataWriter();
                var ver = reader.GetInt();
                if (ver != AppInfo.protocolLevel)
                {
                    writer.Put(0);
                }
                else
                {
                    writer.Put(1);
                }
            }

        }

    }
}
