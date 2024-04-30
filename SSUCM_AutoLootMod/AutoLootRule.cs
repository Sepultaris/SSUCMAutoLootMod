using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACE.Server.WorldObjects;
using ACE.Entity.Enum;
using ACE.Server.Entity;

namespace SSUCM_AutoLootMod
{
    internal class AutoLootRule
    {
        public string Name { get; set; } = "";
        public List<AutoLootRequirement> Requirements { get; set; } = new List<AutoLootRequirement>();
    }
}
