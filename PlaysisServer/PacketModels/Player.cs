using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public static NetDataWriter PlayerSpawn(NetPeer peer, NetPacketReader reader)
        {
            var writer = new NetDataWriter();
            if (!Program.LoggedInUsers.TryGetValue(peer, out var _))
            {
                writer.Put(0);
                Program.ConnectedPeers.Remove(peer);
                peer.Disconnect();
                return writer;
            }
            var uid = reader.GetInt();
            Vector3 location = new(0, 0, 0.7f);
            Vector3 scale = CommonObjects.GetVector3(reader);
            Vector3 rotation = new Vector3(0, 0, 0);
            HttpClient client = new HttpClient();
            var result = client.GetStringAsync((string)Program.Config["huanyuApiHost"]! + $"/PlaysisService/GetUsername?uid={uid}");

            PlayerObject player = new(uid, result.ToString()!, (string)Program.LoggedInUsers[peer]["secret"]);
            writer.Put(1);
            return writer;
        }
    }
}
