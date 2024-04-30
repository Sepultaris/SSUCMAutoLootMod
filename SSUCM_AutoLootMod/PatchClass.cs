using System.ComponentModel;
using System.Linq;
using System.Reflection;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Command;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.WorldObjects;
using Newtonsoft.Json.Linq;
using SSUCM_AutoLootMod;


namespace SSUCM_AutoLootMod
{
    [HarmonyPatch]
    public class PatchClass
    {
        #region Settings
        const int RETRIES = 10;

        public static Settings Settings = new();
        static string settingsPath => Path.Combine(Mod.ModPath, "Settings.json");
        private FileInfo settingsInfo = new(settingsPath);

        private JsonSerializerOptions _serializeOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private void SaveSettings()
        {
            string jsonString = JsonSerializer.Serialize(Settings, _serializeOptions);

            if (!settingsInfo.RetryWrite(jsonString, RETRIES))
            {
                ModManager.Log($"Failed to save settings to {settingsPath}...", ModManager.LogLevel.Warn);
                Mod.State = ModState.Error;
            }
        }

        private void LoadSettings()
        {
            if (!settingsInfo.Exists)
            {
                ModManager.Log($"Creating {settingsInfo}...");
                SaveSettings();
            }
            else
                ModManager.Log($"Loading settings from {settingsPath}...");

            if (!settingsInfo.RetryRead(out string jsonString, RETRIES))
            {
                Mod.State = ModState.Error;
                return;
            }

            try
            {
                Settings = JsonSerializer.Deserialize<Settings>(jsonString, _serializeOptions);
            }
            catch (Exception)
            {
                ModManager.Log($"Failed to deserialize Settings: {settingsPath}", ModManager.LogLevel.Warn);
                Mod.State = ModState.Error;
                return;
            }
        }
        #endregion

        #region Start/Shutdown
        public void Start()
        {
            //Need to decide on async use
            Mod.State = ModState.Loading;
            LoadSettings();

            if (Mod.State == ModState.Error)
            {
                ModManager.DisableModByPath(Mod.ModPath);
                return;
            }

            Mod.State = ModState.Running;
        }

        public void Shutdown()
        {
            //if (Mod.State == ModState.Running)
            // Shut down enabled mod...

            //If the mod is making changes that need to be saved use this and only manually edit settings when the patch is not active.
            //SaveSettings();

            if (Mod.State == ModState.Error)
                ModManager.Log($"Improper shutdown: {Mod.ModPath}", ModManager.LogLevel.Error);
        }
        #endregion

        #region Patches

        [CommandHandler("al_clearprofile", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld)]
        public static void HandleAutoLootClearProfile(Session session, params string[] parameters)
        {
            var player = session.Player;
            player.SetProperty(Settings.WorkingAutoLootProfile, JsonSerializer.Serialize(new AutoLootProfile()));
        }

        [CommandHandler("al_removerule", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld)]
        public static void HandleAutoLootRemoveRule(Session session, params string[] parameters)
        {
            var player = session.Player;
            if (parameters.Length != 1)
            {
                player.SendMessage("Usage: /al_removerule <name>", ChatMessageType.Broadcast);
                return;
            }

            var workingAutoLootProfileString = player.GetProperty(Settings.WorkingAutoLootProfile);
            if (workingAutoLootProfileString == null)
            {
                workingAutoLootProfileString = JsonSerializer.Serialize(new AutoLootProfile());
            }
            AutoLootProfile workingAutoLootProfile = JsonSerializer.Deserialize<AutoLootProfile>(workingAutoLootProfileString);
            if (workingAutoLootProfile.Rules.ContainsKey(parameters[0]))
            {
                workingAutoLootProfile.Rules.Remove(parameters[0]);
            }
            else
            {
                player.SendMessage("Rule not found", ChatMessageType.Broadcast);
                return;
            }
            player.SetProperty(Settings.WorkingAutoLootProfile, JsonSerializer.Serialize(workingAutoLootProfile));
        }

