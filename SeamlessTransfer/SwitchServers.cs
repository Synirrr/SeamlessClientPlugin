using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using Sandbox.ModAPI;
using SeamlessClientPlugin.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.GameServices;
using VRage.Steam;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace SeamlessClientPlugin.SeamlessTransfer
{
    public class SwitchServers
    {
        public MyGameServerItem TargetServer { get; }
        public MyObjectBuilder_World TargetWorld { get; }


        public SwitchServers(MyGameServerItem TargetServer, MyObjectBuilder_World TargetWorld)
        {
            this.TargetServer = TargetServer;
            this.TargetWorld = TargetWorld;
        }


        public void BeginSwitch()
        {
            MySandboxGame.Static.Invoke(delegate
            {
                //Set camera controller to fixed spectator
                MySession.Static.SetCameraController(MyCameraControllerEnum.SpectatorFixed);
                UnloadCurrentServer();
                SetNewMultiplayerClient();



            }, "SeamlessClient");
        }


        private void SetNewMultiplayerClient()
        {
            // Following is called when the multiplayer is set successfully to target server
            Patches.OnJoinEvent += OnJoinEvent;


            MySandboxGame.Static.SessionCompatHelper.FixSessionComponentObjectBuilders(TargetWorld.Checkpoint, TargetWorld.Sector);

            // Create constructors
            var LayerInstance = Patches.TransportLayerConstructor.Invoke(new object[] { 2 });
            var SyncInstance = Patches.SyncLayerConstructor.Invoke(new object[] { LayerInstance });
            var instance = Patches.ClientConstructor.Invoke(new object[] { TargetServer, SyncInstance });


            MyMultiplayer.Static = Utility.CastToReflected(instance, Patches.ClientType);
            MyMultiplayer.Static.ExperimentalMode = true;

            // Set the new SyncLayer to the MySession.Static.SyncLayer
            Patches.MySessionLayer.SetValue(MySession.Static, MyMultiplayer.Static.SyncLayer);

            SeamlessClient.TryShow("Successfully set MyMultiplayer.Static");

           
            Sync.Clients.SetLocalSteamId(Sync.MyId, false, MyGameService.UserName);
            Sync.Players.RegisterEvents();





        }




        private void OnJoinEvent(object sender, VRage.Network.JoinResultMsg e)
        {
            ForceClientConnection();

            // Un-register the event
            Patches.OnJoinEvent -= OnJoinEvent;
        }



        private void ForceClientConnection()
        {
        

            MyHud.Chat.RegisterChat(MyMultiplayer.Static);

            MyMultiplayer.Static.OnSessionReady();

            LoadConnectedClients();
            LoadOnlinePlayers();
            SetWorldSettings();
            RemoveOldEntities();
            UpdateWorldGenerator();


            StartEntitySync();

            MyModAPIHelper.Initialize();
            // Allow the game to start proccessing incoming messages in the buffer
            MyMultiplayer.Static.StartProcessingClientMessages();
        }


        private void LoadOnlinePlayers()
        {
            //Get This players ID

            MyPlayer.PlayerId? savingPlayerId = new MyPlayer.PlayerId(Sync.MyId);
            if (!savingPlayerId.HasValue)
            {
                SeamlessClient.TryShow("SavingPlayerID is null! Creating Default!");
                savingPlayerId = new MyPlayer.PlayerId(Sync.MyId);
            }
            SeamlessClient.TryShow("Saving PlayerID: " + savingPlayerId.ToString());

            Sync.Players.LoadConnectedPlayers(TargetWorld.Checkpoint, savingPlayerId);
            Sync.Players.LoadControlledEntities(TargetWorld.Checkpoint.ControlledEntities, TargetWorld.Checkpoint.ControlledObject, savingPlayerId);
            /*
          

            SeamlessClient.TryShow("Saving PlayerID: " + savingPlayerId.ToString());



            foreach (KeyValuePair<MyObjectBuilder_Checkpoint.PlayerId, MyObjectBuilder_Player> item3 in TargetWorld.Checkpoint.AllPlayersData.Dictionary)
            {
                MyPlayer.PlayerId playerId5 = new MyPlayer.PlayerId(item3.Key.GetClientId(), item3.Key.SerialId);

                SeamlessClient.TryShow($"ConnectedPlayer: {playerId5.ToString()}");
                if (savingPlayerId.HasValue && playerId5.SteamId == savingPlayerId.Value.SteamId)
                {
                    playerId5 = new MyPlayer.PlayerId(Sync.MyId, playerId5.SerialId);
                }

                Patches.LoadPlayerInternal.Invoke(MySession.Static.Players, new object[] { playerId5, item3.Value, false });
                ConcurrentDictionary<MyPlayer.PlayerId, MyPlayer> Players = (ConcurrentDictionary<MyPlayer.PlayerId, MyPlayer>)Patches.MPlayerGPSCollection.GetValue(MySession.Static.Players);
                //LoadPlayerInternal(ref playerId5, item3.Value);
                if (Players.TryGetValue(playerId5, out MyPlayer myPlayer))
                {
                    List<Vector3> value2 = null;
                    if (TargetWorld.Checkpoint.AllPlayersColors != null && TargetWorld.Checkpoint.AllPlayersColors.Dictionary.TryGetValue(item3.Key, out value2))
                    {
                        myPlayer.SetBuildColorSlots(value2);
                    }
                    else if (TargetWorld.Checkpoint.CharacterToolbar != null && TargetWorld.Checkpoint.CharacterToolbar.ColorMaskHSVList != null && TargetWorld.Checkpoint.CharacterToolbar.ColorMaskHSVList.Count > 0)
                    {
                        myPlayer.SetBuildColorSlots(TargetWorld.Checkpoint.CharacterToolbar.ColorMaskHSVList);
                    }
                }
            }

            */

        }

        private void SetWorldSettings()
        {
            //MyEntities.MemoryLimitAddFailureReset();

            //Clear old list
            MySession.Static.PromotedUsers.Clear();
            MySession.Static.CreativeTools.Clear();
            Dictionary<ulong, AdminSettingsEnum> AdminSettingsList = (Dictionary<ulong, AdminSettingsEnum>)Patches.RemoteAdminSettings.GetValue(MySession.Static);
            AdminSettingsList.Clear();



            // Set new world settings
            MySession.Static.Name = MyStatControlText.SubstituteTexts(TargetWorld.Checkpoint.SessionName);
            MySession.Static.Description = TargetWorld.Checkpoint.Description;

            MySession.Static.Mods = TargetWorld.Checkpoint.Mods;
            MySession.Static.Settings = TargetWorld.Checkpoint.Settings;
            MySession.Static.CurrentPath = MyLocalCache.GetSessionSavesPath(MyUtils.StripInvalidChars(TargetWorld.Checkpoint.SessionName), contentFolder: false, createIfNotExists: false);
            MySession.Static.WorldBoundaries = TargetWorld.Checkpoint.WorldBoundaries;
            MySession.Static.InGameTime = MyObjectBuilder_Checkpoint.DEFAULT_DATE;
            MySession.Static.ElapsedGameTime = new TimeSpan(TargetWorld.Checkpoint.ElapsedGameTime);
            MySession.Static.Settings.EnableSpectator = false;

            MySession.Static.Password = TargetWorld.Checkpoint.Password;
            MySession.Static.PreviousEnvironmentHostility = TargetWorld.Checkpoint.PreviousEnvironmentHostility;
            MySession.Static.RequiresDX = TargetWorld.Checkpoint.RequiresDX;
            MySession.Static.CustomLoadingScreenImage = TargetWorld.Checkpoint.CustomLoadingScreenImage;
            MySession.Static.CustomLoadingScreenText = TargetWorld.Checkpoint.CustomLoadingScreenText;
            MySession.Static.CustomSkybox = TargetWorld.Checkpoint.CustomSkybox;


            MySession.Static.Gpss = new MyGpsCollection();
            MySession.Static.Gpss.LoadGpss(TargetWorld.Checkpoint);
            MyRenderProxy.RebuildCullingStructure();
            //MySession.Static.Toolbars.LoadToolbars(checkpoint);

            Sync.Players.RespawnComponent.InitFromCheckpoint(TargetWorld.Checkpoint);


            // Set new admin settings

            if (TargetWorld.Checkpoint.PromotedUsers != null)
            {
                MySession.Static.PromotedUsers = TargetWorld.Checkpoint.PromotedUsers.Dictionary;
            }
            else
            {
                MySession.Static.PromotedUsers = new Dictionary<ulong, MyPromoteLevel>();
            }




            foreach (KeyValuePair<MyObjectBuilder_Checkpoint.PlayerId, MyObjectBuilder_Player> item in TargetWorld.Checkpoint.AllPlayersData.Dictionary)
            {
                ulong clientId = item.Key.GetClientId();
                AdminSettingsEnum adminSettingsEnum = (AdminSettingsEnum)item.Value.RemoteAdminSettings;
                if (TargetWorld.Checkpoint.RemoteAdminSettings != null && TargetWorld.Checkpoint.RemoteAdminSettings.Dictionary.TryGetValue(clientId, out var value))
                {
                    adminSettingsEnum = (AdminSettingsEnum)value;
                }
                if (!MyPlatformGameSettings.IsIgnorePcuAllowed)
                {
                    adminSettingsEnum &= ~AdminSettingsEnum.IgnorePcu;
                    adminSettingsEnum &= ~AdminSettingsEnum.KeepOriginalOwnershipOnPaste;
                }


                AdminSettingsList[clientId] = adminSettingsEnum;
                if (!Sync.IsDedicated && clientId == Sync.MyId)
                {
                    Patches.AdminSettings.SetValue(MySession.Static, adminSettingsEnum);

                    //m_adminSettings = adminSettingsEnum;
                }



                if (!MySession.Static.PromotedUsers.TryGetValue(clientId, out var value2))
                {
                    value2 = MyPromoteLevel.None;
                }
                if (item.Value.PromoteLevel > value2)
                {
                    MySession.Static.PromotedUsers[clientId] = item.Value.PromoteLevel;
                }
                if (!MySession.Static.CreativeTools.Contains(clientId) && item.Value.CreativeToolsEnabled)
                {
                    MySession.Static.CreativeTools.Add(clientId);
                }
            }




        }

        private void LoadConnectedClients()
        {


            Patches.LoadMembersFromWorld.Invoke(MySession.Static, new object[] { TargetWorld, MyMultiplayer.Static });


            //Re-Initilize Virtual clients
            object VirtualClientsValue = Patches.VirtualClients.GetValue(MySession.Static);
            Patches.InitVirtualClients.Invoke(VirtualClientsValue, null);

            /*
            SeamlessClient.TryShow("Loading exsisting Members From World!");
            foreach (var Client in TargetWorld.Checkpoint.Clients)
            {
                SeamlessClient.TryShow("Adding New Client: " + Client.Name);
                Sync.Clients.AddClient(Client.SteamId, Client.Name);
            }*/

        }

        private void StartEntitySync()
        {
            SeamlessClient.TryShow("Requesting Player From Server");
            Sync.Players.RequestNewPlayer(Sync.MyId, 0, MyGameService.UserName, null, realPlayer: true, initialPlayer: true);
            if (MySession.Static.ControlledEntity == null && Sync.IsServer && !Sandbox.Engine.Platform.Game.IsDedicated)
            {
                MyLog.Default.WriteLine("ControlledObject was null, respawning character");
                //m_cameraAwaitingEntity = true;
                MyPlayerCollection.RequestLocalRespawn();
            }

            //Request client state batch
            (MyMultiplayer.Static as MyMultiplayerClientBase).RequestBatchConfirmation();
            MyMultiplayer.Static.PendingReplicablesDone += MyMultiplayer_PendingReplicablesDone;
            //typeof(MyGuiScreenTerminal).GetMethod("CreateTabs")

            MySession.Static.LoadDataComponents();
            //MyGuiSandbox.LoadData(false);
            //MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.HUDScreen));
            MyRenderProxy.RebuildCullingStructure();
            MyRenderProxy.CollectGarbage();

            SeamlessClient.TryShow("OnlinePlayers: " + MySession.Static.Players.GetOnlinePlayers().Count);
            SeamlessClient.TryShow("Loading Complete!");

         
            //Recreate all controls... Will fix weird gui/paint/crap
            MyGuiScreenHudSpace.Static.RecreateControls(true);
        }

        private void MyMultiplayer_PendingReplicablesDone()
        {
            if (MySession.Static.VoxelMaps.Instances.Count > 0)
            {
                MySandboxGame.AreClipmapsReady = false;
            }
            MyMultiplayer.Static.PendingReplicablesDone -= MyMultiplayer_PendingReplicablesDone;
        }

        private void UpdateWorldGenerator()
        {
            //This will re-init the MyProceduralWorldGenerator. (Not doing this will result in asteroids not rendering in properly)


           //This shoud never be null
           var Generator = MySession.Static.GetComponent<MyProceduralWorldGenerator>();

            //Force component to unload
            Patches.UnloadProceduralWorldGenerator.Invoke(Generator, null);

            //Re-call the generator init
            MyObjectBuilder_WorldGenerator GeneratorSettings = (MyObjectBuilder_WorldGenerator)TargetWorld.Checkpoint.SessionComponents.FirstOrDefault(x => x.GetType() == typeof(MyObjectBuilder_WorldGenerator));
            if (GeneratorSettings != null)
            {
                //Re-initilized this component (forces to update asteroid areas like not in planets etc)
                Generator.Init(GeneratorSettings);
            }



            //Force component to reload, re-syncing settings and seeds to the destination server
            Generator.LoadData();


            //We need to go in and force planets to be empty areas in the generator. This is originially done on planet init.
            FieldInfo PlanetInitArgs = typeof(MyPlanet).GetField("m_planetInitValues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var Planet in MyEntities.GetEntities().OfType<MyPlanet>())
            {
                MyPlanetInitArguments args = (MyPlanetInitArguments)PlanetInitArgs.GetValue(Planet);

                float MaxRadius = args.MaxRadius;

                Generator.MarkEmptyArea(Planet.PositionComp.GetPosition(), MaxRadius);
            }
        }

        private void UnloadCurrentServer()
        {
            //Unload current session on game thread
            if (MyMultiplayer.Static == null)
                throw new Exception("MyMultiplayer.Static is null on unloading? dafuq?");


            //Clear all old players and clients.
            Sync.Clients.Clear();
            Sync.Players.ClearPlayers();


            MyHud.Chat.UnregisterChat(MyMultiplayer.Static);
            MySession.Static.Gpss.RemovePlayerGpss(MySession.Static.LocalPlayerId);
            MyHud.GpsMarkers.Clear();
            MyMultiplayer.Static.ReplicationLayer.Disconnect();
            MyMultiplayer.Static.ReplicationLayer.Dispose();
            MyMultiplayer.Static.Dispose();
            MyMultiplayer.Static = null;
        }

        private void RemoveOldEntities()
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
