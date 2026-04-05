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
