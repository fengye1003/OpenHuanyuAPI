using PlaysisServer.Objects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlaysisServer.Essentials
{
    public static class PublicHallHelper
    {
        //public static string SavePath = "./publichall.json";

        private static readonly JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            IncludeFields = true
        };

        // =========================
        // 保存
        // =========================
        public static void Save(PlaysisRoom room)
        {
            var save = ToSave(room);

            string json = JsonSerializer.Serialize(save, options);
            File.WriteAllText(Program.HallSavePath, json);
        }

        // =========================
        // 加载
        // =========================
        public static PlaysisRoom Load()
        {
            if (!File.Exists(Program.HallSavePath))
                throw new Exception("存档不存在");

            string json = File.ReadAllText(Program.HallSavePath);

            var save = JsonSerializer.Deserialize<PlaysisRoomSave>(json, options)
                       ?? throw new Exception("反序列化失败");

            return FromSave(save);
        }

        // =========================
        // 转换：运行时 -> 存档
        // =========================
        private static PlaysisRoomSave ToSave(PlaysisRoom room)
        {
            var save = new PlaysisRoomSave
            {
                RoomID = room.RoomID,
                Name = room.Name,
                Description = room.Description,
                OwnerUID = room.Owner?.UID
            };

            foreach (var p in room.Players)
            {
                save.Players.Add(new PlayerSave
                {
                    UID = p.UID
                });
            }

            foreach (var m in room.Models.Values)
            {
                save.Models.Add(new ModelSave
                {
                    ParentUID = m.ParentPlayer.UID,
                    Name = m.Name,
                    FetchURL = m.FetchURL,
                    ModelHash = m.ModelHash,
                    Location = m.Location,
                    Scale = m.Scale,
                    Rotation = m.Rotation
                });
            }

            return save;
        }

        // =========================
        // 转换：存档 -> 运行时
        // =========================
        private static PlaysisRoom FromSave(PlaysisRoomSave save)
        {
            // 先构建玩家表（用于引用恢复）
            Dictionary<int, PlayerObject> playerMap = new();

            foreach (var p in save.Players)
            {
                playerMap[p.UID] = new PlayerObject(p.UID, null, null);
            }

            // 创建房间
            PlayerObject? owner = null;
            if (save.OwnerUID.HasValue && playerMap.ContainsKey(save.OwnerUID.Value))
            {
                owner = playerMap[save.OwnerUID.Value];
            }

            var room = new PlaysisRoom(save.RoomID, save.Name, owner, save.Description);

            // 加入玩家
            foreach (var p in playerMap.Values)
            {
                room.Players.Add(p);
            }

            // 恢复模型
            int modelId = 0;
            foreach (var m in save.Models)
            {
                if (!playerMap.TryGetValue(m.ParentUID, out var parent))
                    continue;

                var model = new ModelObject(parent, m.Name, m.FetchURL, m.ModelHash)
                {
                    Location = m.Location,
                    Scale = m.Scale,
                    Rotation = m.Rotation
                };

                room.Models[modelId++] = model;
            }

            return room;
        }
    }
}