﻿/*
TODO:
- Add command to see who has reserved.slot permission
- Add option to automatically make admin reserved
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Reserved", "Wulf/lukespragg", "1.1.0")]
    [Description("Allows players with permission to always be able to connect.")]

    class Reserved : CovalencePlugin
    {
        // Do NOT edit this file, instead edit Reserved.json in oxide/config and Reserved.en.json in the oxide/lang directory,
        // or create a new language file for another language using the 'en' file as a default.

        #region Configuration

        bool AutoAdminSlots => GetConfig("AutoAdminSlots", true);
        bool DynamicSlots => GetConfig("DynamicSlots", false);
        //bool IgnorePlayerLimit => GetConfig("IgnorePlayerLimit", false);
        bool KickForReserved => GetConfig("KickForReserved", false);
        int ReservedSlots => GetConfig("ReservedSlots", 5);

        protected override void LoadDefaultConfig()
        {
            Config["AutoAdminSlots"] = AutoAdminSlots;
            Config["DynamicSlots"] = DynamicSlots;
            //Config["IgnorePlayerLimit"] = IgnorePlayerLimit;
            Config["KickForReserved"] = KickForReserved;
            Config["ReservedSlots"] = ReservedSlots;
            SaveConfig();
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"KickedForReserved", "Kicked for reserved slot"},
                {"ReservedSlotsOnly", "Only reserved slots available"},
                {"SlotsNowAvailable", "{total} slot(s) now available"}
            }, this);
        }

        #endregion

        #region Initialization

        void Loaded()
        {
            #if !HURTWORLD && !REIGNOFKINGS && !RUST && !RUSTLEGACY
            throw new NotSupportedException("This plugin does not support this game");
            #endif

            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission("reserved.slot", this);
        }

        void OnServerInitialized()
        {
            if (!DynamicSlots) return;

            var slotCount = players.GetAllPlayers().Count(player => player.HasPermission("reserved.slot"));
            Config["ReservedSlots"] = slotCount;
            SaveConfig();

            Puts(GetMessage("SlotsNowAvailable").Replace("{total}", slotCount.ToString()));
        }

        #endregion

        #region Reserved Check

        string CheckForSlots(int currentPlayers, int maxPlayers, string userId)
        {
            if (AutoAdminSlots && IsAdmin(userId))

            if ((currentPlayers + ReservedSlots) >= maxPlayers && !HasPermission(userId, "reserved.slot"))
                return GetMessage("ReservedSlotsOnly", userId);

            //if (IgnorePlayerLimit) return null;
            /*if (currentPlayers >= maxPlayers)
            {
                // TODO: Kick random player with no reserved slot
                var targets = players.GetAllOnlinePlayers().ToArray();
                var target = players[(new Random()).Next(0, targets.Length)];
                if (!target.HasPermission("reserved.slot")) target.ConnectedPlayer.Kick(GetMessage("KickedForReserved", target.UniqueID));

                return null;
            }*/

            return null;
        }

        #if HURTWORLD
        object CanClientLogin(PlayerSession session)
        {
            return CheckForSlots(GameManager.Instance.GetPlayerCount(), GameManager.Instance.ServerConfig.MaxPlayers, session.SteamId.ToString());
        }
        #endif

        #if REIGNOFKINGS
        object OnUserApprove(CodeHatch.Engine.Core.Networking.ConnectionLoginData data)
        {
            var playerCount = CodeHatch.Engine.Networking.Server.PlayerCount;
            var playerLimit = CodeHatch.Engine.Networking.Server.PlayerLimit;
            return CheckForSlots(playerCount, playerLimit, data.PlayerId.ToString()) != null ? (object)ConnectionError.LimitedPlayers : null;
        }
        #endif

        #if RUST
        object CanClientLogin(Network.Connection connection)
        {
            return CheckForSlots(BasePlayer.activePlayerList.Count, ConVar.Server.maxplayers, connection.userid.ToString());
        }
        #endif

        #if RUSTLEGACY
        object CanClientLogin(ClientConnection connection)
        {
            return CheckForSlots(PlayerClient.All.Count, global::server.maxplayers, connection.UserID.ToString());
        }
        #endif

        #endregion

        #region Helper Methods

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);

        bool HasPermission(string userId, string perm) => permission.UserHasPermission(userId, perm);

        bool IsAdmin(string userId) => permission.UserHasGroup(userId, "admin");

        #endregion
    }
}
