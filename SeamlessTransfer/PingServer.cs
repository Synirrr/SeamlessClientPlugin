using Sandbox.Engine.Networking;
using Sandbox.Game.Multiplayer;
using SeamlessClientPlugin.ClientMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.GameServices;

namespace SeamlessClientPlugin.SeamlessTransfer
{
    public class ServerPing
    {

        private static WorldRequest Request { get { return Transfer.WorldRequest; } }
        private static Transfer Transfer;


        public static void StartServerPing(Transfer ClientTransfer)
        {
            // We need to first ping the server to make sure its running and so we can get a connection
            Transfer = ClientTransfer;


            if (Transfer.TargetServerID == 0)
            {
                SeamlessClient.TryShow("This is not a valid server!");
                return;
            }

           

            MyGameServerItem E = new MyGameServerItem();
            E.ConnectionString = Transfer.IPAdress;
            E.SteamID = Transfer.TargetServerID;
            E.Name = Transfer.ServerName;
            


            SeamlessClient.TryShow("Beginning Redirect to server: " + Transfer.TargetServerID);

            SwitchServers Switcher = new SwitchServers(E, Request.DeserializeWorldData());
            Switcher.BeginSwitch();
        }
    }
}
