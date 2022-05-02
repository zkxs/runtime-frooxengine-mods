using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using FrooxEngine;
using System.Reflection.Emit;

namespace NeosModConfigurationExample
{
    public class NeosModTest : NeosMod
    {
        public override string Name => "NeosModTest";
        public override string Author => "runtime";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/zkxs/NeosModConfigurationExample";

        // this override lets us change optional settings in our configuration definition
        public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
        {
            builder
                .Version(new Version(1, 0, 0)) // manually set config version (default is 1.0.0)
                .AutoSave(false); // don't autosave on Neos shutdown (default is true)
        }

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("dev.zkxs.neosmodtest");
            PatchSomething(harmony);
        }
        private static void PatchSomething(Harmony harmony)
        {

            Type patchedType = typeof(DynamicVariableSpace.ValueManager<Camera>);

            MethodInfo original = AccessTools.DeclaredMethod(patchedType, "SetValue", new Type[] { typeof(Camera) }, new Type[] { typeof(Camera) });
            if (original == null)
            {
                Error("original is null");
                return;
            }

            MethodInfo patch = AccessTools.DeclaredMethod(typeof(NeosModTest), nameof(DoNothingPostfix));
            if (patch == null)
            {
                Error("patch is null, which means I really fucked up");
                return;
            }

            harmony.Patch(original, postfix: new HarmonyMethod(patch));
            Debug($"Method [{patchedType} :: {original}] patched!");
        }

        private static IEnumerable<CodeInstruction> DoNothingTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            Msg("DoNothingTranspiler has run");
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                Debug($"{i}: {codes[i]}");
            }

            return codes.AsEnumerable();
        }

        private static void DoNothingPostfix()
        {
            Msg("DoNothingPostFix has run");
        }
    }
}
