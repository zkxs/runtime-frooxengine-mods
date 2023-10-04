// This file is part of NeosModConfig and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley & Neos Modding Group contributors.

using FrooxEngine;
using HarmonyLib;
using NeosModConfig.JsonConverters;
using NeosModConfig.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NeosModConfig
{
	public class ModConfigurationManager
	{

		private static readonly string ConfigDirectory = Path.Combine(PlatformHelper.MainDirectory, "nml_config");
		internal static readonly string VERSION_JSON_KEY = "version";
		internal static readonly string VALUES_JSON_KEY = "values";

		// contains all the mod configuration data in memory
		private static Dictionary<string, ModConfiguration> modConfigurations;

		internal static readonly JsonSerializer jsonSerializer = CreateJsonSerializer();

		private static JsonSerializer CreateJsonSerializer()
		{
			JsonSerializerSettings settings = new()
			{
				MaxDepth = 32,
				ReferenceLoopHandling = ReferenceLoopHandling.Error,
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
			};
			List<JsonConverter> converters = new();
			IList<JsonConverter> defaultConverters = settings.Converters;
			if (defaultConverters != null && defaultConverters.Count() != 0)
			{
				Plugin.Log!.LogDebug($"Using {defaultConverters.Count()} default json converters");
				converters.AddRange(defaultConverters);
			}
			converters.Add(new EnumConverter());
			converters.Add(new NeosPrimitiveConverter());
			settings.Converters = converters;
			return JsonSerializer.Create(settings);
		}

		private static void EnsureDirectoryExists()
		{
			Directory.CreateDirectory(ConfigDirectory);
		}

		internal static string GetModConfigPath(string owner)
		{
			//TODO: make sure characters that Windows will throw a fit over get filtered out.
			return Path.Combine(ConfigDirectory, owner, ".json");
		}

		private static bool AreVersionsCompatible(Version serializedVersion, Version currentVersion)
		{
			if (serializedVersion.Major != currentVersion.Major)
			{
				// major version differences are hard incompatible
				return false;
			}

			if (serializedVersion.Minor > currentVersion.Minor)
			{
				// if serialized config has a newer minor version than us
				// in other words, someone downgraded the mod but not the config
				// then we cannot load the config
				return false;
			}

			// none of the checks failed!
			return true;
		}

		internal static void RegisterShutdownHook(Harmony harmony)
		{
			try
			{
				Engine.Current.RegisterShutdownTask(Task.Run(ShutdownHook));
			}
			catch (Exception e)
			{
				Plugin.Log!.LogError($"Unexpected exception applying shutdown hook!\n{e}");
			}
		}

		private static void ShutdownHook()
		{
			int count = 0;
			modConfigurations
				.Select(keyValuePair => keyValuePair.Value)
				.Where(config => config != null)
				.Where(config => config!.AutoSave)
				.Where(config => config!.AnyValuesSet())
				.Do(config =>
				{
					try
					{
						// synchronously save the config
						config!.SaveInternal();
					}
					catch (Exception e)
					{
						Plugin.Log!.LogError($"Error saving configuration for {config!.Owner}:\n{e}");
					}
					count += 1;
				});
			Plugin.Log!.LogInfo($"Configs saved for {count} mods.");
		}

		private static ModConfiguration? LoadConfigForMod(ModConfigurationDefinition? definition)
		{
			if (definition == null)
			{
				// if there's no definition, then there's nothing for us to do here
				return null;
			}

			string configFile = GetModConfigPath(definition.Owner);

			try
			{
				using StreamReader file = File.OpenText(configFile);
				using JsonTextReader reader = new(file);
				JObject json = JObject.Load(reader);
				Version version = new(json[VERSION_JSON_KEY]!.ToObject<string>(jsonSerializer));
				if (!AreVersionsCompatible(version, definition.Version))
				{
					var handlingMode = definition.HandleIncompatibleConfigurationVersions(definition.Version, version);
					switch (handlingMode)
					{
						case IncompatibleConfigurationHandlingOption.CLOBBER:
							Plugin.Log!.LogWarning($"{definition.Owner} saved config version is {version} which is incompatible with mod's definition version {definition.Version}. Clobbering old config and starting fresh.");
							return new ModConfiguration(definition);
						case IncompatibleConfigurationHandlingOption.FORCE_LOAD:
							// continue processing
							break;
						case IncompatibleConfigurationHandlingOption.ERROR: // fall through to default
						default:
							throw new ModConfigurationException($"{definition.Owner} saved config version is {version} which is incompatible with mod's definition version {definition.Version}");
					}
				}
				foreach (ModConfigurationKey key in definition.ConfigurationItemDefinitions)
				{
					string keyName = key.Name;
					try
					{
						JToken? token = json[VALUES_JSON_KEY]?[keyName];
						if (token != null)
						{
							object? value = token.ToObject(key.ValueType(), jsonSerializer);
							key.Set(value);
						}
					}
					catch (Exception e)
					{
						// I know not what exceptions the JSON library will throw, but they must be contained
						throw new ModConfigurationException($"Error loading {key.ValueType()} config key \"{keyName}\" for {definition.Owner}", e);
					}
				}
			}
			catch (FileNotFoundException)
			{
				// return early
				return new ModConfiguration(definition);
			}
			catch (Exception e)
			{
				// I know not what exceptions the JSON library will throw, but they must be contained
				throw new ModConfigurationException($"Error loading config for {definition.Owner}", e);
			}

			return new ModConfiguration(definition);
		}
	}
}
