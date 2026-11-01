using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Essencial_Repos;
using PlaysisServer.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public static NetDataWriter SyncRoomStatus(NetPeer peer, NetPacketReader reader, int requestId)
        {
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
            var players = ((PlayerObject)userInfo["playerObj"]).Room.Players;
            foreach (var player in players)
            {
                CommonObjects.PlacePlayerPacket(writer, player);
            }
            CommonObjects.PlaceEOFPacket(writer);
            //Log.SaveLog($"已发送包，共{players.Count}玩家");
            return(writer);
        }
        public static NetDataWriter SyncRoomModelStatus(NetPeer peer, NetPacketReader reader, int requestId)
        {
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
            foreach (var model in ((PlayerObject)userInfo["playerObj"]).Room.Models.Values)
            {
                CommonObjects.PlaceModelPacket(writer, model);
                //Log.SaveLog($"已发送{model.ModelHash}");
            }
            CommonObjects.PlaceEOFPacket(writer);
            Log.SaveLog($"发模型同步：{((PlayerObject)userInfo["playerObj"]).Room.Models.Count} 个");
            return writer;
        }


    
        public static NetDataWriter FetchRoomList(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var w = new NetDataWriter();
            w.Put(requestId);

            var uid = reader.GetString();
            Dictionary<string, object>? info = null;
            lock (Program.LoggedInUsers) Program.LoggedInUsers.TryGetValue(peer, out info);

            if (info == null || (string)info["uid"] != uid)
            {
                w.Put(0);
                return w;
            }

            List<PlaysisRoom> rooms;
            lock (Program.Rooms) rooms = new List<PlaysisRoom>(Program.Rooms);

            w.Put(1);
            w.Put(rooms.Count);
            foreach (var room in rooms)
            {
                w.Put(room.RoomID);
                w.Put(room.Name ?? "");
                w.Put(room.Description ?? "");
                w.Put(room.Players.Count);
                w.Put(room.Owner != null ? room.Owner.UID : "");
                w.Put(room.Capacity);
            }
            return w;
        }

        public static NetDataWriter SwitchRoom(NetPeer peer, NetPacketReader reader, int requestId)
        {
            var w = new NetDataWriter();
            w.Put(requestId);

            var uid = reader.GetString();
            var targetRoomId = reader.GetInt();

            Dictionary<string, object>? info = null;
            lock (Program.LoggedInUsers) Program.LoggedInUsers.TryGetValue(peer, out info);

            if (info == null || (string)info["uid"] != uid || !info.TryGetValue("playerObj", out var po) || po is not PlayerObject player)
            {
                w.Put(0);
                return w;
            }

            PlaysisRoom? target;
            lock (Program.Rooms) target = Program.Rooms.Find(r => r.RoomID == targetRoomId);
            if (target == null)
            {
                w.Put(0);
                return w;
            }

            player.Room = target;

            w.Put(1);
            w.Put(target.RoomID);
            w.Put(target.Players.Count);

            PlaysisServer.Essentials.UsageCollector.Record(uid, target.RoomID, "switch_room");

            List<NetPeer> peers;
            lock (Program.LoggedInUsers) peers = new List<NetPeer>(Program.LoggedInUsers.Keys);
            foreach (var p in peers) Program.SendPush(p, CommonObjects.OpCode.RoomListChanged, "");

            return w;
        }
    }}








