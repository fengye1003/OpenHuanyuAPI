using LiteNetLib;
using LiteNetLib.Utils;

namespace PlaysisServer
{
    internal class Program
    {
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
            ApiAvailabilityAuth
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var listener = new EventBasedNetListener();
            var server = new NetManager(listener);
            server.Start(1270);

            listener.ConnectionRequestEvent += request =>
            {
                request.AcceptIfKey("TekoDotIO.ImgHorizon.Playsis.ProtocolType");
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
