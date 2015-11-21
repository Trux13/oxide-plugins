using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("AutoDoors", "Wulf/lukespragg", "2.2.3", ResourceId = 800)]
    [Description("Automatically closes doors behind players after X seconds.")]

    class AutoDoors : RustPlugin
    {
        // Do NOT edit this file, instead edit AutoDoors.json in server/<identity>/oxide/config

        #region Configuration

        // Messages
        string ChatCommand => GetConfig("ChatCommand", "ad");
        string CommandUsage => GetConfig("CommandUsage", "Usage:\n /ad to disable automatic doors\n /ad # (a number between 5 and 30)");
        string DelayDisabled => GetConfig("DelayDisabled", "Automatic door closing is now disabled");
        string DelaySetTo => GetConfig("DelaySetTo", "Automatic door closing delay set to {time}s");

        // Settings
        int DefaultDelay => GetConfig("DefaultDelay", 0);
        int MaximumDelay => GetConfig("MaximumDelay", 30);
        int MinimumDelay => GetConfig("MinimumDelay", 5);

        protected override void LoadDefaultConfig()
        {
            // Messages
            Config["ChatCommand"] = ChatCommand;
            Config["CommandUsage"] = CommandUsage;
            Config["DelayDisabled"] = DelayDisabled;
            Config["DelaySetTo"] = DelaySetTo;

            // Settings
            Config["DefaultDelay"] = DefaultDelay;
            Config["MaximumDelay"] = MaximumDelay;
            Config["MinimumDelay"] = MinimumDelay;

            SaveConfig();
        }

        #endregion

        #region General Setup

        readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("AutoDoors");
        Dictionary<ulong, int> playerPrefs = new Dictionary<ulong, int>();

        void Loaded()
        {
            LoadDefaultConfig();
            playerPrefs = dataFile.ReadObject<Dictionary<ulong, int>>();

            cmd.AddChatCommand(ChatCommand, this, "AutoDoorChatCmd");
        }

        #endregion

        #region Chat Command

        void AutoDoorChatCmd(BasePlayer player, string command, string[] args)
        {
            int time;
            if (args == null || args.Length != 1 || !int.TryParse(args[0], out time)) time = 0;

            if (time > MaximumDelay || time < MinimumDelay && time != 0)
            {
                PrintToChat(player, CommandUsage);
                return;
            }

            playerPrefs[player.userID] = time;
            dataFile.WriteObject(playerPrefs);

            PrintToChat(player, time == 0 ? DelayDisabled : DelaySetTo.Replace("{time}", time.ToString()));
        }

        #endregion

        #region Door Closing

        void OnDoorOpened(Door door, BasePlayer player)
        {
            if (door == null || !door.IsOpen() || door.LookupPrefabName().Contains("shutter")) return;

            int time;
            if (!playerPrefs.TryGetValue(player.userID, out time)) time = DefaultDelay;
            if (time == 0) return;

            timer.Once(time, () =>
            {
                if (!door || !door.IsOpen()) return;
                door.SetFlag(BaseEntity.Flags.Open, false);
                door.SendNetworkUpdateImmediate();
            });
        }

        #endregion

        #region Helper Methods

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        #endregion
    }
}