using BepInEx;
using BepInEx.Logging;
using BepInEx.NET.Common;

namespace NeosModConfig
{
	[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
	public class Plugin : BasePlugin
	{
		internal static new ManualLogSource Log;

		public override void Load()
		{
			// Plugin startup logic
			this.Log = base.Log;
			Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
		}
	}
}
