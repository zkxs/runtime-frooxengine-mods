// This file is part of NeosModLoader and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley & Neos Modding Group contributors.

using FrooxEngine;
using HarmonyLib;
using NeosModConfig.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NeosModConfig
{
	/// <summary>
	/// The configuration for a mod. Each mod has zero or one configuration. The configuration object will never be reassigned once initialized.
	/// </summary>
	public class ModConfiguration : IModConfigurationDefinition
	{
		private readonly ModConfigurationDefinition Definition;

		/// <inheritdoc/>
		public string Owner => Definition.Owner;

		/// <inheritdoc/>
		public Version Version => Definition.Version;

		/// <inheritdoc/>
		public ISet<ModConfigurationKey> ConfigurationItemDefinitions => Definition.ConfigurationItemDefinitions;

		internal bool AutoSave => Definition.AutoSave;

		/// <summary>
		/// The delegate that is called for configuration change events.
		/// </summary>
		/// <param name="configurationChangedEvent">The event containing details about the configuration change</param>
		public delegate void ConfigurationChangedEventHandler(ConfigurationChangedEvent configurationChangedEvent);

		/// <summary>
		/// Called if any config value for any mod changed.
		/// </summary>
		public static event ConfigurationChangedEventHandler? OnAnyConfigurationChanged;

		/// <summary>
		/// Called if one of the values in this mod's config changed.
		/// </summary>
		public event ConfigurationChangedEventHandler? OnThisConfigurationChanged;

		// used to track how frequenly Save() is being called
		private Stopwatch saveTimer = new Stopwatch();

		// time that save must not be called for a save to actually go through
		private int debounceMilliseconds = 3000;

		// used to keep track of mods that spam Save():
		// any mod that calls Save() for the ModConfiguration within debounceMilliseconds of the previous call to the same ModConfiguration
		// will be put into Ultimate Punishment Mode, and ALL their Save() calls, regardless of ModConfiguration, will be debounced.
		// The naughty list is global, while the actual debouncing is per-configuration.
		private static ISet<Assembly> naughtySavers = new HashSet<Assembly>();

		// used to keep track of the debouncers for this configuration.
		private Dictionary<Assembly, Action<bool>> saveActionForCallee = new();

		internal ModConfiguration(ModConfigurationDefinition definition)
		{
			Definition = definition;
		}

		/// <summary>
		/// Checks if the given key is defined in this config.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns><c>true</c> if the key is defined.</returns>
		public bool IsKeyDefined(ModConfigurationKey key)
		{
			// if a key has a non-null defining key it's guaranteed a real key. Lets check for that.
			ModConfigurationKey? definingKey = key.DefiningKey;
			if (definingKey != null)
			{
				return true;
			}

			// okay, the defining key was null, so lets try to get the defining key from the hashtable instead
			if (Definition.TryGetDefiningKey(key, out definingKey))
			{
				// we might as well set this now that we have the real defining key
				key.DefiningKey = definingKey;
				return true;
			}

			// there was no definition
			return false;
		}

		/// <summary>
		/// Checks if the given key is the defining key.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns><c>true</c> if the key is the defining key.</returns>
		internal bool IsKeyDefiningKey(ModConfigurationKey key)
		{
			// a key is the defining key if and only if its DefiningKey property references itself
			return ReferenceEquals(key, key.DefiningKey); // this is safe because we'll throw a NRE if key is null
		}

		/// <summary>
		/// Get a value, throwing a <see cref="KeyNotFoundException"/> if the key is not found.
		/// </summary>
		/// <param name="key">The key to get the value for.</param>
		/// <returns>The value for the key.</returns>
		/// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
		public object GetValue(ModConfigurationKey key)
		{
			if (TryGetValue(key, out object? value))
			{
				return value!;
			}
			else
			{
				throw new KeyNotFoundException($"{key.Name} not found in {Owner} configuration");
			}
		}

		/// <summary>
		/// Get a value, throwing a <see cref="KeyNotFoundException"/> if the key is not found.
		/// </summary>
		/// <typeparam name="T">The type of the key's value.</typeparam>
		/// <param name="key">The key to get the value for.</param>
		/// <returns>The value for the key.</returns>
		/// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
		public T? GetValue<T>(ModConfigurationKey<T> key)
		{
			if (TryGetValue(key, out T? value))
			{
				return value;
			}
			else
			{
				throw new KeyNotFoundException($"{key.Name} not found in {Owner} configuration");
			}
		}

		/// <summary>
		/// Tries to get a value, returning <c>default</c> if the key is not found.
		/// </summary>
		/// <param name="key">The key to get the value for.</param>
		/// <param name="value">The value if the return value is <c>true</c>, or <c>default</c> if <c>false</c>.</param>
		/// <returns><c>true</c> if the value was read successfully.</returns>
		public bool TryGetValue(ModConfigurationKey key, out object? value)
		{
			if (!Definition.TryGetDefiningKey(key, out ModConfigurationKey? definingKey))
			{
				// not in definition
				value = null;
				return false;
			}

			if (definingKey!.TryGetValue(out object? valueObject))
			{
				value = valueObject;
				return true;
			}
			else if (definingKey.TryComputeDefault(out value))
			{
				return true;
			}
			else
			{
				value = null;
				return false;
			}
		}


		/// <summary>
		/// Tries to get a value, returning <c>default(<typeparamref name="T"/>)</c> if the key is not found.
		/// </summary>
		/// <param name="key">The key to get the value for.</param>
		/// <param name="value">The value if the return value is <c>true</c>, or <c>default</c> if <c>false</c>.</param>
		/// <returns><c>true</c> if the value was read successfully.</returns>
		public bool TryGetValue<T>(ModConfigurationKey<T> key, out T? value)
		{
			if (TryGetValue(key, out object? valueObject))
			{
				value = (T)valueObject!;
				return true;
			}
			else
			{
				value = default;
				return false;
			}
		}

		/// <summary>
		/// Sets a configuration value for the given key, throwing a <see cref="KeyNotFoundException"/> if the key is not found
		/// or an <see cref="ArgumentException"/> if the value is not valid for it.
		/// </summary>
		/// <param name="key">The key to get the value for.</param>
		/// <param name="value">The new value to set.</param>
		/// <param name="eventLabel">A custom label you may assign to this change event.</param>
		/// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
		/// <exception cref="ArgumentException">The new value is not valid for the given key.</exception>
		public void Set(ModConfigurationKey key, object? value, string? eventLabel = null)
		{
			if (!Definition.TryGetDefiningKey(key, out ModConfigurationKey? definingKey))
			{
				throw new KeyNotFoundException($"{key.Name} is not defined in the config definition for {Owner}");
			}

			if (value == null)
			{
				if (Util.CannotBeNull(definingKey!.ValueType()))
				{
					throw new ArgumentException($"null cannot be assigned to {definingKey.ValueType()}");
				}
			}
			else if (!definingKey!.ValueType().IsAssignableFrom(value.GetType()))
			{
				throw new ArgumentException($"{value.GetType()} cannot be assigned to {definingKey.ValueType()}");
			}

			if (!definingKey!.Validate(value))
			{
				throw new ArgumentException($"\"{value}\" is not a valid value for \"{Owner}.{definingKey.Name}\"");
			}

			definingKey.Set(value);
			FireConfigurationChangedEvent(definingKey, eventLabel);
		}

		/// <summary>
		/// Sets a configuration value for the given key, throwing a <see cref="KeyNotFoundException"/> if the key is not found
		/// or an <see cref="ArgumentException"/> if the value is not valid for it.
		/// </summary>
		/// <typeparam name="T">The type of the key's value.</typeparam>
		/// <param name="key">The key to get the value for.</param>
		/// <param name="value">The new value to set.</param>
		/// <param name="eventLabel">A custom label you may assign to this change event.</param>
		/// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
		/// <exception cref="ArgumentException">The new value is not valid for the given key.</exception>
		public void Set<T>(ModConfigurationKey<T> key, T value, string? eventLabel = null)
		{
			// the reason we don't fall back to untyped Set() here is so we can skip the type check

			if (!Definition.TryGetDefiningKey(key, out ModConfigurationKey? definingKey))
			{
				throw new KeyNotFoundException($"{key.Name} is not defined in the config definition for {Owner}");
			}

			if (!definingKey!.Validate(value))
			{
				throw new ArgumentException($"\"{value}\" is not a valid value for \"{Owner}.{definingKey.Name}\"");
			}

			definingKey.Set(value);
			FireConfigurationChangedEvent(definingKey, eventLabel);
		}

		/// <summary>
		/// Removes a configuration value, throwing a <see cref="KeyNotFoundException"/> if the key is not found.
		/// </summary>
		/// <param name="key">The key to remove the value for.</param>
		/// <returns><c>true</c> if a value was successfully found and removed, <c>false</c> if there was no value to remove.</returns>
		/// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
		public bool Unset(ModConfigurationKey key)
		{
			if (Definition.TryGetDefiningKey(key, out ModConfigurationKey? definingKey))
			{
				return definingKey!.Unset();
			}
			else
			{
				throw new KeyNotFoundException($"{key.Name} is not defined in the config definition for {Owner}");
			}
		}

		internal bool AnyValuesSet()
		{
			return ConfigurationItemDefinitions
				.Where(key => key.HasValue)
				.Any();
		}



		/// <summary>
		/// Persist this configuration to disk.<br/>
		/// This method is not called automatically.<br/>
		/// Default values are not automatically saved.
		/// </summary>
		public void Save() // this overload is needed for binary compatibility (REMOVE IN NEXT MAJOR VERSION)
		{
			Save(false, false);
		}

		/// <summary>
		/// Persist this configuration to disk.<br/>
		/// This method is not called automatically.
		/// </summary>
		/// <param name="saveDefaultValues">If <c>true</c>, default values will also be persisted.</param>
		public void Save(bool saveDefaultValues = false) // this overload is needed for binary compatibility (REMOVE IN NEXT MAJOR VERSION)
		{
			Save(saveDefaultValues, false);
		}


		/// <summary>
		/// Asynchronously persists this configuration to disk.
		/// </summary>
		/// <param name="saveDefaultValues">If <c>true</c>, default values will also be persisted.</param>
		/// <param name="immediate">If <c>true</c>, skip the debouncing and save immediately.</param>
		internal void Save(bool saveDefaultValues = false, bool immediate = false)
		{

			Thread thread = Thread.CurrentThread;
			Assembly? callee = Util.ExecutingAssembly(new(1));
			Action<bool>? saveAction = null;

			// get saved state for this callee
			if (callee != null && naughtySavers.Contains(callee) && !saveActionForCallee.TryGetValue(callee, out saveAction))
			{
				// handle case where the callee was marked as naughty from a different ModConfiguration being spammed
				saveAction = Util.Debounce<bool>(SaveInternal, debounceMilliseconds);
				saveActionForCallee.Add(callee, saveAction);
			}

			if (saveTimer.IsRunning)
			{
				float elapsedMillis = saveTimer.ElapsedMilliseconds;
				saveTimer.Restart();
				if (elapsedMillis < debounceMilliseconds)
				{
					Plugin.Log!.LogWarning($"ModConfiguration.Save({saveDefaultValues}) called for \"{Owner}\" by \"{callee}\" from thread with id=\"{thread.ManagedThreadId}\", name=\"{thread.Name}\", bg=\"{thread.IsBackground}\", pool=\"{thread.IsThreadPoolThread}\". Last called {elapsedMillis / 1000f}s ago. This is very recent! Do not spam calls to ModConfiguration.Save()! All Save() calls by this mod are now subject to a {debounceMilliseconds}ms debouncing delay.");
					if (saveAction == null && callee != null)
					{
						// congrats, you've switched into Ultimate Punishment Mode where now I don't trust you and your Save() calls get debounced
						saveAction = Util.Debounce<bool>(SaveInternal, debounceMilliseconds);
						saveActionForCallee.Add(callee, saveAction);
						naughtySavers.Add(callee);
					}
				}
				else
				{
					Plugin.Log!.LogDebug($"ModConfiguration.Save({saveDefaultValues}) called for \"{Owner}\" by \"{callee}\" from thread with id=\"{thread.ManagedThreadId}\", name=\"{thread.Name}\", bg=\"{thread.IsBackground}\", pool=\"{thread.IsThreadPoolThread}\". Last called {elapsedMillis / 1000f}s ago.");
				}
			}
			else
			{
				saveTimer.Start();
				Plugin.Log!.LogDebug($"ModConfiguration.Save({saveDefaultValues}) called for \"{Owner}\" by \"{callee}\" from thread with id=\"{thread.ManagedThreadId}\", name=\"{thread.Name}\", bg=\"{thread.IsBackground}\", pool=\"{thread.IsThreadPoolThread}\"");
			}

			if (immediate || saveAction == null)
			{
				// infrequent callers get to save immediately
				Task.Run(() => SaveInternal(saveDefaultValues));
			}
			else
			{
				// bad callers get debounced
				saveAction(saveDefaultValues);
			}
		}

		/// <summary>
		/// performs the actual, synchronous save
		/// </summary>
		/// <param name="saveDefaultValues">If true, default values will also be persisted</param>
		internal void SaveInternal(bool saveDefaultValues = false)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			JObject json = new()
			{
				[ModConfigurationManager.VERSION_JSON_KEY] = JToken.FromObject(Definition.Version.ToString(), ModConfigurationManager.jsonSerializer)
			};

			JObject valueMap = new();
			foreach (ModConfigurationKey key in ConfigurationItemDefinitions)
			{
				if (key.TryGetValue(out object? value))
				{
					// I don't need to typecheck this as there's no way to sneak a bad type past my Set() API
					valueMap[key.Name] = value == null ? null : JToken.FromObject(value, ModConfigurationManager.jsonSerializer);
				}
				else if (saveDefaultValues && key.TryComputeDefault(out object? defaultValue))
				{
					// I don't need to typecheck this as there's no way to sneak a bad type past my computeDefault API
					// like and say defaultValue can't be null because the Json.Net
					valueMap[key.Name] = defaultValue == null ? null : JToken.FromObject(defaultValue, ModConfigurationManager.jsonSerializer);
				}
			}

			json[ModConfigurationManager.VALUES_JSON_KEY] = valueMap;

			string configFile = ModConfigurationManager.GetModConfigPath(Owner);
			using FileStream file = File.OpenWrite(configFile);
			using StreamWriter streamWriter = new(file);
			using JsonTextWriter jsonTextWriter = new(streamWriter);
			json.WriteTo(jsonTextWriter);

			// I actually cannot believe I have to truncate the file myself
			file.SetLength(file.Position);
			jsonTextWriter.Flush();

			Plugin.Log!.LogDebug($"Saved ModConfiguration for \"{Owner}\" in {stopwatch.ElapsedMilliseconds}ms");
		}

		private void FireConfigurationChangedEvent(ModConfigurationKey key, string? label)
		{
			try
			{
				OnAnyConfigurationChanged?.SafeInvoke(new ConfigurationChangedEvent(this, key, label));
			}
			catch (Exception e)
			{
				Plugin.Log!.LogError($"An OnAnyConfigurationChanged event subscriber threw an exception:\n{e}");
			}

			try
			{
				OnThisConfigurationChanged?.SafeInvoke(new ConfigurationChangedEvent(this, key, label));
			}
			catch (Exception e)
			{
				Plugin.Log!.LogError($"An OnThisConfigurationChanged event subscriber threw an exception:\n{e}");
			}
		}

	}

}