        [CommandHandler("al_setrule", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld)]
        public static void HandleAutoLootSetRule(Session session, params string[] parameters)
        {
            var player = session.Player;
            if (parameters.Length == 0)
            {
                player.SendMessage("Usage: /al_setrule <name> <requirements>", ChatMessageType.Broadcast);
                return;
            }
            var workingAutoLootProfileString = player.GetProperty(Settings.WorkingAutoLootProfile);
            if (workingAutoLootProfileString == null)
            {
                workingAutoLootProfileString = JsonSerializer.Serialize(new AutoLootProfile());
            }
            AutoLootProfile workingAutoLootProfile = JsonSerializer.Deserialize<AutoLootProfile>(workingAutoLootProfileString);

            var ruleName = parameters[0];
            var rule = new AutoLootRule();
            if (workingAutoLootProfile.Rules.ContainsKey(ruleName))
            {
                rule = workingAutoLootProfile.Rules[ruleName];
            }
            else
            {
                rule = new AutoLootRule();
                workingAutoLootProfile.Rules.Add(ruleName, rule);
            }
            for (int i = 1; i < parameters.Length - 1; i++)
            {
                foreach(var property in typeof(WorldObject).GetProperties())
                {
                    if ("-" + property.Name == parameters[i])
                    {
                        var requirement = new AutoLootRequirement();
                        requirement.Value = parameters[++i];
                        requirement.PropertyName = property.Name;
                        requirement.Comparison = ComparisonType.GreaterThanOrEqual;
                        rule.Requirements.Add(requirement);
                    }
                    else if ("+" + property.Name == parameters[i])
                    {
                        var requirement = new AutoLootRequirement();
                        requirement.Value = parameters[++i];
                        requirement.PropertyName = property.Name;
                        requirement.Comparison = ComparisonType.LessThanOrEqual;
                        rule.Requirements.Add(requirement);
                    }
                    else if ("=" + property.Name == parameters[i])
                    {
                        var requirement = new AutoLootRequirement();
                        requirement.Value = parameters[++i];
                        requirement.PropertyName = property.Name;
                        requirement.Comparison = ComparisonType.Equal;
                        rule.Requirements.Add(requirement);
                    }
                    else if ("!" + property.Name == parameters[i])
                    {
                        var requirement = new AutoLootRequirement();
                        requirement.Value = parameters[++i];
                        requirement.PropertyName = property.Name;
                        requirement.Comparison = ComparisonType.NotEqual;
                        rule.Requirements.Add(requirement);
                    }
                }
            }
            player.SetProperty(Settings.WorkingAutoLootProfile, JsonSerializer.Serialize(workingAutoLootProfile));
        }

        [CommandHandler("al_createprofile", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld)]
        public static void HandleAutoLootCreateProfile(Session session, params string[] parameters)
        {
            var player = session.Player;
            if (parameters.Length == 0)
            {
                player.SendMessage("Usage: /al_createprofile <name>", ChatMessageType.Broadcast);
                return;
            }
            var autoLootProfilesString = player.GetProperty(Settings.AutoLootProfiles);
            if (autoLootProfilesString == null)
            {
                autoLootProfilesString = JsonSerializer.Serialize(new Dictionary<string, AutoLootProfile>());
            }
            var autoLootProfiles = JsonSerializer.Deserialize<Dictionary<string, AutoLootProfile>>(autoLootProfilesString);
            if (autoLootProfiles.ContainsKey(parameters[0]))
            {
                player.SendMessage("Profile already exists.", ChatMessageType.Broadcast);
                return;
            }
            var autoLootProfile = new AutoLootProfile();
            autoLootProfile.Name = parameters[0];
            autoLootProfiles.Add(parameters[0], autoLootProfile);
            player.SetProperty(Settings.AutoLootProfiles, JsonSerializer.Serialize(autoLootProfiles));
        }

