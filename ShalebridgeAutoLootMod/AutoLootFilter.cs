using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACE.Server.WorldObjects;
using ACE.Entity.Enum;

namespace ShalebridgeBaseMod
{
    internal class AutoLootFilter
    { 
        public ItemType? ItemType = null;

        public string? Name = null;

        public int? MinArmorLevel = null;

        public int? Damage = null;

        public DamageType? DamageType = null;

        public float? ElementalDamageMod = null;

        public float? DamageMod = null;

        public Skill? WieldSkillType = null;
        
        public WieldRequirement? MinWieldRequirement = null;

        public int? WieldDifficulty = null;

        public int? MinAvereageRatings = null;

        public string[]? Cantrips = null;

    }
}
