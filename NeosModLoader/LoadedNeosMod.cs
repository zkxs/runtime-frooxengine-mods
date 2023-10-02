// This file is part of NeosModLoader and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley.

namespace NeosModLoader
{
	internal class LoadedNeosMod
	{
		internal LoadedNeosMod(NeosMod neosMod, AssemblyFile modAssembly)
		{
			NeosMod = neosMod;
			ModAssembly = modAssembly;
		}

		internal NeosMod NeosMod { get; private set; }
		internal AssemblyFile ModAssembly { get; private set; }
		internal ModConfiguration? ModConfiguration { get; set; }
		internal bool AllowSavingConfiguration = true;
		internal bool FinishedLoading { get => NeosMod.FinishedLoading; set => NeosMod.FinishedLoading = value; }
		internal string Name { get => NeosMod.Name; }
	}
}
