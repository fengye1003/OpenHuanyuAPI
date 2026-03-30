using LiteNetLib;
using LiteNetLib.Utils;
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
        public static NetDataWriter SyncRoomStatus(NetPeer peer, NetPacketReader reader)
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
            writer.Put(1);
            var players = ((PlayerObject)userInfo["playerObj"]).Room.Players;
            foreach (var player in players)
            {
                CommonObjects.PlacePlayerPacket(writer, player);
            }
            CommonObjects.PlaceEOFPacket(writer);
            return(writer);
        }

        
    }
}
