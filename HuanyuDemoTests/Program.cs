using LiteNetLib;
using LiteNetLib.Utils;

namespace HuanyuDemoTests
{
    internal class Program
    {
        public enum OpCode : byte
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
            Console.WriteLine("这是一个Playsis客户端测试程序。");
            var listener = new EventBasedNetListener();
            var client = new NetManager(listener);
            client.Start();
            client.Connect("127.0.0.1", 1270, "TekoDotIO.ImgHorizon.Playsis.ProtocolType");

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("已连接到服务器");

                var writer = new NetDataWriter();
                writer.Put((byte)OpCode.ApiAvailabilityAuth);
                writer.Put(0);

                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                
            };

            listener.NetworkReceiveEvent += (peer, reader, channel, deliverymode) =>
            {
                var a = reader.GetInt();
                Console.WriteLine(a);
            };

            while (true)
            {
                Thread.Sleep(1000);
                client.PollEvents();
            }
        }
    }
}
