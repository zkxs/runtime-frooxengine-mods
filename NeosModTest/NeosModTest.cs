using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NeosModConfigurationExample
{
	public class NeosModTest : NeosMod
	{
		public override string Name => "NeosModTest";
		public override string Author => "runtime";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/zkxs/NeosModConfigurationExample";

		[AutoRegisterConfigKey]
		private readonly ModConfigurationKey<bool> KEY_ENABLE = new ModConfigurationKey<bool>("enabled", "Enables the NeosModTest mod", () => true);

		// this override lets us change optional settings in our configuration definition
		public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
		{
			builder
				.Version(new Version(1, 0, 0)) // manually set config version (default is 1.0.0)
				.AutoSave(false); // don't autosave on Neos shutdown (default is true)
		}

		public override void OnEngineInit()
		{
			// disable the mod if the enabled config has been set to false
			ModConfiguration config = GetConfiguration();
			if (!config.GetValue(KEY_ENABLE)) // this is safe as the config has a default value
			{
				Debug("Mod disabled, returning early.");
				return;
			}

			Harmony harmony = new Harmony("dev.zkxs.neosmodtest");
			patchSlideGrabbed(harmony);
		}
		private static void patchSlideGrabbed(Harmony harmony)
		{
			MethodInfo original = AccessTools.DeclaredMethod(typeof(CommonTool), "SlideGrabbed", new Type[] { typeof(float), typeof(float) });
			if (original == null)
			{
				Error("original is null");
				return;
			}

			MethodInfo transpiler = AccessTools.DeclaredMethod(typeof(NeosModTest), nameof(SlideGrabbedTranspiler));
			if (transpiler == null)
			{
				Error("transpiler is null");
				return;
			}

			harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
			Debug("Method \"SlideGrabbed\" patched!");
		}

		private static IEnumerable<CodeInstruction> SlideGrabbedTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);
			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Stloc_2 && codes[i + 1].opcode == OpCodes.Ldarg_0 && codes[i + 2].opcode == OpCodes.Ldloc_2 && codes[i + 3].opcode == OpCodes.Ldarg_0 && codes[i + 4].opcode == OpCodes.Call && ((MethodInfo)codes[i + 4].operand == typeof(Worker).GetMethod("get_InputInterface")) && codes[i + 5].opcode == OpCodes.Callvirt && ((MethodInfo)codes[i + 5].operand == typeof(InputInterface).GetMethod("get_VR_Active")) && codes[i + 6].opcode == OpCodes.Brtrue_S)
				{
					codes[i + 3].opcode = OpCodes.Nop;  //Nop loading base reference onto stack
					codes[i + 4].opcode = OpCodes.Nop;  //Nop call to get InputInterface instance, that would use the base refernce
					codes[i + 5].opcode = OpCodes.Nop;  //Nop callvirt for VR_Active getter
					codes[i + 6].opcode = OpCodes.Br_S;  //Exchange jump on true for unconditional jump
				}

				if (codes[i].opcode == OpCodes.Ldloc_2 && codes[i + 1].opcode == OpCodes.Ldc_R4 && codes[i + 2].opcode == OpCodes.Bge_Un && codes[i + 3].opcode == OpCodes.Ldarg_0 && codes[i + 4].opcode == OpCodes.Call && ((MethodInfo)codes[i + 4].operand == typeof(Worker).GetMethod("get_InputInterface")) && codes[i + 5].opcode == OpCodes.Callvirt && ((MethodInfo)codes[i + 5].operand == typeof(InputInterface).GetMethod("get_VR_Active")) && codes[i + 6].opcode == OpCodes.Brfalse)
				{
					codes[i + 3].opcode = OpCodes.Nop;  //Nop loading base reference onto stack
					codes[i + 4].opcode = OpCodes.Nop;  //Nop call to get InputInterface instance, that would use the base refernce
					codes[i + 5].opcode = OpCodes.Nop;  //Nop callvirt for VR_Active getter
					codes[i + 6].opcode = OpCodes.Nop;  //Nop the jump on false
				}
			}
			return codes.AsEnumerable();
		}
	}
}
