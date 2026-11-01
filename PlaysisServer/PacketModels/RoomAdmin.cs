using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Objects;
using PlaysisServer.Essencial_Repos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        static readonly Random CodeRng = new();

        static readonly Dictionary<int, string> RoomCodes = new();

        public static void RegisterCode(int roomId, string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            lock (RoomCodes) RoomCodes[roomId] = code;
        }

        public static string CodeFor(int roomId)
        {
            lock (RoomCodes)
                return RoomCodes.TryGetValue(roomId, out var code) ? code : "";
        }

        static string NewCode(int roomId)
        {
            lock (RoomCodes)
            {
                if (RoomCodes.TryGetValue(roomId, out var existing)) return existing;

                while (true)
                {
                    var chars = new char[6];
                    for (var i = 0; i < chars.Length; i++) chars[i] = CodeAlphabet[CodeRng.Next(CodeAlphabet.Length)];
                    var code = new string(chars);
                    if (RoomCodes.ContainsValue(code)) continue;
                    RoomCodes[roomId] = code;
                    return code;
                }
            }
        }

        public static NetDataWriter RoomCreate(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);

            var uid = reader.GetString();
            if (!TryGetPlayer(peer, uid, out var player))
            {
                writer.Put(0);
                return writer;
            }

            var name = reader.GetString();
            var description = reader.GetString();

            var capacity = 0;
            if (reader.AvailableBytes >= 4) capacity = reader.GetInt();
            if (capacity < 0) capacity = 0;

            if (string.IsNullOrWhiteSpace(name) || name.Length > 64 || description.Length > 256)
            {
                writer.Put(0);
                return writer;
            }

            PlaysisRoom room;
            lock (Program.Rooms)
            {
                var nextId = 1;
                if (Program.Rooms.Count > 0) nextId = Program.Rooms.Max(r => r.RoomID) + 1;

                room = new PlaysisRoom(nextId, name, player, description) { Capacity = capacity };
                Program.Rooms.Add(room);
            }

            player.Room = room;

            var code = NewCode(room.RoomID);

            writer.Put(1);
            writer.Put(room.RoomID);
            writer.Put(room.Name);
            writer.Put(code);

            Program.PlaysisRoomChanged();
            RoomStore.Save(Program.Rooms, CodeFor);
            PlaysisServer.Essentials.UsageCollector.Record(uid, room.RoomID, "create_room");
            return writer;
        }

        public static NetDataWriter RoomJoinByCode(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);

            var uid = reader.GetString();
            if (!TryGetPlayer(peer, uid, out var player))
            {
                writer.Put(0);
                return writer;
            }

            var code = reader.GetString().Trim().ToUpperInvariant();

            int roomId;
            lock (RoomCodes)
            {
                var hit = RoomCodes.FirstOrDefault(kv => kv.Value == code);
                roomId = hit.Equals(default(KeyValuePair<int, string>)) ? -1 : hit.Key;
            }

            PlaysisRoom? target = null;
            if (roomId >= 0)
                lock (Program.Rooms) target = Program.Rooms.Find(r => r.RoomID == roomId);

            if (target == null)
            {
                writer.Put(0);
                return writer;
            }

            lock (target)
            {
                if (target.Capacity > 0 && target.Players.Count >= target.Capacity && !target.Players.Contains(player))
                {
                    writer.Put(0);
                    writer.Put(1);
                    return writer;
                }
            }

            player.Room = target;

            writer.Put(1);
            writer.Put(target.RoomID);
            writer.Put(target.Name);
            writer.Put(target.Players.Count);

            Program.PlaysisRoomChanged();
            PlaysisServer.Essentials.UsageCollector.Record(uid, target.RoomID, "join_room_by_code");
            return writer;
        }

        static bool TryGetPlayer(NetPeer peer, string uid, out PlayerObject player)
        {
            player = null!;

            Dictionary<string, object>? info = null;
            lock (Program.LoggedInUsers) Program.LoggedInUsers.TryGetValue(peer, out info);

            if (info == null || (string)info["uid"] != uid) return false;
            if (!info.TryGetValue("playerObj", out var po) || po is not PlayerObject p) return false;

            player = p;
            return true;
        }
    
        public static NetDataWriter RoomDelete(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);

            var uid = reader.GetString();
            if (!TryGetPlayer(peer, uid, out var player))
            {
                writer.Put(0);
                return writer;
            }

            var roomId = reader.GetInt();
            if (roomId == 0)
            {
                writer.Put(0);
                return writer;
            }

            PlaysisRoom? target;
            lock (Program.Rooms) target = Program.Rooms.Find(r => r.RoomID == roomId);
            if (target == null || target.Owner == null || target.Owner.UID != uid)
            {
                writer.Put(0);
                return writer;
            }

            List<PlayerObject> moved;
            lock (target)
            {
                moved = new List<PlayerObject>(target.Players);
                foreach (var p in moved) p.Room = Program.PublicHall;
                target.Players.Clear();
                target.Models.Clear();
                target.Annotations.Clear();
                target.Scores.Clear();
            }

            lock (Program.Rooms) Program.Rooms.Remove(target);
            lock (RoomCodes) RoomCodes.Remove(roomId);

            try
            {
                var annPath = AnnotationStore.DirFor(roomId);
                if (System.IO.File.Exists(annPath)) System.IO.File.Delete(annPath);
                var scorePath = ScoreStore.FileFor(roomId);
                if (System.IO.File.Exists(scorePath)) System.IO.File.Delete(scorePath);
            }
            catch (Exception ex)
            {
                Log.SaveLog("删除房间存档失败: " + ex.Message);
            }

            writer.Put(1);
            writer.Put(roomId);

            Program.PlaysisRoomChanged();
            RoomStore.Save(Program.Rooms, CodeFor);
            PlaysisServer.Essentials.UsageCollector.Record(uid, roomId, "delete_room");
            return writer;
        }
    }
}







