/*
TODO:
- Add no permission message for chat command
- Allow localization of item names
*/

using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("MasterKey", "Wulf/lukespragg", "0.4.3", ResourceId = 1151)]
    [Description("Gain access to any locked object with permissions.")]

    class MasterKey : RustPlugin
    {
        // Do NOT edit this file, instead edit MasterKey.json in oxide/config and MasterKey.en.json in oxide/lang,
        // or create a language file for another language using the 'en' file as a default.

        #region Localization

        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"ChatCommand", "masterkey"},
                {"Disabled", "MasterKey access is now disabled"},
                {"Enabled", "MasterKey access is now enabled"},
                {"MasterKeyUsed", "{0} ({1}) used master key at {2}"},
                {"UnlockedWith", "Unlocked {0} with master key!"}
            };
            lang.RegisterMessages(messages, this);
        }

        #endregion

        #region Configuration

        bool LogUsage => GetConfig("LogUsage", true);
        bool ShowMessages => GetConfig("ShowMessages", true);

        protected override void LoadDefaultConfig()
        {
            Config["LogUsage"] = LogUsage;
            Config["ShowMessages"] = ShowMessages;
            SaveConfig();
        }

        #endregion

        #region Initialization

        readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("MasterKey");
        Dictionary<ulong, bool> playerPrefs = new Dictionary<ulong, bool>();

        void Loaded()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            playerPrefs = dataFile.ReadObject<Dictionary<ulong, bool>>();
            cmd.AddChatCommand(GetMessage("ChatCommand"), this, "MasterKeyChatCmd");

            permission.RegisterPermission("masterkey.all", this);
            permission.RegisterPermission("masterkey.boxes", this);
            permission.RegisterPermission("masterkey.cells", this);
            permission.RegisterPermission("masterkey.cupboards", this);
            permission.RegisterPermission("masterkey.doors", this);
            permission.RegisterPermission("masterkey.gates", this);
            permission.RegisterPermission("masterkey.shops", this);
			permission.RegisterPermission("masterkey.hatches", this);
        }

        #endregion

        #region Chat Command

        void MasterKeyChatCmd(BasePlayer player)
        {
            if (!playerPrefs.ContainsKey(player.userID)) playerPrefs.Add(player.userID, true);
            playerPrefs[player.userID] = !playerPrefs[player.userID];
            dataFile.WriteObject(playerPrefs);

            PrintToChat(player, playerPrefs[player.userID] ? GetMessage("Enabled", player.UserIDString) : GetMessage("Disabled"));
        }

        #endregion

        #region Lock Access

        object CanUseDoor(BasePlayer player, BaseLock door)
        {
            var parent = door.parentEntity.Get(true);
            var prefab = parent.LookupPrefabName();

            if (!door.IsLocked()) return true;
            if (playerPrefs.ContainsKey(player.userID) && !playerPrefs[player.userID]) return null;

            if (prefab.Contains("box"))
            {
                if (!HasPermission(player.UserIDString, "masterkey.all") && !HasPermission(player.UserIDString, "masterkey.boxes")) return null;
                if (ShowMessages) PrintToChat(player, string.Format(GetMessage("UnlockedWith"), "box"));
                if (LogUsage) LogToFile(player, GetMessage("MasterKeyUsed"));
                return true;
            }

            if (prefab.Contains("cell"))
            {
                if (!HasPermission(player.UserIDString, "masterkey.all") && !HasPermission(player.UserIDString, "masterkey.cells")) return null;
                if (parent.IsOpen()) return true;
                if (ShowMessages) PrintToChat(player, string.Format(GetMessage("UnlockedWith"), "cell"));
                if (LogUsage) LogToFile(player, GetMessage("MasterKeyUsed"));
                return true;
            }

            if (prefab.Contains("door"))
            {
                if (!HasPermission(player.UserIDString, "masterkey.all") && !HasPermission(player.UserIDString, "masterkey.doors")) return null;
                if (parent.IsOpen()) return true;
                if (ShowMessages) PrintToChat(player, string.Format(GetMessage("UnlockedWith"), "door"));
                if (LogUsage) LogToFile(player, GetMessage("MasterKeyUsed"));
                return true;
            }

            if (prefab.Contains("fence.gate") || prefab.Contains("gates.external"))
            {
                if (!HasPermission(player.UserIDString, "masterkey.all") && !HasPermission(player.UserIDString, "masterkey.gates")) return null;
                if (parent.IsOpen()) return true;
                if (ShowMessages) PrintToChat(player, string.Format(GetMessage("UnlockedWith"), "gate"));
                if (LogUsage) LogToFile(player, GetMessage("MasterKeyUsed"));
                return true;
            }

            if (prefab.Contains("shopfront"))
            {
                if (!HasPermission(player.UserIDString, "masterkey.all") && !HasPermission(player.UserIDString, "masterkey.shops")) return null;
                if (parent.IsOpen()) return true;
                if (ShowMessages) PrintToChat(player, string.Format(GetMessage("UnlockedWith"), "shop"));
                if (LogUsage) LogToFile(player, GetMessage("MasterKeyUsed"));
                return true;
            }

            if (prefab.Contains("floor"))
            {
                if (!HasPermission(player.UserIDString, "masterkey.all") && !HasPermission(player.UserIDString, "masterkey.floor")) return null;
                if (parent.IsOpen()) return true;
                if (ShowMessages) PrintToChat(player, string.Format(GetMessage("UnlockedWith"), "floor"));
                if (LogUsage) LogToFile(player, GetMessage("MasterKeyUsed"));
                return true;
            }

            return null;
        }

        #endregion

        #region Cupboard Access

        void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        {
            var player = entity as BasePlayer;

            if (player == null || !(trigger is BuildPrivilegeTrigger)) return;
            if (playerPrefs.ContainsKey(player.userID) && !playerPrefs[player.userID]) return;
            if (!HasPermission(player.UserIDString, "masterkey.all") && !HasPermission(player.UserIDString, "masterkey.cupboards")) return;

            timer.Once(0.1f, () => player.SetPlayerFlag(BasePlayer.PlayerFlags.HasBuildingPrivilege, true));
            if (ShowMessages) PrintToChat(player, string.Format(GetMessage("UnlockedWith"), "cupboard"));
            if (LogUsage) LogToFile(player, GetMessage("UnlockedWith"));
        }

        #endregion

        #region Helper Methods

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);

        static void LogToFile(BasePlayer player, string message)
        {
            var dateTime = DateTime.Now.ToString("yyyy-M-d");
            var position = $"{player.transform.position.x}, {player.transform.position.y}, {player.transform.position.z}";
            message = message.Replace("{player}", player.displayName).Replace("{steamid}", player.UserIDString).Replace("{position}", position);
            ConVar.Server.Log($"oxide/logs/masterkeys_{dateTime}.txt", message);
        }

        bool HasPermission(string userId, string perm) => permission.UserHasPermission(userId, perm);

        #endregion
    }
}
