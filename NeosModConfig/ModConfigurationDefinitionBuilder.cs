// This file is part of NeosModLoader and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley & Neos Modding Group contributors.

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NeosModConfig
{
	/// <summary>
	/// Represents a fluent configuration interface to define mod configurations.
	/// </summary>
	public class ModConfigurationDefinitionBuilder
	{
		private readonly string Owner;
		private Version ConfigVersion = new(1, 0, 0);
		private readonly HashSet<ModConfigurationKey> Keys = new();
		private bool AutoSaveConfig = true;
		private Func<Version, Version, IncompatibleConfigurationHandlingOption> ConfigIncompatibleVersionHandler = HandleIncompatibleConfigurationVersions;

		internal ModConfigurationDefinitionBuilder(string owner)
		{
			Owner = owner;
		}

		/// <summary>
		/// Sets the semantic version of this configuration definition. Default is 1.0.0.
		/// </summary>
		/// <param name="version">The config's semantic version.</param>
		/// <returns>This builder.</returns>
		public ModConfigurationDefinitionBuilder Version(Version version)
		{
			ConfigVersion = version;
			return this;
		}

		/// <summary>
		/// Sets the semantic version of this configuration definition. Default is 1.0.0.
		/// </summary>
		/// <param name="version">The config's semantic version, as a string.</param>
		/// <returns>This builder.</returns>
		public ModConfigurationDefinitionBuilder Version(string version)
		{
			ConfigVersion = new Version(version);
			return this;
		}

		/// <summary>
		/// Adds a new key to this configuration definition.
		/// </summary>
		/// <param name="key">A configuration key.</param>
		/// <returns>This builder.</returns>
		public ModConfigurationDefinitionBuilder Key(ModConfigurationKey key)
		{
			Keys.Add(key);
			return this;
		}

		/// <summary>
		/// Sets the AutoSave property of this configuration definition. Default is <c>true</c>.
		/// </summary>
		/// <param name="autoSave">If <c>false</c>, the config will not be autosaved on Neos close.</param>
		/// <returns>This builder.</returns>
		public ModConfigurationDefinitionBuilder AutoSave(bool autoSave)
		{
			AutoSaveConfig = autoSave;
			return this;
		}

		/// <summary>
		/// Sets the incompatible version handler for this configuration definition. Default is <c>IncompatibleConfigurationHandlingOption.ERROR</c> for all incompatibilities.
		/// </summary>
		/// <param name="incompatibleVersionHandler">A function that given <c>serializedVersion</c> and <c>definedVersion</c> returns your desired handling for the incompatibility.</param>
		/// <returns>This builder.</returns>
		public ModConfigurationDefinitionBuilder IncompatibleVersionHandler(Func<Version, Version, IncompatibleConfigurationHandlingOption> incompatibleVersionHandler)
		{
			ConfigIncompatibleVersionHandler = incompatibleVersionHandler;
			return this;
		}

		internal void ProcessAttributes()
		{
			var fields = AccessTools.GetDeclaredFields(Owner.GetType());
			fields
				.Where(field => Attribute.GetCustomAttribute(field, typeof(AutoRegisterConfigKeyAttribute)) != null)
				.Do(ProcessField);
		}

		private void ProcessField(FieldInfo field)
		{
			if (!typeof(ModConfigurationKey).IsAssignableFrom(field.FieldType))
			{
				// wrong type
				Plugin.Log!.LogWarning($"{Owner} had an [AutoRegisterConfigKey] field of the wrong type: {field}");
				return;
			}

			ModConfigurationKey fieldValue = (ModConfigurationKey)field.GetValue(field.IsStatic ? null : Owner);
			Keys.Add(fieldValue);
		}

		/// <summary>
		/// Default handling of incompatible configuration versions
		/// </summary>
		/// <param name="serializedVersion">Configuration version read from the config file</param>
		/// <param name="definedVersion">Configuration version defined in the mod code</param>
		/// <returns></returns>
		private static IncompatibleConfigurationHandlingOption HandleIncompatibleConfigurationVersions(Version serializedVersion, Version definedVersion)
		{
			return IncompatibleConfigurationHandlingOption.ERROR;
		}

		internal ModConfigurationDefinition? Build()
		{
			if (Keys.Count > 0)
			{
				return new ModConfigurationDefinition(Owner, ConfigVersion, Keys, AutoSaveConfig, ConfigIncompatibleVersionHandler);
			}
			return null;
		}
	}
}
