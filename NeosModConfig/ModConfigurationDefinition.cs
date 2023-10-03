// This file is part of NeosModConfig and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley & Neos Modding Group contributors.

using System;
using System.Collections.Generic;

namespace NeosModConfig
{
	/// <summary>
	/// Defines a mod configuration. This should be defined by a <see cref="NeosMod"/> using the <see cref="NeosMod.DefineConfiguration(ModConfigurationDefinitionBuilder)"/> method.
	/// </summary>
	public class ModConfigurationDefinition : IModConfigurationDefinition
	{
		/// <inheritdoc/>
		public string Owner { get; private set; }

		/// <inheritdoc/>
		public Version Version { get; private set; }

		internal bool AutoSave;

		internal Func<Version, Version, IncompatibleConfigurationHandlingOption> IncompatibleVersionHandler;

		// this is a ridiculous hack because HashSet.TryGetValue doesn't exist in .NET 4.6.2
		private Dictionary<ModConfigurationKey, ModConfigurationKey> configurationItemDefinitionsSelfMap;

		/// <inheritdoc/>
		public ISet<ModConfigurationKey> ConfigurationItemDefinitions
		{
			// clone the collection because I don't trust giving public API users shallow copies one bit
			get => new HashSet<ModConfigurationKey>(configurationItemDefinitionsSelfMap.Keys);
		}

		internal bool TryGetDefiningKey(ModConfigurationKey key, out ModConfigurationKey? definingKey)
		{
			if (key.DefiningKey != null)
			{
				// we've already cached the defining key
				definingKey = key.DefiningKey;
				return true;
			}

			// first time we've seen this key instance: we need to hit the map
			if (configurationItemDefinitionsSelfMap.TryGetValue(key, out definingKey))
			{
				// initialize the cache for this key
				key.DefiningKey = definingKey;
				return true;
			}
			else
			{
				// not a real key
				definingKey = null;
				return false;
			}

		}

		internal ModConfigurationDefinition(string owner, Version version, HashSet<ModConfigurationKey> configurationItemDefinitions, bool autoSave,
			Func<Version, Version, IncompatibleConfigurationHandlingOption> incompatibleVersionHandler)
		{
			Owner = owner;
			Version = version;
			AutoSave = autoSave;
			IncompatibleVersionHandler = incompatibleVersionHandler;

			configurationItemDefinitionsSelfMap = new Dictionary<ModConfigurationKey, ModConfigurationKey>(configurationItemDefinitions.Count);
			foreach (ModConfigurationKey key in configurationItemDefinitions)
			{
				key.DefiningKey = key; // early init this property for the defining key itself
				configurationItemDefinitionsSelfMap.Add(key, key);
			}
		}
		internal IncompatibleConfigurationHandlingOption HandleIncompatibleConfigurationVersions(Version serializedVersion, Version definedVersion)
		{
			return IncompatibleVersionHandler.Invoke(serializedVersion, definedVersion);
		}
	}
}
