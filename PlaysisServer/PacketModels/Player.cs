using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Essencial_Repos;
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
        public static NetDataWriter PlayerSpawn(NetPeer peer, NetPacketReader reader, int requestId)
        {
            
            var writer = new NetDataWriter();
            writer.Put(requestId);
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
            
            Vector3 location = CommonObjects.GetVector3(reader);
            Vector3 scale = CommonObjects.GetVector3(reader);
            Vector3 rotation = CommonObjects.GetVector3(reader);
            HttpClient client = new HttpClient();
            var result = client.GetStringAsync((string)Program.Config["huanyuApiHost"]! + $"/PlaysisService/GetUsername?uid={uid}");

            PlayerObject player = new(uid, result.ToString()!, (string)Program.LoggedInUsers[peer]["secret"]);
            
            player.UserNetPeer = peer;
            player.Room = Program.PublicHall;
            player.Location = location;
            player.Scale = scale;
            player.Rotation = rotation;
            Program.LoggedInUsers[peer].Add("playerObj", player);

            NetDataWriter playerMsg = new();
            playerMsg.Put((byte)CommonObjects.OpCode.PlayerSpawn);
            playerMsg.Put(uid);
            CommonObjects.PutVector3(playerMsg, location);
            CommonObjects.PutVector3(playerMsg, scale);
            CommonObjects.PutVector3(playerMsg, rotation);
            // Broadcast Stage
            //Log.SaveLog($"Sending spawn for player {uid}");
            //foreach (var target in player.Room.Players)
            //{
            //    if (target.UserNetPeer != peer)
            //    {
            //        target.UserNetPeer!.Send(playerMsg, DeliveryMethod.ReliableOrdered);
            //    }
            //}
            //Abandoned due to duplicated function of Sync players

            writer.Put(1);
            //Log.SaveLog("已回调。");
            return writer;
        }

        public static NetDataWriter PlayerMove(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);
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
            //Log.SaveLog($"{location.X} {location.Y} {location.Z} ");
            
            if (Program.LoggedInUsers[peer].TryGetValue("playerObj", out object? value))
            {
                ((PlayerObject)value).Location = location;
                ((PlayerObject)value).Rotation = rotation;
                writer.Put(1);
            }
            else
            {
                writer.Put(0);
            }

            //this is for server-push solution
            ////Broadcast Stage
            //NetDataWriter playerMsg = new();
            //playerMsg.Put((byte)CommonObjects.OpCode.PlayerMove);
            //playerMsg.Put(uid);
            //CommonObjects.PutVector3(playerMsg, location);
            //CommonObjects.PutVector3(playerMsg, rotation);
            //// Broadcast Stage
            //foreach (var target in ((PlayerObject)(Program.LoggedInUsers[peer]["playerObj"])).Room.Players)
            //{
            //    target.UserNetPeer!.Send(playerMsg, DeliveryMethod.ReliableOrdered);
            //}
            //writer.Put(1);

            return writer;
        }
        public static NetDataWriter GetPlayerNameByUid(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);
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
            writer.Put(1);

            var queryUid = reader.GetInt();
            if (!TryGetNameByUid(queryUid, out string? result)) 
            {
                writer.Put(uid);
                writer.Put(1);
                writer.Put(result);
            }
            else
            {
                writer.Put(0);
            }
            return writer;
        }

        static public bool TryGetNameByUid(int uid, out string? name)
        {
            foreach (var item in Program.LoggedInUsers.Values)
            {
                if (!item.TryGetValue("playerObj", out object? targetObj))
                    continue;
                if (((PlayerObject)targetObj).UID == uid)
                {
                    name = ((PlayerObject)targetObj).Name!;
                    return true;
                }
            }
            name = null;
            return false;
        }
    }
}
