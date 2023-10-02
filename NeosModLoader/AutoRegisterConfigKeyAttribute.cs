// This file is part of NeosModLoader and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley & Neos Modding Group contributors.

using System;

namespace NeosModLoader
{
	/// <summary>
	/// Marks a field of type <see cref="ModConfigurationKey{T}"/> on a class
	/// deriving from <see cref="NeosMod"/> to be automatically included in that mod's configuration.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class AutoRegisterConfigKeyAttribute : Attribute
	{ }
}
