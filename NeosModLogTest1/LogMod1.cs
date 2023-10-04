using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeosModLogTest
{
    public class LogMod1: NeosMod
    {
        public override string Name => "LogMod1";
        public override string Author => "runtime";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/zkxs/NeosModLogTest";


        public override void OnEngineInit()
        {
            Msg("I am LogMod1, logging as me");
            LogMod2.Log();
        }
    }
}
