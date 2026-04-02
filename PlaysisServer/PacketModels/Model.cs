using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Essencial_Repos;
using PlaysisServer.Essentials;
using PlaysisServer.Objects;
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

        public static NetDataWriter SpawnModel(NetPeer peer, NetPacketReader reader)
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
            var location = CommonObjects.GetVector3(reader);
            var scale = CommonObjects.GetVector3(reader);
            var rotation = CommonObjects.GetVector3(reader);
            if (File.Exists($"./ModelStorage/{hash}")) 
            {
                ModelObject model = new((PlayerObject)userInfo["playerObj"], 
                    ((PlayerObject)userInfo["playerObj"]).Name!,
                    File.ReadAllText($"./ModelStorage/{hash}").Split("\n")[1],
                    hash);
                model.Location = location;
                model.Scale = scale;
                model.Rotation = rotation;
                var existed = ((PlayerObject)userInfo["playerObj"]).Room.Models.TryGetValue(uid, out var value);
                if (!existed)
                    ((PlayerObject)userInfo["playerObj"]).Room.Models.Add(uid, model);
                else
                    value = model;
                writer.Put(1);
            }
            else
                writer.Put(0);
            return writer;
        }
        
        public static NetDataWriter FetchModelInfo(NetPeer peer, NetPacketReader reader)
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
            if (File.Exists($"./ModelStorage/{hash}"))
            {
                writer.Put(1);
                var content = File.ReadAllLines($"./ModelStorage/{hash}");
                var modelOwnerUid = Convert.ToInt32((content[0]));
                var url = content[1];
                HttpClient client = new();
                var api = (string)Program.Config["huanyuApiHost"]! + $"/PlaysisService/GetUsername?uid={uid}";
                var response = client.GetAsync(api);
                var resContent = response.Result.Content.ReadAsStringAsync();
                string result = resContent.Result;
                var name = result;
                writer.Put(modelOwnerUid);
                writer.Put(name);
                writer.Put(url);
            }
            else
                writer.Put(0);
            return writer;
            
        }
    }

}
