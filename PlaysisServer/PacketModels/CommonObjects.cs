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
            FetchModelUpload
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
        public void BroadcastPacket(NetDataWriter writer, List<PlayerObject> targets)
        {

        }
    }
}
