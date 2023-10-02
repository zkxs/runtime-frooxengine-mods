// This file is part of TrackerIdStabilizer and is licensed under the GNU GPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley

using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Reflection;

namespace TrackerIdStabilizer
{
	public class TrackerIdStabilizer : NeosMod
	{
		internal const string VERSION = "1.0.0";
		public override string Name => "TrackerIdStabilizer";
		public override string Author => "runtime";
		public override string Version => VERSION;
		public override string Link => "https://github.com/zkxs/runtime-frooxengine-mods/TrackerIdStabilizer";

		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("net.michaelripley.TrackerIdStabilizer");

			MethodInfo originalMethod = AccessTools.DeclaredPropertySetter(typeof(ViveTracker), nameof(ViveTracker.UniqueID));
			if (originalMethod == null)
			{
				Error($"Could not find ViveTracker.UniqueID setter");
				return;
			}

			MethodInfo patch = AccessTools.DeclaredMethod(typeof(TrackerIdStabilizer), nameof(TrackerIdStabilizer.UniqueIDSetterPostfix));
			harmony.Patch(originalMethod, postfix: new HarmonyMethod(patch));
			Msg("patch installed successfully");
		}

		public static void UniqueIDSetterPostfix(ViveTracker __instance, string value)
		{
			MethodInfo publicIdSetter = AccessTools.DeclaredPropertySetter(typeof(ViveTracker), nameof(ViveTracker.PublicID));
			if (publicIdSetter == null)
			{
				Error("Could not find ViveTracker.PublicID setter");
				return;
			}
			try
			{
				Debug($"renaming vive tracker from {__instance.PublicID} to {value}");
				publicIdSetter.Invoke(__instance, new object[] { value });
			}
			catch (Exception e)
			{
				Error($"Exception calling ViveTracker.PublicID setter:\n{e}");
			}
		}
	}
}
