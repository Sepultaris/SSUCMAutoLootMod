using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSUCM_AutoLootMod
{
    internal class AutoLootProfile
    {
        public string Name { get; set; } = "";

        public Dictionary<string, AutoLootRule> Rules { get; set; } = new Dictionary<string, AutoLootRule>();

    }
}
