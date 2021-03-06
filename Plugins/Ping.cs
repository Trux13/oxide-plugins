/*
TODO:
- Add ping strikes and banning support
*/

using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Ping", "Wulf/lukespragg", "1.5.4", ResourceId = 1921)]
    [Description("Ping command and automatic kicking of players with high pings")]

    class Ping : CovalencePlugin
    {
        // Do NOT edit this file, instead edit Ping.json in oxide/config and Ping.en.json in the oxide/lang directory,
        // or create a new language file for another language using the 'en' file as a default.

        #region Initialization

        const string permBypass = "ping.bypass";

        void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permBypass, this);
        }

        #endregion

        #region Configuration

        bool adminExcluded;
        int checkEvery;
        bool highPingKick;
        bool kickNotices;
        int pingLimit;
        bool repeatCheck;

        protected override void LoadDefaultConfig()
        {
            Config["AdminExcluded"] = adminExcluded = GetConfig("AdminExcluded", true);
            Config["CheckEvery"] = checkEvery = GetConfig("CheckEvery", 1800); // Seconds
            Config["HighPingKick"] = highPingKick = GetConfig("HighPingKick", true);
            Config["KickNotices"] = kickNotices = GetConfig("KickNotices", true);
            Config["PingLimit"] = pingLimit = GetConfig("PingLimit", 200); // Milliseconds
            Config["RepeatCheck"] = repeatCheck = GetConfig("RepeatCheck", true);
            SaveConfig();
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["HighPing"] = "Ping is too high: {0}ms",
                ["Kicked"] = "{0} kicked for high ping ({1}ms)",
                ["Ping"] = "You have a ping of {0}ms"
            }, this);
        }

        #endregion

        #region Chat Command

        [Command("ping", "pong")]
        void ChatPing(IPlayer player, string command, string[] args)
        {
            player.Reply(Lang("Ping", player.Id, player.ConnectedPlayer.Ping.ToString()));
        }

        #endregion

        #region Hooks

        void OnUserConnected(IPlayer player) => timer.Once(10f, () => CheckPing(player));

        void OnServerInitialized()
        {
            foreach (var player in players.GetAllOnlinePlayers())
                timer.Once(1f, () => CheckPing(player.BasePlayer));
        }

        #endregion

        #region Ping Check

        void CheckPing(IPlayer player)
        {
            if (player?.ConnectedPlayer == null) return;

            var ping = player.ConnectedPlayer.Ping;
            if (HasPermission(player.Id, permBypass) || (adminExcluded && IsAdmin(player.Id))) return;
            if (ping >= pingLimit && highPingKick) PingKick(player, ping.ToString());
            if (repeatCheck) timer.Every(checkEvery, () => CheckPing(player));
        }

        void PingKick(IPlayer player, string ping)
        {
            player.ConnectedPlayer.Kick(Lang("HighPing", player.Id, ping));

            if (kickNotices)
            {
                Puts(Lang("Kicked", null, player.Name, ping));
                server.Broadcast(Lang("Kicked", null, player.Name, ping));
            }
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        bool IsAdmin(string id) => permission.UserHasGroup(id, "admin");

        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
