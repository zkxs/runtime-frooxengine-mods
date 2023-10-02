// This file is part of NeosModLoader and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley.

using Elements.Core;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace NeosModLoader.JsonConverters
{
	internal class NeosPrimitiveConverter : JsonConverter
	{
		private static readonly Assembly ELEMENTS_CORE = typeof(colorX).Assembly;

		public override bool CanConvert(Type objectType)
		{
			// handle all non-enum Neos Primitives in the Elements.Core assembly
			return !objectType.IsEnum && ELEMENTS_CORE.Equals(objectType.Assembly) && Coder.IsEnginePrimitive(objectType);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			if (reader.Value is string serialized)
			{
				// use Neos's built-in decoding if the value was serialized as a string
				return typeof(Coder<>).MakeGenericType(objectType).GetMethod("DecodeFromString").Invoke(null, new object[] { serialized });
			}

			throw new ArgumentException($"Could not deserialize an Elements.Core type: {objectType} from a {reader?.Value?.GetType()}");
		}

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			string serialized = (string)typeof(Coder<>).MakeGenericType(value!.GetType()).GetMethod("EncodeToString").Invoke(null, new object[] { value });
			writer.WriteValue(serialized);
		}
	}
}
