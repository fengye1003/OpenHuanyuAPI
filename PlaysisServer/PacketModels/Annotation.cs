using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Essencial_Repos;
using PlaysisServer.Objects;
using System;
using System.Collections.Generic;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public const byte AnnotationOpAdd = 0;
        public const byte AnnotationOpDelete = 1;
        public const byte AnnotationOpList = 2;

        public static void WriteAnnotation(NetDataWriter writer, AnnotationObject a)
        {
            writer.Put(a.Id);
            writer.Put(a.ModelUid);
            writer.Put(a.X);
            writer.Put(a.Y);
            writer.Put(a.Z);
            writer.Put(a.NX);
            writer.Put(a.NY);
            writer.Put(a.NZ);
            writer.Put(a.Text ?? "");
            writer.Put(a.AuthorUid);
            writer.Put(a.CreatedAt);
        }

        public static NetDataWriter AnnotationSync(NetPeer peer, NetPacketReader reader, int requestId)
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

            if (op == AnnotationOpAdd)
            {
                var modelUid = reader.GetString();
                var x = reader.GetFloat();
                var y = reader.GetFloat();
                var z = reader.GetFloat();
                var nx = reader.GetFloat();
                var ny = reader.GetFloat();
                var nz = reader.GetFloat();
                var text = reader.GetString();

                if (string.IsNullOrWhiteSpace(text) || text.Length > 500)
                {
                    writer.Put(0);
                    return writer;
                }

                AnnotationObject annotation;
                lock (room)
                {
                    annotation = new AnnotationObject
                    {
                        Id = room.NextAnnotationId++,
                        ModelUid = modelUid,
                        X = x, Y = y, Z = z,
                        NX = nx, NY = ny, NZ = nz,
                        Text = text,
                        AuthorUid = uid,
                        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                    room.Annotations.Add(annotation);
                }

                AnnotationStore.Save(room);

                PlaysisServer.Essentials.UsageCollector.Record(uid, room.RoomID, "add_annotation",
                    annotationCount: room.Annotations.Count);

                PushToRoom(room, peer, annotation, op);
                WriteFullList(writer, room);
                return writer;
            }

            if (op == AnnotationOpDelete)
            {
                var id = reader.GetInt();
                var removed = false;
                AnnotationObject? target = null;

                lock (room)
                {
                    target = room.Annotations.Find(a => a.Id == id);
                    if (target != null && target.AuthorUid == uid)
                    {
                        room.Annotations.Remove(target);
                        removed = true;
                    }
                }

                if (!removed)
                {
                    writer.Put(0);
                    return writer;
                }

                AnnotationStore.Save(room);

                PushToRoom(room, peer, target!, op);
                WriteFullList(writer, room);
                return writer;
            }

            WriteFullList(writer, room);
            return writer;
        }

        static void WriteFullList(NetDataWriter writer, PlaysisRoom room)
        {
            List<AnnotationObject> snapshot;
            lock (room) snapshot = new List<AnnotationObject>(room.Annotations);

            writer.Put(1);
            writer.Put(snapshot.Count);
            foreach (var a in snapshot) WriteAnnotation(writer, a);
        }

        static void PushToRoom(PlaysisRoom room, NetPeer source, AnnotationObject annotation, byte op)
        {
            var payload = new NetDataWriter();
            payload.Put(op);
            payload.Put(room.RoomID);
            WriteAnnotation(payload, annotation);

            List<NetPeer> peers;
            lock (Program.LoggedInUsers) peers = new List<NetPeer>(Program.LoggedInUsers.Keys);

            foreach (var p in peers)
            {
                if (p == source) continue;

                Dictionary<string, object>? info = null;
                lock (Program.LoggedInUsers) Program.LoggedInUsers.TryGetValue(p, out info);
                if (info == null || !info.TryGetValue("playerObj", out var po) || po is not PlayerObject other) continue;
                if (other.Room != room) continue;

                Program.SendPush(p, CommonObjects.OpCode.AnnotationSync, annotation.AuthorUid, payload);
            }
        }
    }
}




