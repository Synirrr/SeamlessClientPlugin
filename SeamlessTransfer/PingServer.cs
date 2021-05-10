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

        private static WorldRequest Request;
        private static Transfer Transfer;


        public static void StartServerPing(Transfer ClientTransfer)
        {
            // We need to first ping the server to make sure its running and so we can get a connection
            Transfer = ClientTransfer;
            Request = Transfer.WorldRequest;


            if (Transfer.TargetServerID == 0)
            {
                SeamlessClient.TryShow("This is not a valid server!");
                return;
            }

            SeamlessClient.TryShow("Beginning Redirect to server: " + Transfer.TargetServerID);
            MyGameService.OnPingServerResponded += PingResponded;
            MyGameService.OnPingServerFailedToRespond += FailedToRespond;

            MyGameService.PingServer(Transfer.IPAdress);
        }

        private static void PingResponded(object sender, MyGameServerItem e)
        {
            //If server ping was successful we need to begin the switching proccess
            UnRegisterEvents();
          
            SeamlessClient.TryShow($"{e.Name} was successfully pinged!");
            SwitchServers Switcher = new SwitchServers(e, Request.DeserializeWorldData());
            Switcher.BeginSwitch();
           // LoadServer.LoadWorldData(e, Request.DeserializeWorldData());
        }


        private static void FailedToRespond(object sender, EventArgs e)
        {
            // If the target server failed to respond, we need to exit/return to menu
            UnRegisterEvents();
        }


        private static void UnRegisterEvents()
        {
            //Un-register ping events
            MyGameService.OnPingServerResponded -= PingResponded;
            MyGameService.OnPingServerFailedToRespond -= FailedToRespond;
        }
    }
}
