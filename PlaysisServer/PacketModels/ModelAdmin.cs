using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Objects;
using System;
using System.Collections.Generic;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public static NetDataWriter RemoveModel(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);

            var uid = reader.GetString();
            var modelUid = reader.GetString();

            Dictionary<string, object>? info = null;
            lock (Program.LoggedInUsers) Program.LoggedInUsers.TryGetValue(peer, out info);

            if (info == null || (string)info["uid"] != uid ||
                !info.TryGetValue("playerObj", out var po) || po is not PlayerObject player)
            {
                writer.Put(0);
                return writer;
            }

            var room = player.Room;
            if (room == null)
            {
                writer.Put(0);
                return writer;
            }

            ModelObject? target = null;
            lock (room)
            {
                if (!room.Models.TryGetValue(modelUid, out target))
                {
                    writer.Put(0);
                    return writer;
                }

                var isOwner = target.ParentPlayer != null && target.ParentPlayer.UID == uid;
                var isRoomOwner = room.Owner != null && room.Owner.UID == uid;
                if (!isOwner && !isRoomOwner)
                {
                    writer.Put(0);
                    return writer;
                }

                room.Models.Remove(modelUid);
            }

            writer.Put(1);
            writer.Put(modelUid);
            writer.Put(room.Models.Count);

            PushModelRemoved(room, peer, modelUid);
            PlaysisServer.Essentials.UsageCollector.Record(uid, room.RoomID, "remove_model", modelId: modelUid);
            return writer;
        }

        static void PushModelRemoved(PlaysisRoom room, NetPeer source, string modelUid)
        {
            var payload = new NetDataWriter();
            payload.Put(room.RoomID);
            payload.Put(modelUid);

            List<NetPeer> peers;
            lock (Program.LoggedInUsers) peers = new List<NetPeer>(Program.LoggedInUsers.Keys);

            foreach (var p in peers)
            {
                if (p == source) continue;

                Dictionary<string, object>? info = null;
                lock (Program.LoggedInUsers) Program.LoggedInUsers.TryGetValue(p, out info);
                if (info == null || !info.TryGetValue("playerObj", out var po) || po is not PlayerObject other) continue;
                if (other.Room != room) continue;

                Program.SendPush(p, CommonObjects.OpCode.ModelChanged, modelUid, payload);
            }
        }
    }
}



