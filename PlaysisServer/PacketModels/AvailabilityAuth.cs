using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public static NetDataWriter ApiAvailabilityAuth(NetPeer peer, NetPacketReader reader)
        {
            var writer = new NetDataWriter();
            var ver = reader.GetInt();
            if (ver != Program.AppInfo.protocolLevel)
            {
                writer.Put(0);
            }
            else
            {
                writer.Put(1);
            }
            return writer;
        }
    }
}
