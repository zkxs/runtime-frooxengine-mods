using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            Harmony.DEBUG = true;

            Type type = typeof(DynamicVariableSpace.ValueManager<>);
            Debug($"type: {type}");

            // get the generic method
            MethodInfo genericOriginal = AccessTools.DeclaredMethod(type, "SetValue");
            if (genericOriginal == null)
            {
                Error("genericOriginal is null");
                return;
            }
            Debug($"genericOriginal: {genericOriginal}");
            Debug($"genericOriginal.IsGenericMethod // {genericOriginal.IsGenericMethod}");
            Debug($"genericOriginal.IsGenericMethodDefinition // {genericOriginal.IsGenericMethodDefinition}");
            
            // close the generic method
            MethodInfo original = genericOriginal.MakeGenericMethod(new Type[] { typeof(object) });
            if (original == null)
            {
                Error("original is null");
                return;
            }
            Debug($"original: {original}");
            Debug($"original.IsGenericMethod // {original.IsGenericMethod}");
            Debug($"original.IsGenericMethodDefinition // {original.IsGenericMethodDefinition}");

            MethodInfo patch = AccessTools.DeclaredMethod(typeof(NeosModTest), nameof(DoNothingPostfix));
            if (patch == null)
            {
                Error("patch is null, which means I really fucked up");
                return;
            }

            harmony.Patch(original, postfix: new HarmonyMethod(patch));
            Debug($"Method [{type} :: {original}] patched!");
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
            Warn("DoNothingPostFix has run");
        }

        private static void DoNothingPostfix2(object[] __args)
        {
            if (__args != null)
            {
                IEnumerable<string> argData = __args.Select(arg => $"{arg?.GetType()}: {arg}");
                Msg($"DoNothingPostFix has run with args [{string.Join(", ", argData)}]");
            }
            else
            {
                Warn($"DoNothingPostFix has run with args NULL");
            }
        }

        private static void LogMatchingTypes()
        {
            Type genericType = typeof(DynamicVariableSpace.ValueManager<>);
            Msg($"Logging types matching {genericType}:");

            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsGenericType)
                .Where(type => genericType.Equals(type.GetGenericTypeDefinition()));

            foreach (Type type in types)
            {
                Msg($"Type matching {genericType}: {type}");
            }
        }

        private static void LogAssemblies()
        {
            Debug("Assembly list:");
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Debug($"- {assembly}");
            }
        }
    }
}
