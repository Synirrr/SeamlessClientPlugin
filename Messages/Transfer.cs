using ProtoBuf;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.GameServices;
using VRage.Network;
using VRage.Steam;
using VRage.Utils;
using VRageMath;

namespace SeamlessClientPlugin.Messages
{

    [ProtoContract]
    public class Transfer
    {
        [ProtoMember(1)]
        public ulong TargetServerID;
        [ProtoMember(2)]
        public string IPAdress;
        [ProtoMember(6)]
        public WorldRequest WorldRequest;
        [ProtoMember(7)]
        public string PlayerName;

        [ProtoMember(8)]
        public List<MyObjectBuilder_Gps.Entry> PlayerGPSCoords;

        [ProtoMember(9)]
        public MyObjectBuilder_Toolbar PlayerToolbar;

        [ProtoMember(10)]
        public string ServerName;

        public Transfer(ulong ServerID, string IPAdress)
        {
            /*  This is only called serverside
             */

            this.IPAdress = IPAdress;
            TargetServerID = ServerID;
        }

        public Transfer() { }





    }
}
