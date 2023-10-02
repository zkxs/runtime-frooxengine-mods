// This file is part of NeosModLoader and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley.

using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace NeosModLoader
{
	// this class does all the harmony-related NML work.
	// this is needed to avoid importing harmony in ExecutionHook, where it may not be loaded yet.
	internal class HarmonyWorker
	{
		internal static void LoadModsAndHideModAssemblies(HashSet<Assembly> initialAssemblies)
		{
			Harmony harmony = new("com.neosmodloader");
			ModLoader.LoadMods(harmony);
			AssemblyHider.PatchNeos(harmony, initialAssemblies);
		}
	}
}
