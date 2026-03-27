using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TencentCloud.Gpm.V20200820.Models;
using TencentCloud.Mgobe.V20201014.Models;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public static NetDataWriter PlayerSpawn(NetPeer peer, NetPacketReader reader)
        {
            
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
            
            Vector3 location = new(0, 0, 0.7f);
            Vector3 scale = CommonObjects.GetVector3(reader);
            Vector3 rotation = new Vector3(0, 0, 0);
            HttpClient client = new HttpClient();
            var result = client.GetStringAsync((string)Program.Config["huanyuApiHost"]! + $"/PlaysisService/GetUsername?uid={uid}");

            PlayerObject player = new(uid, result.ToString()!, (string)Program.LoggedInUsers[peer]["secret"]);
            Program.LoggedInUsers[peer].Add("playerObj", player);
            player.UserNetPeer = peer;
            player.Room = Program.PublicHall;
            player.Location = location;
            player.Scale = scale;
            player.Rotation = rotation;


            NetDataWriter playerMsg = new();
            playerMsg.Put((byte)CommonObjects.OpCode.PlayerSpawn);
            playerMsg.Put(uid);
            CommonObjects.PutVector3(playerMsg, location);
            CommonObjects.PutVector3(playerMsg, scale);
            CommonObjects.PutVector3(playerMsg, rotation);
            // Broadcast Stage
            foreach (var target in player.Room.Players)
            {
                target.UserNetPeer.Send(playerMsg, DeliveryMethod.ReliableOrdered);
            }

            writer.Put(1);
            return writer;
        }

        public static NetDataWriter PlayerMove(NetPeer peer, NetPacketReader reader)
        {
            var writer = new NetDataWriter();
            //Receive Stage
            var uid = reader.GetInt();
            if (!Program.LoggedInUsers.TryGetValue(peer, out var userInfo))
            {
                writer.Put(0);
                Program.ConnectedPeers.Remove(peer);
                peer.Disconnect();
                return writer;
            }
            else if ((int)userInfo["uid"] != uid)
            {
                writer.Put(0);
                return writer;
            }

            Vector3 location = CommonObjects.GetVector3(reader);
            Vector3 rotation = CommonObjects.GetVector3(reader);

            //Broadcast Stage
            NetDataWriter playerMsg = new();
            playerMsg.Put((byte)CommonObjects.OpCode.PlayerMove);
            playerMsg.Put(uid);
            CommonObjects.PutVector3(playerMsg, location);
            CommonObjects.PutVector3(playerMsg, rotation);
            // Broadcast Stage
            foreach (var target in ((PlayerObject)(Program.LoggedInUsers[peer]["playerObj"])).Room.Players)
            {
                target.UserNetPeer.Send(playerMsg, DeliveryMethod.ReliableOrdered);
            }

            writer.Put(1);
            return writer;
        }
    }
}
