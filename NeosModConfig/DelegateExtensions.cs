// This file is part of NeosModLoader and is licensed under the GNU LGPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley.

using System;
using System.Collections.Generic;
using System.Linq;

namespace NeosModConfig
{
	internal static class DelegateExtensions
	{
		internal static void SafeInvoke(this Delegate del, params object[] args)
		{
			var exceptions = new List<Exception>();

			foreach (var handler in del.GetInvocationList())
			{
				try
				{
					handler.Method.Invoke(handler.Target, args);
				}
				catch (Exception ex)
				{
					exceptions.Add(ex);
				}
			}

			if (exceptions.Any())
			{
				throw new AggregateException(exceptions);
			}
		}
	}
}
