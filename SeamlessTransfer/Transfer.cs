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
using SeamlessClientPlugin.ClientMessages;
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

namespace SeamlessClientPlugin.SeamlessTransfer
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

        public List<Vector3> PlayerBuildSlots;

        public Transfer(ulong ServerID, string IPAdress)
        {
            /*  This is only called serverside
             */

            this.IPAdress = IPAdress;
            TargetServerID = ServerID;
        }

        public Transfer() { }



        public void PingServerAndBeginRedirect()
        {
            if (TargetServerID == 0)
            {
                SeamlessClient.TryShow("This is not a valid server!");
                return;
            }

            SeamlessClient.TryShow("SyncMyID: " + Sync.MyId.ToString());
            SeamlessClient.TryShow("Beginning Redirect to server: " + TargetServerID);
            MyGameService.OnPingServerResponded += MyGameService_OnPingServerResponded;
            MyGameService.OnPingServerFailedToRespond += MyGameService_OnPingServerFailedToRespond;

            MyGameService.PingServer(IPAdress);
        }

        private void MyGameService_OnPingServerFailedToRespond(object sender, EventArgs e)
        {
            MyGameService.OnPingServerResponded -= MyGameService_OnPingServerResponded;
            MyGameService.OnPingServerFailedToRespond -= MyGameService_OnPingServerFailedToRespond;
            SeamlessClient.TryShow("ServerPing failed!");

        }

        private void MyGameService_OnPingServerResponded(object sender, MyGameServerItem e)
        {
           

            MyGameService.OnPingServerResponded -= MyGameService_OnPingServerResponded;
            MyGameService.OnPingServerFailedToRespond -= MyGameService_OnPingServerFailedToRespond;
            SeamlessClient.TryShow("ServerPing Successful! Attempting to connect to lobby: " + e.GameID);


            LoadServer.LoadWorldData(e, WorldRequest.DeserializeWorldData());


            MySandboxGame.Static.Invoke(delegate
            {
                StringBuilder Builder = new StringBuilder();
                Builder.AppendLine("Please be patient! Some users can spend a minute switching servers... others may be able to swtich faster. Lots of factors to consider.");
                Builder.AppendLine();
                Builder.AppendLine("Sitting in spectator is perfectly normal for a few seconds!");

                MyAPIGateway.Utilities.ShowMissionScreen("Switching Servers!",null, null, Builder.ToString(), null, "Ok!");
                //MySessionLoader.UnloadAndExitToMenu();
                UnloadCurrentServer();

                //MyJoinGameHelper.JoinGame(e, true);
                LoadServer.ResetMPClient();

                ClearEntities();
                //ReloadPatch.SeamlessSwitch = false;
            }, "SeamlessClient");
            return;
            

            

        }





        private void UnloadCurrentServer()
        {
            if (MyMultiplayer.Static != null)
            {
                Sync.Clients.Clear();
                Sync.Players.ClearPlayers();

                MyHud.Chat.UnregisterChat(MyMultiplayer.Static);
                //OnPlayerCreated
                //OnConnectedClient

                try
                {
                    //Could throw error when there are no gps points
                    var PCollection = MySession.Static.Gpss[MySession.Static.LocalPlayerId];
                    PCollection?.Clear();
                    SeamlessClient.TryShow(PCollection.Count + "registered GPS points");
                }
                catch(Exception ex)
                {
                    SeamlessClient.TryShow(ex.ToString());
                }


                MyHud.GpsMarkers.Clear();


                MyMultiplayer.Static.ReplicationLayer.Disconnect();
                MyMultiplayer.Static.ReplicationLayer.Dispose();

                MyMultiplayer.Static.Dispose();
                MyMultiplayer.Static = null;

                //Sync.Clients.Clear();

                
                

                
                // MyGuiSandbox.UnloadContent();
            }
        }
        

        private void ClearEntities()
        {
            foreach (var ent in MyEntities.GetEntities())
            {
                if (ent is MyPlanet)
                    continue;

                ent.Close();
            }
        }

    }
}
