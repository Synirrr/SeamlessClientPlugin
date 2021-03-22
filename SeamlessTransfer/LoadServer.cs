using HarmonyLib;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Engine.Analytics;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using SeamlessClientPlugin.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.SessionComponents;
using VRage.Game.Voxels;
using VRage.GameServices;
using VRage.Network;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace SeamlessClientPlugin.SeamlessTransfer
{
    public class LoadServer
    {
        //Protected or internal class types
        private static readonly Type ClientType = Type.GetType("Sandbox.Engine.Multiplayer.MyMultiplayerClient, Sandbox.Game");
        private static readonly Type SyncLayerType = Type.GetType("Sandbox.Game.Multiplayer.MySyncLayer, Sandbox.Game");
        private static readonly Type MyTransportLayerType = Type.GetType("Sandbox.Engine.Multiplayer.MyTransportLayer, Sandbox.Game");
        private static readonly Type MySessionType = Type.GetType("Sandbox.Game.World.MySession, Sandbox.Game");
        private static readonly Type VirtualClientsType = Type.GetType("Sandbox.Engine.Multiplayer.MyVirtualClients, Sandbox.Game");
        private static readonly Type GUIScreenChat = Type.GetType("Sandbox.Game.Gui.MyGuiScreenChat, Sandbox.Game");

        private static Harmony Patcher = new Harmony("SeamlessClientReUnload");
        private static MyGameServerItem Server;
        private static MyObjectBuilder_World World;

        public static object MyMulitplayerClient;



        public static ConstructorInfo ClientConstructor;
        public static ConstructorInfo SyncLayerConstructor;
        public static ConstructorInfo TransportLayerConstructor;
        public static ConstructorInfo MySessionConstructor;

        //Reflected Methods
        public static FieldInfo VirtualClients;
        public static FieldInfo AdminSettings;

        public static FieldInfo RemoteAdminSettings;
        public static FieldInfo MPlayerGPSCollection;
        public static MethodInfo RemovePlayerFromDictionary;
        public static MethodInfo InitVirtualClients;
        public static MethodInfo LoadPlayerInternal;
        public static MethodInfo LoadMembersFromWorld;

        public static MethodInfo LoadMultiplayer;


        public LoadServer()
        {
            InitiatePatches();




            //TargetWorld = World;
            //Server = e;


        }



        public void InitiatePatches()
        {
            //Patch the on connection event
            MethodInfo OnJoin = ClientType.GetMethod("OnUserJoined", BindingFlags.NonPublic | BindingFlags.Instance);
            Patcher.Patch(OnJoin, postfix: new HarmonyMethod(GetPatchMethod(nameof(OnUserJoined))));


            ClientConstructor = ClientType?.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[2] { typeof(MyGameServerItem), SyncLayerType }, null);
            SyncLayerConstructor = SyncLayerType?.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[1] { MyTransportLayerType }, null);
            TransportLayerConstructor = MyTransportLayerType?.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[1] { typeof(int) }, null);
            MySessionConstructor = MySessionType?.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[2] { typeof(MySyncLayer), typeof(bool) }, null);

            if (ClientConstructor == null)
            {
                throw new InvalidOperationException("Couldn't find ClientConstructor");
            }

            if (SyncLayerConstructor == null)
            {
                throw new InvalidOperationException("Couldn't find SyncLayerConstructor");
            }

            if (TransportLayerConstructor == null)
            {
                throw new InvalidOperationException("Couldn't find TransportLayerConstructor");
            }

            if (MySessionConstructor == null)
            {
                throw new InvalidOperationException("Couldn't find MySessionConstructor");
            }

            RemovePlayerFromDictionary = typeof(MyPlayerCollection).GetMethod("RemovePlayerFromDictionary", BindingFlags.Instance | BindingFlags.NonPublic);
            VirtualClients = typeof(MySession).GetField("VirtualClients", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            InitVirtualClients = VirtualClientsType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
            LoadPlayerInternal = typeof(MyPlayerCollection).GetMethod("LoadPlayerInternal", BindingFlags.Instance | BindingFlags.NonPublic);
            LoadMembersFromWorld = typeof(MySession).GetMethod("LoadMembersFromWorld", BindingFlags.NonPublic | BindingFlags.Instance);
            AdminSettings = typeof(MySession).GetField("m_adminSettings", BindingFlags.Instance | BindingFlags.NonPublic);
            RemoteAdminSettings = typeof(MySession).GetField("m_remoteAdminSettings", BindingFlags.Instance | BindingFlags.NonPublic);
            MPlayerGPSCollection = typeof(MyPlayerCollection).GetField("m_players", BindingFlags.Instance | BindingFlags.NonPublic);
            LoadMultiplayer = typeof(MySession).GetMethod("LoadMultiplayer", BindingFlags.Static | BindingFlags.NonPublic);

           
            MethodInfo LoadingAction = typeof(MySessionLoader).GetMethod("LoadMultiplayerSession", BindingFlags.Public | BindingFlags.Static);
            Patcher.Patch(LoadingAction, prefix: new HarmonyMethod(GetPatchMethod(nameof(LoadMultiplayerSession))));

        }


        private static MethodInfo GetPatchMethod(string v)
        {
            return typeof(LoadServer).GetMethod(v, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }



        private static bool GetCustomLoadingScreenPath(List<MyObjectBuilder_Checkpoint.ModItem> Mods, out string File)
        {
            File = null;
            string WorkshopDir = MyFileSystem.ModsPath;
            SeamlessClient.TryShow(WorkshopDir);
            try
            {
                SeamlessClient.TryShow("Installed Mods: " + Mods);
                foreach (var Mod in Mods)
                {
                    string SearchDir = Mod.GetPath();

                    if (!Directory.Exists(SearchDir))
                        continue;

                    var files = Directory.GetFiles(SearchDir, "*.dds", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        if (Path.GetFileNameWithoutExtension(file) == "CustomLoadingBackground")
                        {
                            SeamlessClient.TryShow(Mod.FriendlyName + " contains a custom loading background!");
                            File = file;
                            return true;
                        }
                    }
                }

            }catch(Exception ex)
            {
                SeamlessClient.TryShow(ex.ToString());
            }

            SeamlessClient.TryShow("No installed custom loading screen!");
            return false;
        }



        private static bool LoadMultiplayerSession(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            MyLog.Default.WriteLine("LoadSession() - Start");
            if (!MyWorkshop.CheckLocalModsAllowed(world.Checkpoint.Mods, allowLocalMods: false))
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.OK, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer)));
                MyLog.Default.WriteLine("LoadSession() - End");
                return false;
            }
            MyWorkshop.DownloadModsAsync(world.Checkpoint.Mods, delegate (bool success)
            {
                if (success)
                {
                    MyScreenManager.CloseAllScreensNowExcept(null);
                    MyGuiSandbox.Update(16);
                    if (MySession.Static != null)
                    {
                        MySession.Static.Unload();
                        MySession.Static = null;
                    }

                    string CustomBackgroundImage = null;
                    GetCustomLoadingScreenPath(world.Checkpoint.Mods, out CustomBackgroundImage);

                    MySessionLoader.StartLoading(delegate
                    {
                        
                        LoadMultiplayer.Invoke(null, new object[] { world, multiplayerSession });
                        //MySession.LoadMultiplayer(world, multiplayerSession);
                    }, null, CustomBackgroundImage, null);
                }
                else
                {
                    multiplayerSession.Dispose();
                    MySessionLoader.UnloadAndExitToMenu();
                    if (MyGameService.IsOnline)
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.OK, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed)));
                    }
                    else
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.OK, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), messageText: new StringBuilder(string.Format(MyTexts.GetString(MyCommonTexts.DialogTextDownloadModsFailedSteamOffline), MySession.GameServiceName))));
                    }
                }
                MyLog.Default.WriteLine("LoadSession() - End");
            }, delegate
            {
                multiplayerSession.Dispose();
                MySessionLoader.UnloadAndExitToMenu();
            });

            return false;
        }





        private static void OnUserJoined(ref JoinResultMsg msg)
        {
            if (SeamlessClient.IsSwitching && msg.JoinResult == JoinResult.OK)
            {
                SeamlessClient.TryShow("User Joined! Result: "+msg.JoinResult.ToString());
                ForceClientConnection();
            }else if (SeamlessClient.IsSwitching && msg.JoinResult != JoinResult.OK)
            {
                SeamlessClient.TryShow("Failed to join server! Reason: " + msg.JoinResult.ToString());
                MySession.Static.Unload();
            }
        }



        public static void LoadWorldData(MyGameServerItem TargetServer, MyObjectBuilder_World TargetWorld)
        {
            Server = TargetServer;
            World = TargetWorld;
        }


        public static void ResetMPClient()
        {
            try
            {
                

                MySandboxGame.Static.SessionCompatHelper.FixSessionComponentObjectBuilders(World.Checkpoint, World.Sector);


                var LayerInstance = TransportLayerConstructor.Invoke(new object[] { 2 });
                var SyncInstance = SyncLayerConstructor.Invoke(new object[] { LayerInstance });
                var instance = ClientConstructor.Invoke(new object[] { Server, SyncInstance });
                MyMulitplayerClient = instance;


                

                MyMultiplayer.Static = (MyMultiplayerBase)instance;
                MyMultiplayer.Static.ExperimentalMode = MySandboxGame.Config.ExperimentalMode;
                SeamlessClient.TryShow("Successfully set MyMultiplayer.Static");
                //var m = ClientType.GetMethod("SendPlayerData", BindingFlags.Public | BindingFlags.Instance);
                //m.Invoke(MyMultiplayer.Static, new object[] { MyGameService.UserName });
                Server.GetGameTagByPrefix("gamemode");
                //typeof(MySession).GetMethod("LoadMembersFromWorld", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(MySession.Static, new object[] { LoadServer.TargetWorld, MyMultiplayer.Static });


                //MyScreenManager.CloseScreen(GUIScreenChat);
                MyHud.Chat.RegisterChat(MyMultiplayer.Static);

            }
            catch (Exception ex)
            {
                SeamlessClient.TryShow("Error! " + ex.ToString());
            }
        }

        public static void LoadMP(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            SeamlessClient.TryShow("Starting LoadMP!");

   
            //var MySessionConstructor = MySessionType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[2] { typeof(MySyncLayer), typeof(bool) }, null);
            //MySession.Static = (MySession)MySessionConstructor.Invoke(new object[] { MyMultiplayer.Static.SyncLayer, true });
            MySession.Static.Mods = World.Checkpoint.Mods;
            MySession.Static.Settings = World.Checkpoint.Settings;
            MySession.Static.CurrentPath = MyLocalCache.GetSessionSavesPath(MyUtils.StripInvalidChars(world.Checkpoint.SessionName), contentFolder: false, createIfNotExists: false);
            MySession.Static.WorldBoundaries = world.Checkpoint.WorldBoundaries;
            MySession.Static.InGameTime = MyObjectBuilder_Checkpoint.DEFAULT_DATE;



           // MySession.Static.Players.LoadConnectedPlayers(world.Checkpoint);
            
            //typeof(MySession).GetMethod("PrepareBaseSession", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(MyObjectBuilder_Checkpoint), typeof(MyObjectBuilder_Sector) }, null).Invoke(MySession.Static, new object[] { world.Checkpoint, world.Sector });

            if (MyFakes.MP_SYNC_CLUSTERTREE)
            {
                SeamlessClient.TryShow("Deserializing Clusters!");
                //MyPhysics.DeserializeClusters(world.Clusters);
            }



            //_ = world.Checkpoint.ControlledObject;
            //world.Checkpoint.ControlledObject = -1L;
            LoadOnlinePlayers(world.Checkpoint);
            LoadWorld(world.Checkpoint, world.Sector);
            SeamlessClient.TryShow("Loading World Complete!");
        }



        private static void LoadWorld(MyObjectBuilder_Checkpoint checkpoint, MyObjectBuilder_Sector sector)
        {

            Dictionary<ulong, AdminSettingsEnum> AdminSettingsList = (Dictionary<ulong, AdminSettingsEnum>)RemoteAdminSettings.GetValue(MySession.Static);
            AdminSettingsList.Clear();

            MySession.Static.PromotedUsers.Clear();
            MySession.Static.CreativeTools.Clear();

            MyEntities.MemoryLimitAddFailureReset();
            MySession.Static.ElapsedGameTime = new TimeSpan(checkpoint.ElapsedGameTime);
            MySession.Static.InGameTime = checkpoint.InGameTime;
            MySession.Static.Name = MyStatControlText.SubstituteTexts(checkpoint.SessionName);
            MySession.Static.Description = checkpoint.Description;


            if (checkpoint.PromotedUsers != null)
            {
                MySession.Static.PromotedUsers = checkpoint.PromotedUsers.Dictionary;
            }
            else
            {
                MySession.Static.PromotedUsers = new Dictionary<ulong, MyPromoteLevel>();
            }




            foreach (KeyValuePair<MyObjectBuilder_Checkpoint.PlayerId, MyObjectBuilder_Player> item in checkpoint.AllPlayersData.Dictionary)
            {
                ulong clientId = item.Key.GetClientId();
                AdminSettingsEnum adminSettingsEnum = (AdminSettingsEnum)item.Value.RemoteAdminSettings;
                if (checkpoint.RemoteAdminSettings != null && checkpoint.RemoteAdminSettings.Dictionary.TryGetValue(clientId, out var value))
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
                    AdminSettings.SetValue(MySession.Static, adminSettingsEnum);

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
        

            //MySession.Static.WorkshopId = checkpoint.WorkshopId;
            MySession.Static.Password = checkpoint.Password;
            MySession.Static.PreviousEnvironmentHostility = checkpoint.PreviousEnvironmentHostility;
            MySession.Static.RequiresDX = checkpoint.RequiresDX;
            MySession.Static.CustomLoadingScreenImage = checkpoint.CustomLoadingScreenImage;
            MySession.Static.CustomLoadingScreenText = checkpoint.CustomLoadingScreenText;
            MySession.Static.CustomSkybox = checkpoint.CustomSkybox;
            //FixIncorrectSettings(Settings);
            // MySession.Static.AppVersionFromSave = checkpoint.AppVersion;
            //MyToolbarComponent.InitCharacterToolbar(checkpoint.CharacterToolbar);
            //LoadCameraControllerSettings(checkpoint);





            SeamlessClient.TryShow("LocalPlayerID: " + MySession.Static.LocalPlayerId);
            //checkpoint.Gps.Dictionary.TryGetValue(MySession.Static.LocalPlayerId, out MyObjectBuilder_Gps GPSCollection);
            //SeamlessClient.TryShow("You have " + GPSCollection.Entries.Count + " gps points!");



            MySession.Static.Gpss = new MyGpsCollection();
            MySession.Static.Gpss.LoadGpss(checkpoint);
            MyRenderProxy.RebuildCullingStructure();
            //MySession.Static.Toolbars.LoadToolbars(checkpoint);

            Sync.Players.RespawnComponent.InitFromCheckpoint(checkpoint);
        }







        private static void ForceClientConnection()
        {
            SeamlessClient.IsSwitching = false;

            try
            {

                try
                {
                    if (MyMultiplayer.Static == null)
                        SeamlessClient.TryShow("MyMultiplayer.Static is null");

                    if (World == null)
                        SeamlessClient.TryShow("TargetWorld is null");

                    LoadClients();

                    LoadMP(World, MyMultiplayer.Static);


                }catch(Exception ex)
                {
                    SeamlessClient.TryShow(ex.ToString());

                }


                SeamlessClient.TryShow("Requesting Player From Server");
                Sync.Players.RequestNewPlayer(Sync.MyId, 0, MyGameService.UserName, null, realPlayer: true, initialPlayer: true);
                if (MySession.Static.ControlledEntity == null && Sync.IsServer && !Sandbox.Engine.Platform.Game.IsDedicated)
                {
                    SeamlessClient.TryShow("C");
                    MyLog.Default.WriteLine("ControlledObject was null, respawning character");
                    //m_cameraAwaitingEntity = true;
                    MyPlayerCollection.RequestLocalRespawn();
                }





                //typeof(MyGuiScreenTerminal).GetMethod("CreateTabs")
                MyMultiplayer.Static.OnSessionReady();
                MySession.Static.LoadDataComponents();
                //MyGuiSandbox.LoadData(false);
                //MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.HUDScreen));
                MyRenderProxy.RebuildCullingStructure();
                MyRenderProxy.CollectGarbage();

                SeamlessClient.TryShow("OnlinePlayers: " + MySession.Static.Players.GetOnlinePlayers().Count);
                SeamlessClient.TryShow("Loading Complete!");

            }
            catch (Exception Ex)
            {
                SeamlessClient.TryShow(Ex.ToString());
            }







        }

       

        private static void LoadClients()
        {


            try
            {
                //Remove all old players
                foreach (var Client in MySession.Static.Players.GetOnlinePlayers())
                {
                    if (Client.Id.SteamId == Sync.MyId)
                        continue;

                    SeamlessClient.TryShow("Disconnecting: " + Client.DisplayName);
                    RemovePlayerFromDictionary.Invoke(MySession.Static.Players, new object[] { Client.Id });
                }

                //Clear all exsisting clients
                foreach (var Client in Sync.Clients.GetClients().ToList())
                {
                    if (Client.SteamUserId == Sync.MyId)
                        continue;

                    Sync.Clients.RemoveClient(Client.SteamUserId);
                }


                object VirtualClientsValue = VirtualClients.GetValue(MySession.Static);

                //Re-Initilize Virtual clients
                SeamlessClient.TryShow("Initilizing Virtual Clients!");
                InitVirtualClients.Invoke(VirtualClientsValue, null);


                //Load Members from world
                SeamlessClient.TryShow("Loading Members From World!");
                LoadMembersFromWorld.Invoke(MySession.Static, new object[] { World, MyMulitplayerClient });
                foreach (var Client in World.Checkpoint.Clients)
                {
                    SeamlessClient.TryShow("Adding New Client: " + Client.Name);
                    Sync.Clients.AddClient(Client.SteamId, Client.Name);
                }




            }
            catch (Exception ex)
            {
                SeamlessClient.TryShow(ex.ToString());
            }


        }

        private static void LoadOnlinePlayers(MyObjectBuilder_Checkpoint checkpoint)
        {
            //Get This players ID
            MyPlayer.PlayerId? savingPlayerId = new MyPlayer.PlayerId(Sync.MyId);
            if (!savingPlayerId.HasValue)
            {
                SeamlessClient.TryShow("SavingPlayerID is null! Creating Default!");
                savingPlayerId = new MyPlayer.PlayerId(Sync.MyId);
            }

            SeamlessClient.TryShow("Saving PlayerID: "+savingPlayerId.ToString());


            SeamlessClient.TryShow("Checkpoint.AllPlayers: " + checkpoint.AllPlayers.Count);
            //These both are null/empty. Server doesnt need to send them to the client
            //SeamlessClient.TryShow("Checkpoint.ConnectedPlayers: " + checkpoint.ConnectedPlayers.Dictionary.Count);
            //SeamlessClient.TryShow("Checkpoint.DisconnectedPlayers: " + checkpoint.DisconnectedPlayers.Dictionary.Count);
            SeamlessClient.TryShow("Checkpoint.AllPlayersData: " + checkpoint.AllPlayersData.Dictionary.Count);


            foreach (KeyValuePair<MyObjectBuilder_Checkpoint.PlayerId, MyObjectBuilder_Player> item3 in checkpoint.AllPlayersData.Dictionary)
            {
                MyPlayer.PlayerId playerId5 = new MyPlayer.PlayerId(item3.Key.GetClientId(), item3.Key.SerialId);
                if (savingPlayerId.HasValue && playerId5.SteamId == savingPlayerId.Value.SteamId)
                {
                    playerId5 = new MyPlayer.PlayerId(Sync.MyId, playerId5.SerialId);

                }

                LoadPlayerInternal.Invoke(MySession.Static.Players, new object[] { playerId5, item3.Value, false });
                ConcurrentDictionary<MyPlayer.PlayerId, MyPlayer> Players = (ConcurrentDictionary<MyPlayer.PlayerId, MyPlayer>)MPlayerGPSCollection.GetValue(MySession.Static.Players);
                //LoadPlayerInternal(ref playerId5, item3.Value);
                if (Players.TryGetValue(playerId5, out MyPlayer myPlayer))
                {
                    List<Vector3> value2 = null;
                    if (checkpoint.AllPlayersColors != null && checkpoint.AllPlayersColors.Dictionary.TryGetValue(item3.Key, out value2))
                    {
                        myPlayer.SetBuildColorSlots(value2);
                    }
                    else if (checkpoint.CharacterToolbar != null && checkpoint.CharacterToolbar.ColorMaskHSVList != null && checkpoint.CharacterToolbar.ColorMaskHSVList.Count > 0)
                    {
                        myPlayer.SetBuildColorSlots(checkpoint.CharacterToolbar.ColorMaskHSVList);
                    }
                }
            }
        }


        private static void UpdatePlayerData()
        {

        }

    }





}
