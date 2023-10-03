// This file is part of NeosModLoader and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley.

using System;
using System.Collections.Generic;

namespace NeosModConfig
{
	/// <summary>
	/// Represents an interface for mod configurations.
	/// </summary>
	public interface IModConfigurationDefinition
	{
		/// <summary>
		/// Gets the name of the mod that owns this configuration definition.
		/// </summary>
		string Owner { get; }

		/// <summary>
		/// Gets the semantic version for this configuration definition. This is used to check if the defined and saved configs are compatible.
		/// </summary>
		Version Version { get; }

		/// <summary>
		/// Gets the set of configuration keys defined in this configuration definition.
		/// </summary>
		ISet<ModConfigurationKey> ConfigurationItemDefinitions { get; }
	}
}
