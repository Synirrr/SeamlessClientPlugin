using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage.Game;

namespace SeamlessClientPlugin.SeamlessTransfer
{
    public static class ModLoader
    {
        /*  Mod loader should download and load missing mods for target server, and unload ones that arent needed
         * 
         * Sandbox.Game.World.MyScriptManager.LoadData() is where modded scripts get loaded and added
         * Sandbox.Game.World.MySession() calls MyDefinitionManager.Static.LoadData(mods); which loads mod data files
         * 
         * 
         *  Need to be called in the following order: 
         *  ScriptManager.Init(checkpoint.ScriptManagerData);
	     *  MyDefinitionManager.Static.LoadData(checkpoint.Mods);
	     *  PreloadModels(sector);
         * 
         * 
         */


        //Mods that are currently loaded in this instance.
        private static List<MyObjectBuilder_Checkpoint.ModItem> CurrentLoadedMods = new List<MyObjectBuilder_Checkpoint.ModItem>();

        //Mods that we are switching to.
        private static List<MyObjectBuilder_Checkpoint.ModItem> TargetServerMods = new List<MyObjectBuilder_Checkpoint.ModItem>();


        private static bool FinishedDownloadingMods = false;
        private static bool DownloadSuccess = false;

        private static DateTime DownloadTimeout;
       



        public static void DownloadNewMods(List<MyObjectBuilder_Checkpoint.ModItem> Target)
        {
            CurrentLoadedMods = MySession.Static.Mods;
            TargetServerMods = Target;


            DownloadTimeout = DateTime.Now + TimeSpan.FromMinutes(5);
            SeamlessClient.TryShow("Downloading New Mods");
            MyWorkshop.DownloadModsAsync(Target, ModDownloadingFinished);

        }

        private static void ModDownloadingFinished(bool Success)
        {
            if (Success)
            {
                SeamlessClient.TryShow("Mod Downloading Finished!");
                FinishedDownloadingMods = true;
                DownloadSuccess = true;
                //May need to wait seamless loading if mods have yet to finish downloading
            }
            else
            {
                DownloadSuccess = false;
                FinishedDownloadingMods = true;
            }
        }

        public static void ReadyModSwitch()
        {
           
            while (!FinishedDownloadingMods)
            {

                //Break out of loop
                if (DownloadTimeout < DateTime.Now)
                    break;


                Thread.Sleep(20);
            }

            FinishedDownloadingMods = false;

            //Skip mod switch
            if (!DownloadSuccess)
                return;

            MySession.Static.ScriptManager.LoadData();
            MyDefinitionManager.Static.LoadData(TargetServerMods);
            MyLocalCache.PreloadLocalInventoryConfig();
            SeamlessClient.TryShow("Finished transfering!");

        }


      


    }
}
