using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PlaysisServer.Objects
{
    public class ScoreHistoryEntry
    {
        public int Score { get; set; }
        public string Comment { get; set; } = "";
        public string ByUid { get; set; }
        public long At { get; set; }
    }

    public class ScoreObject
    {
        public string ModelUid { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; } = "";
        public string UpdatedBy { get; set; }
        public long UpdatedAt { get; set; }
        public List<ScoreHistoryEntry> History { get; set; } = new();
    }

    public static class ScoreStore
    {
        public static string FileFor(int roomId) => $"./AnnotationStorage/scores_room_{roomId}.json";

        public static void Save(PlaysisRoom room)
        {
            Directory.CreateDirectory("./AnnotationStorage");
            var values = new List<ScoreObject>(room.Scores.Values);
            File.WriteAllText(FileFor(room.RoomID), JsonSerializer.Serialize(values));
        }

        public static void Load(PlaysisRoom room)
        {
            var path = FileFor(room.RoomID);
            if (!File.Exists(path)) return;

            try
            {
                var list = JsonSerializer.Deserialize<List<ScoreObject>>(File.ReadAllText(path));
                if (list == null) return;
                room.Scores.Clear();
                foreach (var s in list) room.Scores[s.ModelUid] = s;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Score] load failed: " + ex.Message);
            }
        }
    }
}

