// This file is part of MotionBlurDisable and is licensed under the MIT No Attribution License.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley

using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NeosModConfigurationExample
{
	public class NeosModTest : NeosMod
	{
		internal const string VERSION = "1.0.0";
		public override string Name => "NeosModTest";
		public override string Author => "runtime";
		public override string Version => VERSION;
		public override string Link => "https://github.com/zkxs/NeosModConfigurationExample";

		[AutoRegisterConfigKey]
		private readonly ModConfigurationKey<bool> KEY_ENABLE = new ModConfigurationKey<bool>("enabled", "Enables the NeosModTest mod", () => true);

		// this override lets us change optional settings in our configuration definition
		public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
		{
			builder
				.Version(new Version(1, 0, 0)) // manually set config version (default is 1.0.0)
				.AutoSave(false); // don't autosave on Neos shutdown (default is true)
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Engine), "FinishInitialization", new Type[] { })]
		public static void EngineInitDone()
		{
				Msg("Engine.FinishInitialization() just got called! Woohoo!");
		}
	}
}
