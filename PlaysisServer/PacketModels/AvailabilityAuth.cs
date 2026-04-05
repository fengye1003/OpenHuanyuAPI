using LiteNetLib;
using LiteNetLib.Utils;
using PlaysisServer.Essencial_Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaysisServer.PacketModels
{
    partial class Models
    {
        public static NetDataWriter ApiAvailabilityAuth(NetPeer peer, NetPacketReader reader, int requestId)
        {
            //Log.SaveLog("Received");
            var writer = new NetDataWriter();
            writer.Put(requestId);
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
