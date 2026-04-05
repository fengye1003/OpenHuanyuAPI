using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Essencial_Repos;
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
        public static NetDataWriter SyncRoomStatus(NetPeer peer, NetPacketReader reader, int requestId)
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
            writer.Put(1);
            var players = ((PlayerObject)userInfo["playerObj"]).Room.Players;
            foreach (var player in players)
            {
                CommonObjects.PlacePlayerPacket(writer, player);
            }
            CommonObjects.PlaceEOFPacket(writer);
            //Log.SaveLog($"已发送包，共{players.Count}玩家");
            return(writer);
        }
        public static NetDataWriter SyncRoomModelStatus(NetPeer peer, NetPacketReader reader, int requestId)
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
            writer.Put(1);
            foreach (var model in ((PlayerObject)userInfo["playerObj"]).Room.Models.Values)
            {
                CommonObjects.PlaceModelPacket(writer, model);
                Log.SaveLog($"已发送{model.ModelHash}");
            }
            CommonObjects.PlaceEOFPacket(writer);
            return writer;
        }


    }
}
