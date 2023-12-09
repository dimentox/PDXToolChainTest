using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossal.PSI.Environment;
using System.IO;
using Colossal.IO.AssetDatabase;

namespace test
{
    [FileLocation(nameof(test))]
    public class MySettings
    {
        public int IntSetting { get; set; } = 5;
        public bool BoolSetting { get; set; }
    }

    [FileLocation(nameof(test))]
    public class MyOptions
    {
        public string StringSetting { get; set; }
    }
}
