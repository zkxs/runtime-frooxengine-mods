// This file is part of NeosModLoader and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © Neos Modding Group contributors.

using System.IO;

namespace NeosModConfig.Utility
{
	// Provides helper functions for platform-specific operations.
	// Used for cases such as file handling which can vary between platforms.
	internal class PlatformHelper
	{
		public static readonly string AndroidNeosPath = "/sdcard/ModData/com.Solirax.Neos"; //TODO: no idea what this should be 

		// Android does not support Directory.GetCurrentDirectory(), so will fall back to the root '/' directory.
		public static bool UseFallbackPath() => Directory.GetCurrentDirectory().Replace('\\', '/') == "/" && !Directory.Exists("/Neos_Data");
		public static bool IsPathEmbedded(string path) => path.StartsWith("/data/app/");

		public static string MainDirectory
		{
			get { return UseFallbackPath() ? AndroidNeosPath : Directory.GetCurrentDirectory(); }
		}
	}
}
