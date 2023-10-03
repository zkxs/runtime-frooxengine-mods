// This file is part of NeosModLoader and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley.

using BepInEx;
using BepInEx.Logging;
using BepInEx.NET.Common;

namespace NeosModConfig
{
	[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
	public class Plugin : BasePlugin
	{
		internal static Plugin? instance;
		internal static ManualLogSource? Log;

		public override void Load()
		{
			// Plugin startup logic
			instance = this;
			Log = base.Log;
			Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
		}
	}
}
