// This file is part of NeosModConfig and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley.

using System;

namespace NeosModConfig
{
	/// <summary>
	/// Represents an <see cref="Exception"/> encountered while loading a mod's configuration file.
	/// </summary>
	public class ModConfigurationException : Exception
	{
		internal ModConfigurationException(string message) : base(message)
		{
		}

		internal ModConfigurationException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
