using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PlaysisServer.PacketModels
{
    /// <summary>
    /// version.3.30.01.net
    /// </summary>
    internal class CommonObjects
    {
        public enum OpCode : byte
        {
            ReSyncPackets,
            JoinRoom,
            PlayerSpawn,
            PlayerMove,
            SpawnModel,
            SyncRoomPlayerState,
            SyncRoomModelsState,
            SyncRoomNoticeState,
            Auth,
            ApiAvailabilityAuth,
            FetchModelInfo,
            GetPlayerNameByUid,
            RequestUploadModel,
            AddModelUrl,
            CheckUidWhetherAdmin,
            FetchRoomList, //
            SwitchRoom, //
            AdminRegister
        }

        public enum PacketInternalSymbols : byte
        {
            EOF,
            PlayerPacket,
            ModelPacket,
            NoticePacket
        }

        public static Vector3 GetVector3(NetPacketReader reader)
        {
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();
            return new Vector3(x, y, z);
        }
        public static void PutVector3(NetDataWriter writer, Vector3 value)
        {
            writer.Put(value.X);
            writer.Put(value.Y);
            writer.Put(value.Z);
        }

        public static void PlacePlayerPacket(NetDataWriter writer, PlayerObject player)
        {
            writer.Put((byte)PacketInternalSymbols.PlayerPacket);
            writer.Put(player.UID);
            CommonObjects.PutVector3(writer, player.Location);
            //Console.WriteLine(player.Location.X);
            //Console.WriteLine(player.Location.Y);
            //Console.WriteLine(player.Location.Z);
            CommonObjects.PutVector3(writer, player.Scale);
            CommonObjects.PutVector3(writer, player.Rotation);
        }
        public static PlayerObject FetchPlayerPacket(NetPacketReader reader)
        {
            var uid = reader.GetInt();
            PlayerObject result = new(uid, null, null);
            
            result.Location = CommonObjects.GetVector3(reader);
            result.Scale = CommonObjects.GetVector3(reader);
            result.Rotation = CommonObjects.GetVector3(reader);
            return result;
        }

        public static void PlaceModelPacket(NetDataWriter writer, ModelObject model)
        {
            writer.Put((byte)PacketInternalSymbols.ModelPacket);
            writer.Put(model.ParentPlayer.UID);
            writer.Put(model.ModelHash);
            CommonObjects.PutVector3(writer, model.Location);
            CommonObjects.PutVector3(writer, model.Scale);
            CommonObjects.PutVector3(writer, model.Rotation);
        }

        public static ModelObject FetchModelPacket(NetPacketReader reader)
        {
            var uid = reader.GetInt();
            var hash = reader.GetString();
            ModelObject model = new(new PlayerObject(uid, null, null), null, null, hash);
            model.Location = CommonObjects.GetVector3(reader);
            model.Scale = CommonObjects.GetVector3(reader);
            model.Rotation = CommonObjects.GetVector3(reader);
            return model;
        }

        public static void PlaceEOFPacket(NetDataWriter writer)
        {
            writer.Put((byte)PacketInternalSymbols.EOF);
        }
    }
}
