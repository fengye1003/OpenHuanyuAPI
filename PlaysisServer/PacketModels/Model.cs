using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Essencial_Repos;
using PlaysisServer.Essentials;
using PlaysisServer.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TencentCloud.Mgobe.V20201014.Models;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public static NetDataWriter RequestUploadModel(NetPeer peer, NetPacketReader reader, int requestId)
        {
            //Log.SaveLog("Receive request.");
            var writer = new NetDataWriter();
            writer.Put(requestId);
            // Receive Stage
            var uid = reader.GetString();
            if (!Program.LoggedInUsers.TryGetValue(peer, out var userInfo))
            {
                writer.Put(0);
                Program.ConnectedPeers.Remove(peer);
                return writer;
            }
            else if ((string)userInfo["uid"] != uid)
            {
                writer.Put(0);
                return writer;
            }
            writer.Put(1);

            var hash = reader.GetString();//hash
            TencentCOSHelper helper = new();
            helper.filename = hash;
            var url = helper.GetPresignedUrl(hash);
            writer.Put(url);
            //Log.SaveLog(url);
            return writer;
        }

        public static NetDataWriter SendAddModelUrl(NetPeer peer, NetPacketReader reader, int requestId)
        {
            //Log.SaveLog("Receive request.");
            var writer = new NetDataWriter();
            writer.Put(requestId);
            // Receive Stage
            var uid = reader.GetString();
            if (!Program.LoggedInUsers.TryGetValue(peer, out var userInfo))
            {
                writer.Put(0);
                Program.ConnectedPeers.Remove(peer);
                return writer;
            }
            else if ((string)userInfo["uid"] != uid)
            {
                writer.Put(0);
                return writer;
            }
            writer.Put(1);

            var hash = reader.GetString();
            var url = reader.GetString();

            if (!Directory.Exists("./ModelStorage/"))
            {
                Directory.CreateDirectory("./ModelStorage/");
            }
            if (!File.Exists($"./ModelStorage/{hash}"))
            {
                File.Create($"./ModelStorage/{hash}").Close();
            }
            File.WriteAllText($"./ModelStorage/{hash}", $"{uid}\n{url}");
            Log.SaveLog($"成功存储模型{hash}");

            ModelObject model = new ModelObject((PlayerObject)userInfo["playerObj"], ((PlayerObject)userInfo["playerObj"]).Name, url, hash);
            model.Location = CommonObjects.GetVector3(reader);
            model.Scale = CommonObjects.GetVector3(reader);
            model.Rotation = CommonObjects.GetVector3(reader); 

            var footprintX = 0f;
            var footprintZ = 0f;
            var offsetX = 0f;
            var offsetZ = 0f;
            var normalizeScale = 1f;
            if (reader.AvailableBytes >= 16)
            {
                footprintX = reader.GetFloat();
                footprintZ = reader.GetFloat();
                offsetX = reader.GetFloat();
                offsetZ = reader.GetFloat();
            }

            if (Program.AutoPlaceModels && Math.Abs(model.Location.X) < 0.001f && Math.Abs(model.Location.Z) < 0.001f)
            {
                var room = ((PlayerObject)userInfo["playerObj"]).Room;
                List<Footprint> occupied;
                lock (room) occupied = PlacementAllocator.FromModels(room.Models.Values,
                    m => m.Location.X, m => m.Location.Z,
                    m => m.FootprintX > 0.01f ? m.FootprintX : 20f, m => m.FootprintZ > 0.01f ? m.FootprintZ : 20f);

                var width = footprintX > 0.01f ? footprintX : model.Scale.X;
                var depth = footprintZ > 0.01f ? footprintZ : model.Scale.Z;

                var placement = PlacementAllocator.Allocate(width, depth, occupied,
                    maxRing: 128, keepOut: Program.MainCityRect());

                if (placement.Found)
                {
                    model.Location = new System.Numerics.Vector3(
                        placement.X - offsetX,
                        Math.Max(model.Location.Y, Program.ModelGroundOffset),
                        placement.Z - offsetZ);
                    normalizeScale = placement.Scale;
                    model.FootprintX = width;
                    model.OffsetX = offsetX;
                    model.OffsetZ = offsetZ;
                    model.FootprintZ = depth;

                    model.PlacementX = placement.X;
                    model.PlacementZ = placement.Z;

                    Log.SaveLog($"自动摆放：model {hash} 尺寸 {width:F1}x{depth:F1} -> ({placement.X:F1}, {placement.Z:F1})");
                }
                else
                {
                    Log.SaveLog($"自动摆放失败（无空间）：model {hash} 尺寸 {width:F1}x{depth:F1}");
                }
            }
            if (((PlayerObject)userInfo["playerObj"]).Room.Models.TryGetValue(((PlayerObject)userInfo["playerObj"]).UID, out var obj))
            {
                ((PlayerObject)userInfo["playerObj"]).Room.Models[((PlayerObject)userInfo["playerObj"]).UID] = model;
            }
            else
            {
                ((PlayerObject)userInfo["playerObj"]).Room.Models.Add(((PlayerObject)userInfo["playerObj"]).UID, model);
            }
            
            CommonObjects.PutVector3(writer, model.Location);
            writer.Put(normalizeScale);
            PlaysisServer.Essentials.UsageCollector.Record(uid,
                ((PlayerObject)userInfo["playerObj"]).Room.RoomID, "add_model", modelId: uid);
            Log.SaveLog($"成功将模型{hash}加载到房间。");
            if (Program.DoMaintainHallOnReload)
            {
                try
                {
                    PublicHallHelper.Save(Program.PublicHall);
                    Log.SaveLog("已将模型储存到本地存档备用。");
                }
                catch (Exception ex)
                {
                    Log.SaveLog($"存储存档到本地出现问题。异常：{ex}");
                    //throw;
                }
            }
            return writer;
        }

        [Obsolete]
        public static NetDataWriter SpawnModel(NetPeer peer, NetPacketReader reader)
        {
            //Log.SaveLog("Receive request.");
            var writer = new NetDataWriter();

            // Receive Stage
            var uid = reader.GetString();
            if (!Program.LoggedInUsers.TryGetValue(peer, out var userInfo))
            {
                writer.Put(0);
                Program.ConnectedPeers.Remove(peer);
                return writer;
            }
            else if ((string)userInfo["uid"] != uid)
            {
                writer.Put(0);
                return writer;
            }
            writer.Put(1);

            var hash = reader.GetString();
            var location = CommonObjects.GetVector3(reader);
            var scale = CommonObjects.GetVector3(reader);
            var rotation = CommonObjects.GetVector3(reader);
            if (File.Exists($"./ModelStorage/{hash}")) 
            {
                ModelObject model = new((PlayerObject)userInfo["playerObj"], 
                    ((PlayerObject)userInfo["playerObj"]).Name!,
                    File.ReadAllText($"./ModelStorage/{hash}").Split("\n")[1],
                    hash);
                model.Location = location;
                model.Scale = scale;
                model.Rotation = rotation;
                var existed = ((PlayerObject)userInfo["playerObj"]).Room.Models.TryGetValue(uid, out var value);
                if (!existed)
                    ((PlayerObject)userInfo["playerObj"]).Room.Models.Add(uid, model);
                else
                    ((PlayerObject)userInfo["playerObj"]).Room.Models[uid] = model;
                writer.Put(1);
            }
            else
                writer.Put(0);
            return writer;
        }
        
        public static NetDataWriter FetchModelInfo(NetPeer peer, NetPacketReader reader, int requestId)
        {
            //Log.SaveLog("Receive request.");
            var writer = new NetDataWriter();
            writer.Put(requestId);
            // Receive Stage
            var uid = reader.GetString();
            if (!Program.LoggedInUsers.TryGetValue(peer, out var userInfo))
            {
                writer.Put(0);
                Program.ConnectedPeers.Remove(peer);
                return writer;
            }
            else if ((string)userInfo["uid"] != uid)
            {
                writer.Put(0);
                return writer;
            }
            writer.Put(1);

            var hash = reader.GetString();
            if (File.Exists($"./ModelStorage/{hash}"))
            {
                writer.Put(1);
                var content = File.ReadAllLines($"./ModelStorage/{hash}");
                var modelOwnerUid = content[0].Trim();
                var url = content[1];
                HttpClient client = new();
                var api = (string)Program.Config["huanyuApiHost"]! + $"/PlaysisService/GetUsername?uid={modelOwnerUid}";
                var response = client.GetAsync(api);
                var resContent = response.Result.Content.ReadAsStringAsync();
                string result = resContent.Result;
                var name = result;
                writer.Put(modelOwnerUid);
                writer.Put(name);
                writer.Put(url);
            }
            else
                writer.Put(0);
            return writer;
            
        }
    
        public static async Task FetchModelInfoAsync(NetPeer peer, string uid, string hash, int requestId)
        {
            var writer = new NetDataWriter();
            writer.Put(requestId);

            Dictionary<string, object>? userInfo = null;
            lock (Program.LoggedInUsers)
                Program.LoggedInUsers.TryGetValue(peer, out userInfo);

            if (userInfo == null || (string)userInfo["uid"] != uid)
            {
                writer.Put(0);
            }
            else if (!File.Exists($"./ModelStorage/{hash}"))
            {
                writer.Put(1);
                writer.Put(0);
            }
            else
            {
                writer.Put(1);
                writer.Put(1);

                var content = File.ReadAllLines($"./ModelStorage/{hash}");
                var modelOwnerUid = content[0].Trim();
                var url = content[1];

                string name = "";
                try
                {
                    var api = (string)Program.Config["huanyuApiHost"]! + $"/PlaysisService/GetUsername?uid={modelOwnerUid}";
                    using var resp = await Program.Http.GetAsync(api).ConfigureAwait(false);
                    name = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.SaveLog("FetchModelInfoAsync HTTP 失败: " + ex.GetType().Name + " " + ex.Message);
                }

                writer.Put(modelOwnerUid);
                writer.Put(name);
                writer.Put(url);
            }

            Program.PendingSends.Enqueue(() =>
            {
                if (peer.ConnectionState == ConnectionState.Connected)
                    peer.SendWithDeliveryEvent(writer, DeliveryMethod.ReliableOrdered, null);
            });
        }
    }}













