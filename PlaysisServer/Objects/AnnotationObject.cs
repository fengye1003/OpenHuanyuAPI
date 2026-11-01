using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PlaysisServer.Objects
{
    public class AnnotationObject
    {
        public int Id { get; set; }
        public string ModelUid { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float NX { get; set; }
        public float NY { get; set; }
        public float NZ { get; set; }
        public string Text { get; set; } = "";
        public string AuthorUid { get; set; }
        public long CreatedAt { get; set; }
    }

    public static class AnnotationStore
    {
        public static string DirFor(int roomId) => $"./AnnotationStorage/room_{roomId}.json";

        public static void Save(PlaysisRoom room)
        {
            Directory.CreateDirectory("./AnnotationStorage");
            var json = JsonSerializer.Serialize(room.Annotations, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(DirFor(room.RoomID), json);
        }

        public static void Load(PlaysisRoom room)
        {
            var path = DirFor(room.RoomID);
            if (!File.Exists(path)) return;
            try
            {
                var list = JsonSerializer.Deserialize<List<AnnotationObject>>(File.ReadAllText(path));
                if (list == null) return;
                room.Annotations.Clear();
                room.Annotations.AddRange(list);
                foreach (var a in room.Annotations)
                    if (a.Id >= room.NextAnnotationId) room.NextAnnotationId = a.Id + 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Annotation] load failed: " + ex.Message);
            }
        }
    }
}

