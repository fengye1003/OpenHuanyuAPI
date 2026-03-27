using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public static NetDataWriter Auth(NetPeer peer, NetPacketReader reader)
        {
            var writer = new NetDataWriter();
            var uid = reader.GetInt();
            var passwordhash = reader.GetLargeString();
            HttpClient client = new();
            var resultTask = client.GetStringAsync((string)Program.Config["huanyuApiHost"]!+$"/PlaysisService/Login?uid={uid}&passwordhash={passwordhash}");
            string result = resultTask.Result;
            if (result == "NotFoundUID" || result == "InvalidPasswordHash")
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
    }
}
