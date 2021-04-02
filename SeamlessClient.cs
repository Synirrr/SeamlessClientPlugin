using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using SeamlessClientPlugin.ClientMessages;
using SeamlessClientPlugin.SeamlessTransfer;
using SeamlessClientPlugin.Updater;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using VRage.Input;
using VRage.Plugins;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace SeamlessClientPlugin
{

    //SendAllMembersDataToClient

    public class SeamlessClient : IPlugin
    {
        /*  First Step. How does a player join a game?
         *  First JoinGameInternal is called with the ServersLobbyID.
         *      In this method, MySessionLoader.UnloadAndExitToMenu() is called. Ultimatley we want to prevent this as we dont want to unload the entire game. Just the basic neccessities.
         *      Then JoinLobby is called. This looks to be simply a check to see if the client gets a result from the server. If it does, it initilizes a new STATIC/Multiplayer base by:  [Static = new MyMultiplayerLobbyClient(lobby, new MySyncLayer(new MyTransportLayer(2)))));]
         *      Once this above method is done and join is done, we begin the OnJoin()
         *      
         *  On join begins by checking join result. Success downloads world and failed sends you back to menu. (May need to use this to send players to menu)
         *      Download world Requires the multiplayerbase and MyGUIScreenProgress. Which is essentially checking that the user hasnt cliecked left or closed. 
         *      
         *      	StringBuilder text = MyTexts.Get(MyCommonTexts.DialogTextJoiningWorld);
         *          MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MyCommonTexts.Cancel);
         *          
         *          This just looks to be like what happens still with the little GUI popupscreen before load starts
         * 
         *          DownloadWorld also contains a method to get the serversessionstate too. We will need to check this before load. Ultimatley once everything has passed join checks Downloadworld is called. (MyMultiplayerClientBase.DownloadWorld)
         *          
         *          
         *    MyMultiplayerClientBase.DownloadWorld simply rasies a static event to the server for world request. [return MyMultiplayerServerBase.WorldRequest;]
         *          
         *          
         *          
         *          
         *    MyMultiplayerServerBase.WorldRequest (WorldRequest) This is near the start of where the magic happens. THIS IS ALL RAN SERVERSIDE
         *          Checks to see if the client has been kicked or banned to prevent world requests. Not sure we really need to worry about this.
         *          
         *          Server gets world clears non important data such as player gps. Our player gps gets added on join so we can yeet this.
         *          Also theres a sendfluch via transport layer. Might need to keep this in mind and use this for our testings
         *          Theres a CleanUpData that gets called with world and playerid/identity ID. This is probably to limit things and only send whats neccessary
         *              CLEANUPDATA shows me how to send allplayers data synced on the server.
         *          
         *          
         *          Once we have everythings we use a MS to serialize everything to byte[] and via replication layer we send world to client.
         *          
         *          
         *     BACK TO CLIENT:
         *     RecieveWorld is called and packet is deserialized and then MyJoinGameHelper.WorldReceived(...,...) is called
         *     
         *     WorldReceived does some checks for extra version mismatches and to make sure the CheckPointObjectBuilder isnt null etc.
         *     Once it passes all these checks, CheckDx11AndJoin(world, MyMutliplayerBase) is called
         *     
         *     CheckDx11AndJoin just checks if its a scenario world or not. Forget about this. We can figure that out all later. Then it runs: MySessionLoader.LoadMultiplayerSession(world, multiplayer);
         *     
         *     
         *     MySessionLoader.LoadMultiplayerSession looks to be the start of the join code. It also checks for mod mismatches. (And Downloads them). However once it passes this, MySession.LoadMultiplayer is called.
         *     
         *     
         *     
         *     
         *     
         *     
         *     
         *     MySession.LoadMultiplayer (The most important step in world loading)
         *          Creates new MySession.
         *          Does settings stuff.
         *          LoadMembersFromWorld (Loads Clients)
         *          FixSessionComponentObjectBuilders
         *          
         *          
         *          
         *          PrepareBaseSession is something we need. Looks like it does some weird stuff to init fonts, Sector Enviroment settings, Loading datacomponents from world, and re-initilizes modAPI stuff
         *          DeserializeClusters
         *          Loads planets.
         *          RegistersChat
         *          
         *          LOADWORLD -----------
         *          Static.BeforeStartComponents
         *              
         *          
         *          
         * 
         * -plugin "../Plugins/SeamlessClientPlugin.dll"
         */



        public static string Version = "1.2.12";
        public static bool Debug = false;


        private bool Initilized = false;
        private bool SentPingResponse = false;
        public const ushort SeamlessClientNetID = 2936;
        private System.Timers.Timer PingTimer = new System.Timers.Timer(3000);

        public static LoadServer Server = new LoadServer();


        public static bool IsSwitching = false;
        public static bool RanJoin = false;

     

        public static Action JoinAction = () => { };


        public void Dispose()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            MyAPIGateway.Multiplayer?.UnregisterMessageHandler(SeamlessClientNetID, MessageHandler);
#pragma warning restore CS0618 // Type or member is obsolete
            Initilized = false;
            SentPingResponse = false;
            PingTimer.Stop();
            //throw new NotImplementedException();
        }

        public void Init(object gameInstance)
        {
            TryShow("Running Seamless Client Plugin v[" + Version + "]");

            UpdateChecker Checker = new UpdateChecker(false);
            Task UpdateChecker = new Task(() => Checker.PingUpdateServer());
            UpdateChecker.Start();

           

           

            // Reload = new ReloadPatch();
            //Patching goes here


            PingTimer.Elapsed += PingTimer_Elapsed;
            PingTimer.Start();
            //throw new NotImplementedException();
        }

        

        private void PingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //TryShow("Sending PluginVersion to Server!");
            try
            {



                ClientMessage PingServer = new ClientMessage(ClientMessageType.FirstJoin);
                MyAPIGateway.Multiplayer?.SendMessageToServer(SeamlessClientNetID, Utilities.Utility.Serialize(PingServer));


                

            }
            catch (Exception ex)
            {
                TryShow(ex.ToString());
            }
        }


        public void Update()
        {
            if (MyAPIGateway.Multiplayer == null)
                return;

            if (!Initilized)
            {
                TryShow("Initilizing Communications!");
                RunInitilizations();
                Initilized = true;
                
            }
            //OnNewPlayerRequest
            //throw new NotImplementedException();
        }



        public static void RunInitilizations()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            MyAPIGateway.Multiplayer.RegisterMessageHandler(SeamlessClientNetID, MessageHandler);
