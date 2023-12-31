// This file is part of NoResetScale and is licensed under the GNU GPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley

using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NoResetScale
{
	public class NoResetScale : NeosMod
	{
		internal const string VERSION = "1.0.0";
		public override string Name => "NoResetScale";
		public override string Author => "runtime";
		public override string Version => VERSION;
		public override string Link => "https://github.com/zkxs/runtime-frooxengine-mods/NoResetScale";

		private static MethodInfo? _startTask;
		private static MethodInfo? _isAtScale;

		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("net.michaelripley.NoResetScale");

			// a function call we'll be searching for later
			_startTask = AccessTools.DeclaredMethod(typeof(Worker), nameof(Worker.StartTask), new Type[] { typeof(Func<Task>) });
			if (_startTask == null)
			{
				Error("Could not find method Worker.StartTask(Func<Task>)");
				return;
			}

			// another function call we'll be searching for later
			_isAtScale = AccessTools.DeclaredMethod(typeof(UserRoot), nameof(UserRoot.IsAtScale), new Type[] { typeof(float) });
			if (_isAtScale == null)
			{
				Error("Could not find method UserRoot.IsAtScale(float)");
				return;
			}

			// where we start reading IL from
			MethodInfo openContextMenu = AccessTools.DeclaredMethod(typeof(InteractionHandler), "OpenContextMenu");
			if (openContextMenu == null)
			{
				Error("Could not find method InteractionHandler.OpenContextMenu(*)");
				return;
			}

			// constructor for our target async method body
			MethodInfo? asyncMethodConstructor = FindAsyncMethod(PatchProcessor.GetOriginalInstructions(openContextMenu));
			if (asyncMethodConstructor == null)
			{
				Error("Could not find target async block constructor in InteractionHandler.OpenContextMenu(*)");
				return;
			}
			Debug($"Found async method constructor: \"{asyncMethodConstructor.FullDescription()}\"");

			// helpful attribute that points us to the async method body
			AsyncStateMachineAttribute asyncAttribute = (AsyncStateMachineAttribute)asyncMethodConstructor.GetCustomAttribute(typeof(AsyncStateMachineAttribute));
			if (asyncAttribute == null)
			{
				Error($"Could not find AsyncStateMachine for \"{asyncMethodConstructor.FullDescription()}\"");
				return;
			}

			// our actual async method body we want to patch
			MethodInfo asyncMethodBody = AccessTools.DeclaredMethod(asyncAttribute.StateMachineType, "MoveNext", new Type[] { });
			if (asyncMethodBody == null)
			{
				Error("Could not find target async block in InteractionHandler.OpenContextMenu(*)");
				return;
			}
			// should be FrooxEngine.InteractionHandler.'<>c__DisplayClass333_0'.'<<OpenContextMenu>b__0>d'.MoveNext()
			Debug($"Found async method: \"{asyncMethodBody.FullDescription()}\"");

			harmony.Patch(asyncMethodBody, transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(NoResetScale), nameof(Transpiler))));
		}

		// search for StartTask((Func<Task>) (async () => {...})) and grab the async method
		private static MethodInfo? FindAsyncMethod(List<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			for (int callIdx = 0; callIdx < codes.Count; callIdx++)
			{
				if (codes[callIdx].Calls(_startTask))
				{
					// idx-5 should be enough instructions to search to fiund the ldftn we're looking for
					for (int ldftnIdx = callIdx - 1; ldftnIdx >= Math.Max(0, callIdx - 5); ldftnIdx--)
					{
						if (codes[ldftnIdx].opcode.Equals(OpCodes.Ldftn))
						{
							return (MethodInfo)codes[ldftnIdx].operand;
						}
					}
				}
			}

			return null;
		}

		// search for IL matching if(FrooxEngine.UserRoot::IsAtScale) and make it unconditionally take the true branch
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			Debug($"There are {instructions.Count()} instructions");
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			for (int idx = 1; idx < codes.Count; idx++)
			{
				CodeInstruction call = codes[idx - 1];
				CodeInstruction branch = codes[idx];

				if (call.Calls(_isAtScale) && branch.opcode.Equals(OpCodes.Brfalse_S))
				{
					// replace the brfalse.s with a pop, which makes it always take the true branch.
					codes[idx] = new CodeInstruction(OpCodes.Pop);

					Msg("Transpiler succeeded");
					return codes.AsEnumerable();
				}
			}

			throw new TranspilerException("Failed to find IL matching if(FrooxEngine.UserRoot::IsAtScale)");
		}

		private class TranspilerException : Exception
		{
			public TranspilerException(string message) : base(message) { }
		}
	}
}
