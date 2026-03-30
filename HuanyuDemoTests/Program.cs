using LiteNetLib;
using LiteNetLib.Utils;
using System.Diagnostics;
using System.Numerics;

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

        public static Vector3 GetVector3(NetPacketReader reader)
        {
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();
            return new Vector3(x, y, z);
        }
        public static void PutVector3(NetDataWriter writer, Vector3 value)
        {
            writer.Put(value.X);
            writer.Put(value.Y);
            writer.Put(value.Z);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("这是一个Playsis客户端测试程序。");
            var listener = new EventBasedNetListener();
            var client = new NetManager(listener);
            client.Start();
            client.Connect("45.145.229.7", 1270, "TekoDotIO.ImgHorizon.Playsis.ProtocolType");

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("已连接到服务器");

                var writer = new NetDataWriter();
                writer.Put((byte)OpCode.Auth);
                writer.Put(0);
                writer.Put("passwd");
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                
            };

            listener.NetworkErrorEvent += (endPoint, socketError) =>
            {
                Console.WriteLine($"NetworkError: {socketError}");
            };

            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                Console.WriteLine($"Disconnected: {info.Reason}");
            };

            listener.NetworkReceiveEvent += (peer, reader, channel, deliverymode) =>
            {
                try
                {
                    var a = reader.GetInt();
                    Console.WriteLine(a);
                    if (a == 1)
                    {

                        var b = reader.GetString();

                        Console.WriteLine(b);
                        var writer = new NetDataWriter();
                        writer.Put((byte)OpCode.PlayerSpawn);
                        writer.Put(0);
                        PutVector3(writer, new Vector3(0, 1.6f, 0));
                        PutVector3(writer, new Vector3(0, 0, 0));
                        PutVector3(writer, new Vector3(0, 0, 0));
                        peer.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                    else
                    {
                        a = reader.GetInt();
                        Console.WriteLine(a);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                
            };

            while (true)
            {
                Thread.Sleep(1000);
                client.PollEvents();
            }
        }
    }
}
