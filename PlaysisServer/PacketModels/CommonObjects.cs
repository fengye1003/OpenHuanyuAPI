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
            JoinRoom = 1,
            PlayerSpawn,
            PlayerMove,
            SpawnModel,
            SyncRoomState,
            Auth,
            ApiAvailabilityAuth,
            FetchModelUpload,
            GetPlayerNameByUid,
            RequestUploadModel,
            AddModelUrl
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

        public static void PlaceEOFPacket(NetDataWriter writer)
        {
            writer.Put((byte)PacketInternalSymbols.EOF);
        }
    }
}
