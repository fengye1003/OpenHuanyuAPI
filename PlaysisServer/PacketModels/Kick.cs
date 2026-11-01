using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Objects;
using System;
using System.Collections.Generic;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public static NetDataWriter KickPlayer(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);

            var uid = reader.GetString();
            var targetUid = reader.GetString();

            Dictionary<string, object>? info = null;
            lock (Program.LoggedInUsers) Program.LoggedInUsers.TryGetValue(peer, out info);

            if (info == null || (string)info["uid"] != uid ||
                !info.TryGetValue("playerObj", out var po) || po is not PlayerObject player)
            {
                writer.Put(0);
                return writer;
            }

            var room = player.Room;
            if (room == null || room.Owner == null || room.Owner.UID != uid || targetUid == uid)
            {
                writer.Put(0);
                return writer;
            }

            PlayerObject? target = null;
            NetPeer? targetPeer = null;
            lock (Program.LoggedInUsers)
            {
                foreach (var kv in Program.LoggedInUsers)
                {
                    if (kv.Value.TryGetValue("playerObj", out var obj) && obj is PlayerObject p && p.UID == targetUid && p.Room == room)
                    {
                        target = p;
                        targetPeer = kv.Key;
                        break;
                    }
                }
            }

            if (target == null || targetPeer == null)
            {
                writer.Put(0);
                return writer;
            }

            target.Room = Program.PublicHall;

            var payload = new NetDataWriter();
            payload.Put(room.RoomID);
            payload.Put(uid);
            payload.Put("房主将你移出了房间");
            Program.SendPush(targetPeer, CommonObjects.OpCode.Kicked, targetUid, payload);

            writer.Put(1);
            writer.Put(targetUid);

            Program.PlaysisRoomChanged();
            PlaysisServer.Essentials.UsageCollector.Record(uid, room.RoomID, "kick_player");
            return writer;
        }
    }
}


