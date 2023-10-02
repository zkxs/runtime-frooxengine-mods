// This file is part of NeosModLoader and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley & Neos Modding Group contributors.

using System;
using System.Reflection;

namespace NeosModLoader
{
	internal class AssemblyFile
	{
		internal string File { get; }
		internal Assembly Assembly { get; set; }
		internal AssemblyFile(string file, Assembly assembly)
		{
			File = file;
			Assembly = assembly;
		}
		private string? sha256;
		internal string Sha256
		{
			get
			{
				if (sha256 == null)
				{
					try
					{
						sha256 = Util.GenerateSHA256(File);
					}
					catch (Exception e)
					{
						Logger.ErrorInternal($"Exception calculating sha256 hash for {File}:\n{e}");
						sha256 = "failed to generate hash";
					}
				}
				return sha256;
			}
		}
	}
}
