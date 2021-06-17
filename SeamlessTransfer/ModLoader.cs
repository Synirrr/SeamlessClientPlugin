using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamlessClientPlugin.SeamlessTransfer
{
    public class ModLoader
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

    }
}
