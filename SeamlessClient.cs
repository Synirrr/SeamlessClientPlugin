﻿using Sandbox.Engine.Multiplayer;
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using VRage.Game.ModAPI;
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



        public static string Version = "1.3.04";
        public static bool Debug = true;
        private static bool Initilized = false;



        public const ushort SeamlessClientNetID = 2936;
        private static System.Timers.Timer PingTimer = new System.Timers.Timer(500);

        public static bool IsSwitching = false;
        public static bool RanJoin = false;



        public void Init(object gameInstance)
        {
           
            TryShow("Running Seamless Client Plugin v[" + Version + "]");
           



          
        }


        public void Update()
        {
            if (MyAPIGateway.Multiplayer == null)
                return;


            if (!Initilized)
            {
                Patches.GetPatches();
                TryShow("Initilizing Communications!");
                RunInitilizations();
            }
        }

        



        public static void RunInitilizations()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(SeamlessClientNetID, MessageHandler);
            Initilized = true;
        }

        public static void DisposeInitilizations()
        {
            MyAPIGateway.Multiplayer?.UnregisterSecureMessageHandler(SeamlessClientNetID, MessageHandler);
            Initilized = false;
            
        }


        private static void MessageHandler(ushort obj1, byte[] obj2, ulong obj3, bool obj4)
        {
            try
            {
                ClientMessage Recieved = Utilities.Utility.Deserialize<ClientMessage>(obj2);

                if(Recieved.MessageType == ClientMessageType.FirstJoin)
                {
                    //Server sent a first join message! Send a reply back so the server knows what version we are on
                    ClientMessage PingServer = new ClientMessage(ClientMessageType.FirstJoin);
                    MyAPIGateway.Multiplayer?.SendMessageToServer(SeamlessClientNetID, Utilities.Utility.Serialize(PingServer));
                }
                else if (Recieved.MessageType == ClientMessageType.TransferServer)
                {
                    //Server sent a transfer message! Begin transfer via seamless
                    Transfer TransferMessage = Recieved.GetTransferData();
                    ServerPing.StartServerPing(TransferMessage);
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

        

        public void Dispose()
        {
            DisposeInitilizations();
        }
    }
}
