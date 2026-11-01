using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PlaysisServer.Objects
{
    public class RoomRecord
    {
        public int RoomID { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string OwnerUid { get; set; }
        public string Code { get; set; } = "";
        public int Capacity { get; set; }
        public long CreatedAt { get; set; }
    }

    public static class RoomStore
    {
        public const string Path = "./Rooms/rooms.json";

        public static void Save(List<PlaysisRoom> rooms, Func<int, string> codeOf)
        {
            try
            {
                Directory.CreateDirectory("./Rooms");

                var records = new List<RoomRecord>();
                foreach (var room in rooms)
                {
                    if (room.RoomID == 0) continue;

                    records.Add(new RoomRecord
                    {
                        RoomID = room.RoomID,
                        Name = room.Name ?? "",
                        Description = room.Description ?? "",
                        OwnerUid = room.Owner?.UID ?? "",
                        Code = codeOf(room.RoomID),
                        CreatedAt = room.CreatedAt,
                        Capacity = room.Capacity
                    });
                }

                File.WriteAllText(Path, JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[RoomStore] save failed: " + ex.Message);
            }
        }

        public static List<RoomRecord> Load()
        {
            if (!File.Exists(Path)) return new List<RoomRecord>();

            try
            {
                var list = JsonSerializer.Deserialize<List<RoomRecord>>(File.ReadAllText(Path));
                return list ?? new List<RoomRecord>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[RoomStore] load failed: " + ex.Message);
                return new List<RoomRecord>();
            }
        }
    }
}




