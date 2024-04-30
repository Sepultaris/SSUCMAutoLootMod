using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACE.Server.Entity;
using ACE.Server.WorldObjects;
using ACE.Entity.Enum;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace SSUCM_AutoLootMod
{
    [HarmonyPatchCategory(nameof(CreatureDeathPatch))]
    internal class CreatureDeathPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Creature), "GenerateTreasure", new Type[] { typeof(DamageHistoryInfo), typeof(Corpse) })]
        public static void PostGenerateTreasure(DamageHistoryInfo killer, Corpse corpse, ref Creature __instance, ref List<WorldObject> __result)
        {
            //Debugger.Break();
            Player player;
            
            try {
                player = killer.TryGetAttacker() as Player;
            }
            catch (Exception e)
            {
                return;
            }
            if (player == null)
            {
                return;
            }
            
            var workingAutoLootProfileString = player.GetProperty(Settings.WorkingAutoLootProfile);
            if (workingAutoLootProfileString == null)
            {
                workingAutoLootProfileString = JsonSerializer.Serialize(new AutoLootProfile());
            }
            AutoLootProfile workingAutoLootProfile = JsonSerializer.Deserialize<AutoLootProfile>(workingAutoLootProfileString);

            foreach (var kvp in corpse.Inventory)
            {
                var loot = kvp.Value;
                foreach (var ruleKeyPair in workingAutoLootProfile.Rules)
                {
                    var rule = ruleKeyPair.Value;
                    var ruleName = ruleKeyPair.Key;
                    var requirementsSatisfied = true;
                    foreach (var requirement in rule.Requirements)
                    {
                        foreach (var property in typeof(WorldObject).GetProperties())
                        { 
                            if (property.Name == requirement.PropertyName)
                            {
                                TypeConverter tc = TypeDescriptor.GetConverter(property.PropertyType);
                                var requirementValue = tc.ConvertFrom(requirement.Value) as IComparable;
                                var objectValue = property.GetValue(loot) as IComparable;
                                if (objectValue == null)
                                {
                                    requirementsSatisfied = false;
                                    continue;
                                }
                                switch (requirement.Comparison)
                                {
                                    case ComparisonType.Equal:
                                        if (!objectValue.Equals(requirementValue))
                                        {
                                            requirementsSatisfied = false;
                                        }
                                        break;
                                    case ComparisonType.NotEqual:
                                        if (!objectValue.Equals(requirementValue))
                                        {
                                            requirementsSatisfied = false;
                                        }
                                        break;
                                    case ComparisonType.LessThanOrEqual:
                                        if (objectValue.CompareTo(requirementValue) > 0)
                                        {
                                            requirementsSatisfied = false;
                                        }
                                        break;
                                    case ComparisonType.GreaterThanOrEqual:
                                        if (objectValue.CompareTo(requirementValue) < 0)
                                        {
                                            requirementsSatisfied = false;
                                        }
                                        break;
                                }
                            }
                        }
                        
                    }
                    if (requirementsSatisfied)
                    {
                        if (player.TryCreateInInventoryWithNetworking(loot))
                        {
                            corpse.Inventory.Remove(loot.Guid);
                            player.SendMessage("You looted " + loot.Name + " from " + corpse.Name + " using the " + ruleName + " rule.", ChatMessageType.Broadcast);
                        }
                    }
                }
            }
        }
    }
}
