using Sandbox;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamlessClientPlugin.SeamlessTransfer
{
    public class MyScriptManagerLoader
    {
		/*
		public void LoadData(MyScriptManager __instance)
		{
			MySandboxGame.Log.WriteLine("MyScriptManager.LoadData() - START");
			MySandboxGame.Log.IncreaseIndent();
			MyScriptManager.Static = __instance;
			__instance.Scripts.Clear();
			__instance.EntityScripts.Clear();
			__instance.SubEntityScripts.Clear();

			TryAddEntityScripts(MyModContext.BaseGame, MyPlugins.SandboxAssembly);
			TryAddEntityScripts(MyModContext.BaseGame, MyPlugins.SandboxGameAssembly);

			if (MySession.Static.CurrentPath != null)
			{
				LoadScripts(MySession.Static.CurrentPath, MyModContext.BaseGame);
			}
			if (MySession.Static.Mods != null)
			{
				bool isServer = Sync.IsServer;
				foreach (MyObjectBuilder_Checkpoint.ModItem mod in MySession.Static.Mods)
				{
					bool flag = false;
					if (mod.IsModData())
					{
						ListReader<string> tags = mod.GetModData().Tags;
						if (tags.Contains(MySteamConstants.TAG_SERVER_SCRIPTS) && !isServer)
						{
							continue;
						}
						flag = tags.Contains(MySteamConstants.TAG_NO_SCRIPTS);
					}
					MyModContext myModContext = (MyModContext)mod.GetModContext();
					try
					{
						LoadScripts(mod.GetPath(), myModContext);
					}
					catch (MyLoadingRuntimeCompilationNotSupportedException)
					{
						if (flag)
						{
							MyVRage.Platform.Scripting.ReportIncorrectBehaviour(MyCommonTexts.ModRuleViolation_RuntimeScripts);
							continue;
						}
						throw;
					}
					catch (Exception ex2)
					{
						MyLog.Default.WriteLine(string.Format("Fatal error compiling {0}:{1} - {2}. This item is likely not a mod and should be removed from the mod list.", myModContext.ModServiceName, myModContext.ModId, myModContext.ModName));
						MyLog.Default.WriteLine(ex2);
						throw;
					}
				}
			}
			foreach (Assembly value in Scripts.Values)
			{
				if (MyFakes.ENABLE_TYPES_FROM_MODS)
				{
					MyGlobalTypeMetadata.Static.RegisterAssembly(value);
				}
				MySandboxGame.Log.WriteLine(string.Format("Script loaded: {0}", value.FullName));
			}
			MyTextSurfaceScriptFactory.LoadScripts();
			MyUseObjectFactory.RegisterAssemblyTypes(Scripts.Values.ToArray());
			MySandboxGame.Log.DecreaseIndent();
			MySandboxGame.Log.WriteLine("MyScriptManager.LoadData() - END");
		}
		*/

	}
}
