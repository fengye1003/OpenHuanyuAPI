using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Essencial_Repos;
using PlaysisServer.Essentials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public static NetDataWriter RequestUploadModel(NetPeer peer, NetPacketReader reader)
        {
            //Log.SaveLog("Receive request.");
            var writer = new NetDataWriter();

            // Receive Stage
            var uid = reader.GetInt();
            if (!Program.LoggedInUsers.TryGetValue(peer, out var userInfo))
            {
                writer.Put(0);
                Program.ConnectedPeers.Remove(peer);
                return writer;
            }
            else if ((int)userInfo["uid"] != uid)
            {
                writer.Put(0);
                return writer;
            }
            writer.Put(1);

            var hash = reader.GetString();//hash
            TencentCOSHelper helper = new();
            helper.filename = hash;
            var url = helper.GetPresignedUrl(hash);
            writer.Put(url);
            //Log.SaveLog(url);
            return writer;
        }

        public static NetDataWriter SendAddModelUrl(NetPeer peer, NetPacketReader reader)
        {
            //Log.SaveLog("Receive request.");
            var writer = new NetDataWriter();

            // Receive Stage
            var uid = reader.GetInt();
            if (!Program.LoggedInUsers.TryGetValue(peer, out var userInfo))
            {
                writer.Put(0);
                Program.ConnectedPeers.Remove(peer);
                return writer;
            }
            else if ((int)userInfo["uid"] != uid)
            {
                writer.Put(0);
                return writer;
            }
            writer.Put(1);

            var hash = reader.GetString();
            var url = reader.GetString();

            if (!Directory.Exists("./ModelStorage/"))
            {
                Directory.CreateDirectory("./ModelStorage/");
            }
            if (!File.Exists($"./ModelStorage/{hash}"))
            {
                File.Create($"./ModelStorage/{hash}").Close();
            }
            File.WriteAllText($"./ModelStorage/{hash}", $"{uid}\n{url}");
            Log.SaveLog($"成功存储模型{hash}");
            return writer;
        }
    }
}
