using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("NeosModLoader")]
[assembly: AssemblyProduct("NeosModLoader")]
[assembly: AssemblyCompany("Runtime Evil Inc.")]
[assembly: AssemblyCopyright("Copyright © 2021")]
[assembly: AssemblyDescription("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion(NeosModLoader.ModLoader.VERSION_CONSTANT)]
[assembly: AssemblyFileVersion(NeosModLoader.ModLoader.VERSION_CONSTANT)]

// prevent PostX from modifying my assembly, as it doesn't need anything done to it
// this keeps PostX from overwriting my AssemblyVersionAttribute
[module: Description("POSTX_PROCESSED")]
