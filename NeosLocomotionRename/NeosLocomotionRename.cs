// This file is part of LocomotionRename and is licensed under the GNU GPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley

using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Reflection;

namespace NeosLocomotionRename
{
	public class NeosLocomotionRename : NeosMod
	{
		internal const string VERSION = "1.0.0";
		public override string Name => "NeosLocomotionRename";
		public override string Author => "runtime";
		public override string Version => VERSION;
		public override string Link => "https://github.com/zkxs/NeosLocomotionRename";

		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("net.michaelripley.NeosLocomotionRename");
			MethodInfo patch = AccessTools.DeclaredMethod(typeof(NeosLocomotionRename), nameof(NeosLocomotionRename.GetLocomotionNamePostfix));

			Type[] moduleTypes = new Type[]
			{
				typeof(LocomotionModule),
				typeof(NoclipLocomotion),
				typeof(GrabWorldLocomotion),
				typeof(SlideLocomotion),
				typeof(TeleportLocomotion),
			};

			foreach (Type moduleType in moduleTypes)
			{
				MethodInfo originalMethod = AccessTools.DeclaredPropertyGetter(moduleType, nameof(LocomotionModule.LocomotionName));
				if (originalMethod == null)
				{
					Warn($"Could not find {moduleType.Name}.LocomotionName getter, skipping!");
					break;
				}
				harmony.Patch(originalMethod, postfix: new HarmonyMethod(patch));
				Msg($"{moduleType.Name} patch installed successfully");
			}
		}

		private static void GetLocomotionNamePostfix(LocomotionModule __instance, ref LocaleString __result)
		{
			__result = __instance.Slot.Name;
		}
	}
}