        [CommandHandler("al_loadprofile", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld)]
        public static void HandleAutoLootLoadProfile(Session session, params string[] parameters)
        {
            var player = session.Player;
            if (parameters.Length == 0)
            {
                player.SendMessage("Usage: /al_loadprofile <name>", ChatMessageType.Broadcast);
                return;
            }
            var autoLootProfilesString = player.GetProperty(Settings.AutoLootProfiles);
            if (autoLootProfilesString == null)
            {
                autoLootProfilesString = JsonSerializer.Serialize(new Dictionary<string, AutoLootProfile>());
            }
            var autoLootProfiles = JsonSerializer.Deserialize<Dictionary<string, AutoLootProfile>>(autoLootProfilesString);
            if (!autoLootProfiles.ContainsKey(parameters[0]))
            {
                player.SendMessage("Profile does not exist.", ChatMessageType.Broadcast);
                return;
            }
            var workingAutoLootProfile = autoLootProfiles[parameters[0]];

            player.SetProperty(Settings.WorkingAutoLootProfile, JsonSerializer.Serialize(workingAutoLootProfile));
        }

        [CommandHandler("al_removeprofile", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld)]
        public static void HandleAutoLootRemoveProfile(Session session, params string[] parameters)
        {
            var player = session.Player;
            if (parameters.Length == 0)
            {
                player.SendMessage("Usage: /al_loadprofile <name>", ChatMessageType.Broadcast);
                return;
            }
            var autoLootProfilesString = player.GetProperty(Settings.AutoLootProfiles);
            if (autoLootProfilesString == null)
            {
                autoLootProfilesString = JsonSerializer.Serialize(new Dictionary<string, AutoLootProfile>());
            }
            var autoLootProfiles = JsonSerializer.Deserialize<Dictionary<string, AutoLootProfile>>(autoLootProfilesString);
            if (!autoLootProfiles.ContainsKey(parameters[0]))
            {
                player.SendMessage("Profile does not exist.", ChatMessageType.Broadcast);
                return;
            }
            else
            {
                autoLootProfiles.Remove(parameters[0]);
            }
            player.SetProperty(Settings.AutoLootProfiles, JsonSerializer.Serialize(autoLootProfiles));
        }

        [CommandHandler("al_saveprofile", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld)]
        public static void HandleAutoLootSaveProfile(Session session, params string[] parameters)
        {
            var player = session.Player;
            if (parameters.Length == 0)
            {
                player.SendMessage("Usage: /al_saveprofile <name>", ChatMessageType.Broadcast);
                return;
            }
            var autoLootProfilesString = player.GetProperty(Settings.AutoLootProfiles);
            if (autoLootProfilesString == null)
            {
                autoLootProfilesString = JsonSerializer.Serialize(new Dictionary<string, AutoLootProfile>());
            }
            var autoLootProfiles = JsonSerializer.Deserialize<Dictionary<string, AutoLootProfile>>(autoLootProfilesString);
            if (!autoLootProfiles.ContainsKey(parameters[0]))
            {
                player.SendMessage("Profile does not exist.", ChatMessageType.Broadcast);
                return;
            }
            var workingAutoLootProfileString = player.GetProperty(Settings.WorkingAutoLootProfile);
            if (workingAutoLootProfileString == null)
            {
                workingAutoLootProfileString = JsonSerializer.Serialize(new AutoLootProfile());
            }
            AutoLootProfile workingAutoLootProfile = JsonSerializer.Deserialize<AutoLootProfile>(workingAutoLootProfileString);
            workingAutoLootProfile.Name = parameters[0];
            autoLootProfiles[parameters[0]] = workingAutoLootProfile;

            player.SetProperty(Settings.AutoLootProfiles, JsonSerializer.Serialize(autoLootProfiles));
        }

        [CommandHandler("al_printprofile", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld)]
        public static void HandleAutoLootPrintProfile(Session session, params string[] parameters)
        {
            var player = session.Player;
            
            var workingAutoLootProfileString = player.GetProperty(Settings.WorkingAutoLootProfile);
            if (workingAutoLootProfileString == null)
            {
                workingAutoLootProfileString = JsonSerializer.Serialize(new AutoLootProfile());
            }
            player.SendMessage(workingAutoLootProfileString, ChatMessageType.Broadcast);
        }
        #endregion
    }
}