#pragma warning restore CS0618 // Type or member is obsolete
            //We need to initiate ping request
        }

        public static void DisposeInitilizations()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(SeamlessClientNetID, MessageHandler);
        }


        private static void MessageHandler(byte[] bytes)
        {
            try
            {
                ClientMessage Recieved = Utilities.Utility.Deserialize<ClientMessage>(bytes);
                if (Recieved.MessageType == ClientMessageType.TransferServer)
                {
                    Transfer TransferMessage = Recieved.DeserializeData<Transfer>();
                    IsSwitching = true;
                    TransferMessage.PingServerAndBeginRedirect();
                    RanJoin = false;
                    //DisposeInitilizations();
                }
                else if (Recieved.MessageType == ClientMessageType.FirstJoin)
                {

                }
            }
            catch (Exception ex)
            {
                TryShow(ex.ToString());
            }
        }




        public static void TryShow(string message)
        {
            if (MySession.Static?.LocalHumanPlayer != null && Debug)
                MyAPIGateway.Utilities?.ShowMessage("NetworkClient", message);

            MyLog.Default?.WriteLineAndConsole($"SeamlessClient: {message}");
        }


        public static void RestartClientAfterUpdate()
        {
            try
            {
                TryShow("Restarting Client!");

                string exe = Assembly.GetEntryAssembly().Location;
                

                Process currentProcess = Process.GetCurrentProcess();

                string[] CommandArgs = Environment.GetCommandLineArgs();

                string NewCommandLine = "";
                for(int i = 1; i < CommandArgs.Length; i++)
                {
                    NewCommandLine += " "+ CommandArgs[i];
                }

                TryShow(NewCommandLine);

                Process.Start(exe, NewCommandLine);

               
                currentProcess.Kill();
            }
            catch (Exception ex)
            {
                TryShow("Restarting Client error!");
            }
        }
    }
}
