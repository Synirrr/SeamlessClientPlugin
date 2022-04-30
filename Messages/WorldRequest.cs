using NLog;
using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.ObjectBuilders;

namespace SeamlessClientPlugin.Messages
{
    [ProtoContract]
    public class WorldRequest
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [ProtoMember(1)]
        public ulong PlayerID;
        [ProtoMember(2)]
        public long IdentityID;
        [ProtoMember(3)]
        public string PlayerName;
        [ProtoMember(4)]
        public byte[] WorldData;

        public WorldRequest(ulong PlayerID,long PlayerIdentity, string Name)
        {
            this.PlayerID = PlayerID;
            this.PlayerName = Name;
            this.IdentityID = PlayerIdentity;
        }

        public WorldRequest() { }

        public void SerializeWorldData(MyObjectBuilder_World WorldData)
        {
            MethodInfo CleanupData = typeof(MyMultiplayerServerBase).GetMethod("CleanUpData", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[3]
{
            typeof(MyObjectBuilder_World),
            typeof(ulong),
            typeof(long),
}, null);
            object[] Data = new object[] { WorldData, PlayerID, IdentityID };
            CleanupData.Invoke(null, Data);
            WorldData = (MyObjectBuilder_World)Data[0];
            using (MemoryStream memoryStream = new MemoryStream())
            {
                MyObjectBuilderSerializer.SerializeXML(memoryStream, WorldData, MyObjectBuilderSerializer.XmlCompression.Gzip);
                this.WorldData = memoryStream.ToArray();
                Log.Warn("Successfully Converted World");
            }
        }

        public MyObjectBuilder_World DeserializeWorldData()
        {
            MyObjectBuilderSerializer.DeserializeGZippedXML<MyObjectBuilder_World>(new MemoryStream(WorldData), out var objectBuilder);
            return objectBuilder;
        }

    }
}
