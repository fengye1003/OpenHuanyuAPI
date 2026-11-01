using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Objects;
using System;
using System.Collections.Generic;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public const byte ScoreOpSet = 0;
        public const byte ScoreOpList = 1;

        public static void WriteScore(NetDataWriter writer, ScoreObject s)
        {
            writer.Put(s.ModelUid);
            writer.Put(s.Score);
            writer.Put(s.Comment ?? "");
            writer.Put(s.UpdatedBy);
            writer.Put(s.UpdatedAt);
            writer.Put(s.History.Count);
            foreach (var h in s.History)
            {
                writer.Put(h.Score);
                writer.Put(h.Comment ?? "");
                writer.Put(h.ByUid);
                writer.Put(h.At);
            }
        }

        public static NetDataWriter ScoreSync(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);

            var uid = reader.GetString();
            Dictionary<string, object>? info = null;
            lock (Program.LoggedInUsers) Program.LoggedInUsers.TryGetValue(peer, out info);

            if (info == null || (string)info["uid"] != uid || !info.TryGetValue("playerObj", out var po) || po is not PlayerObject player)
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

            var op = reader.GetByte();

            if (op == ScoreOpSet)
            {
                var modelUid = reader.GetString();

                var isOwner = room.Owner != null && room.Owner.UID == uid;
                var isSelf = modelUid == uid;
                if (!isOwner && !isSelf)
                {
                    writer.Put(0);
                    return writer;
                }
                var score = reader.GetInt();
                var comment = reader.GetString();

                if (score < 1 || score > 5 || comment.Length > 300)
                {
                    writer.Put(0);
                    return writer;
                }

                ScoreObject target;
                lock (room)
                {
                    if (!room.Scores.TryGetValue(modelUid, out var existing))
                    {
                        existing = new ScoreObject { ModelUid = modelUid };
                        room.Scores[modelUid] = existing;
                    }

                    existing.History.Add(new ScoreHistoryEntry
                    {
                        Score = score,
                        Comment = comment,
                        ByUid = uid,
                        At = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });

                    existing.Score = score;
                    existing.Comment = comment;
                    existing.UpdatedBy = uid;
                    existing.UpdatedAt = existing.History[^1].At;
                    target = existing;
                }

                ScoreStore.Save(room);
                PlaysisServer.Essentials.UsageCollector.Record(uid, room.RoomID, "set_score", modelId: modelUid, score: score);

                PushScore(room, peer, target);
                WriteScoreList(writer, room);
                return writer;
            }

            WriteScoreList(writer, room);
            return writer;
        }

        static void WriteScoreList(NetDataWriter writer, PlaysisRoom room)
        {
            List<ScoreObject> snapshot;
            lock (room) snapshot = new List<ScoreObject>(room.Scores.Values);

            writer.Put(1);
            writer.Put(snapshot.Count);
            foreach (var s in snapshot) WriteScore(writer, s);
        }

        static void PushScore(PlaysisRoom room, NetPeer source, ScoreObject score)
        {
            var payload = new NetDataWriter();
            payload.Put(room.RoomID);
            WriteScore(payload, score);

            List<NetPeer> peers;
            lock (Program.LoggedInUsers) peers = new List<NetPeer>(Program.LoggedInUsers.Keys);

            foreach (var p in peers)
            {
                if (p == source) continue;

                Dictionary<string, object>? info = null;
                lock (Program.LoggedInUsers) Program.LoggedInUsers.TryGetValue(p, out info);
                if (info == null || !info.TryGetValue("playerObj", out var po) || po is not PlayerObject other) continue;
                if (other.Room != room) continue;

                Program.SendPush(p, CommonObjects.OpCode.SetScore, score.UpdatedBy, payload);
            }
        }
    }
}



