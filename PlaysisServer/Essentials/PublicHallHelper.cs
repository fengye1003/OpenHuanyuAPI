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
                    ,
                    FootprintX = m.FootprintX,
                    FootprintZ = m.FootprintZ,
                    OffsetX = m.OffsetX,
                    OffsetZ = m.OffsetZ
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
            Dictionary<string, PlayerObject> playerMap = new();

            foreach (var p in save.Players)
            {
                playerMap[p.UID] = new PlayerObject(p.UID, null, null);
            }

            // 创建房间
            PlayerObject? owner = null;
            if (!string.IsNullOrEmpty(save.OwnerUID) && playerMap.ContainsKey(save.OwnerUID))
            {
                owner = playerMap[save.OwnerUID];
            }

            var room = new PlaysisRoom(save.RoomID, save.Name, owner, save.Description);

            // 加入玩家
            foreach (var p in playerMap.Values)
            {
                room.Players.Add(p);
            }

            var relocated = false;
            // 恢复模型
            //int modelId = 0; //叽里咕噜说什么呢
            foreach (var m in save.Models)
            {
                if (string.IsNullOrEmpty(m.ParentUID)) continue;

                if (!playerMap.TryGetValue(m.ParentUID, out var parent))
                {
                    parent = new PlayerObject(m.ParentUID, null, null);
                    playerMap[m.ParentUID] = parent;
                }

                var model = new ModelObject(parent, m.Name, m.FetchURL, m.ModelHash)
                {
                    Location = m.Location,
                    Scale = m.Scale,
                    Rotation = m.Rotation,
                    FootprintX = m.FootprintX,
                    FootprintZ = m.FootprintZ,
                    OffsetX = m.OffsetX,
                    OffsetZ = m.OffsetZ
                };

                if (model.Location.Y < Program.ModelGroundOffset)
                {
                    model.Location = new System.Numerics.Vector3(model.Location.X, Program.ModelGroundOffset, model.Location.Z);
                    relocated = true;
                }

                var effX = model.FootprintX > 0.01f ? model.FootprintX : 20f;
                var effZ = model.FootprintZ > 0.01f ? model.FootprintZ : 20f;
                model.FootprintX = effX;
                model.FootprintZ = effZ;

                {
                    var half = Program.MainCityHalfSize;
                    var left = model.Location.X + model.OffsetX - effX * 0.5f;
                    var right = model.Location.X + model.OffsetX + effX * 0.5f;
                    var bottom = model.Location.Z + model.OffsetZ - effZ * 0.5f;
                    var top = model.Location.Z + model.OffsetZ + effZ * 0.5f;

                    if (left < half && right > -half && bottom < half && top > -half)
                    {
                        var occupied = PlacementAllocator.FromModels(room.Models.Values,
                            o => o.Location.X, o => o.Location.Z,
                            o => Math.Max(o.FootprintX, 1f), o => Math.Max(o.FootprintZ, 1f));

                        var moved = PlacementAllocator.Allocate(model.FootprintX, model.FootprintZ, occupied,
                            maxRing: 256, keepOut: Program.MainCityRect());

                        if (moved.Found)
                        {
                            PlaysisServer.Essencial_Repos.Log.SaveLog($"模型 {m.ParentUID} 存档位置侵入主城，已重排到 ({moved.X}, {moved.Z})");
                            relocated = true;
                            model.Location = new System.Numerics.Vector3(moved.X - model.OffsetX, model.Location.Y, moved.Z - model.OffsetZ);
                        }
                        else
                        {
                            PlaysisServer.Essencial_Repos.Log.SaveLog($"模型 {m.ParentUID} 存档位置侵入主城且无法重排，已跳过");
                            continue;
                        }
                    }
                }

                if (room.Models.ContainsKey(m.ParentUID))
                    m.ParentUID = m.ParentUID + "_" + room.Models.Count;

                room.Models[m.ParentUID] = model;
            }

            if (relocated)
            {
                try { Save(room); PlaysisServer.Essencial_Repos.Log.SaveLog("主城重排结果已写回存档"); }
                catch (Exception ex) { PlaysisServer.Essencial_Repos.Log.SaveLog("写回存档失败：" + ex.Message); }
            }

            return room;
        }
    }
}





