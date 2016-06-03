﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Godmode", "Wulf/lukespragg", "3.1.0")]
    [Description("Allows players with permission to become invincible/invulnerable")]

    class Godmode : CovalencePlugin
    {
        // Do NOT edit this file, instead edit Godmode.json in oxide/config and Godmode.en.json in the oxide/lang directory,
        // or create a new language file for another language using the 'en' file as a default.

        #region Configuration

        const string permGod = "godmode.allowed";

        bool canBeHurt;
        bool canBeLooted;
        bool canHurtPlayers;
        bool canLootPlayers;
        bool infiniteRun;
        bool informOnAttack;
        bool prefixEnabled;
        string prefixFormat;

        protected override void LoadDefaultConfig()
        {
            Config["CanBeHurt"] = canBeHurt = GetConfig("CanBeHurt", false);
            Config["CanBeLooted"] = canBeLooted = GetConfig("CanBeLooted", false);
            Config["CanHurtPlayers"] = canHurtPlayers = GetConfig("CanHurtPlayers", true);
            Config["CanLootPlayers"] = canLootPlayers = GetConfig("CanLootPlayers", true);
            Config["InfiniteRun"] = infiniteRun = GetConfig("InfiniteRun", true);
            Config["InformOnAttack"] = informOnAttack = GetConfig("InformOnAttack", true);
            Config["PrefixEnabled"] = prefixEnabled = GetConfig("PrefixEnabled", true);
            Config["PrefixFormat"] = prefixFormat = GetConfig("PrefixFormat", "[God]");
            SaveConfig();
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
                {"Disabled", "You have disabled godmode"},
                {"DisabledBy", "Your godmode has been disabled by {0}"},
                {"DisabledFor", "You have disabled godmode for {0}"},
                {"Enabled", "You have enabled godmode"},
                {"EnabledBy", "Your godmode has been enabled by {0}"},
                {"EnabledFor", "You have enabled godmode for {0}"},
                {"Godlist", "Players with godmode enabled:"},
                {"GodlistNone", "No players have godmode enabled"},
                {"InformAttacker", "{0} is in godmode and can't take any damage"},
                {"InformVictim", "{0} just tried to deal damage to you"},
                {"NoLooting", "You are not allowed to loot a player with godmode"},
                {"NotAllowed", "You are not allowed to use this command"},
                {"PlayerNotFound", "No players were found with that name"}
            }, this);
        }

        #endregion

        #region Initialization

        void Loaded()
        {
            #if !RUST
            throw new NotSupportedException("This plugin does not support this game");
            #endif

            LoadDefaultConfig();
            LoadDefaultMessages();
            LoadSavedData();
            permission.RegisterPermission(permGod, this);

            #if RUST
            Unsubscribe(nameof(OnRunPlayerMetabolism));
            #endif
        }

        #endregion

        #region Stored Data

        StoredData storedData;
        readonly Hash<string, PlayerInfo> gods = new Hash<string, PlayerInfo>();
        readonly Dictionary<IPlayer, long> playerInformHistory = new Dictionary<IPlayer, long>();

        class StoredData
        {
            public readonly HashSet<PlayerInfo> Gods = new HashSet<PlayerInfo>();
        }

        class PlayerInfo
        {
            public string UserId;
            public string Name;

            public PlayerInfo()
            {
            }

            public PlayerInfo(IPlayer player)
            {
                UserId = player.Id;
                Name = player.Name;
            }
        }

        void LoadSavedData()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Title);
            foreach (var god in storedData.Gods) gods[god.UserId] = god;
        }

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Title, storedData);
        void OnServerSave() => SaveData();
        void Unload() => SaveData();

        #endregion

        readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        #region Rust Support
        #if RUST

        readonly FieldInfo displayName = typeof(BasePlayer).GetField("_displayName", (BindingFlags.NonPublic | BindingFlags.Instance));

        void OnPlayerInit(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerInit(player));
                return;
            }
            if (gods[player.UserIDString] == null) return;
            ModifyMetabolism(player, true);
            if (prefixEnabled && !player.displayName.Contains(prefixFormat))
                displayName.SetValue(player, $"{prefixFormat} {player.displayName}");
            else
                displayName.SetValue(player, player.displayName.Replace(prefixFormat, "").Trim());
        }

        object CanBeWounded(BasePlayer player) => !canBeHurt && gods[player.UserIDString] != null ? (object)false : null;

        void OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            var victim = entity as BasePlayer;
            var attacker = info.Initiator as BasePlayer;

            if (!victim) return;
            if (!canBeHurt && gods[victim.UserIDString] != null)
            {
                NullifyDamage(ref info);
                if (informOnAttack)
                    InformPlayers(covalence.Players.GetPlayer(victim.UserIDString), covalence.Players.GetPlayer(attacker?.UserIDString));
            }

            if (!attacker) return;
            if (!canHurtPlayers && gods[attacker.UserIDString] != null) NullifyDamage(ref info);
        }

        object CanLootPlayer(BasePlayer target, BasePlayer looter)
        {
            if ((!canBeLooted && gods[target.UserIDString] != null) || (!canLootPlayers && gods[looter.UserIDString] != null))
            {
                NextTick(() =>
                {
                    looter.EndLooting();
                    Reply(covalence.Players.GetPlayer(looter.UserIDString), Lang("NoLooting", looter.UserIDString));
                });
                return false;
            }

            return null;
        }

        void OnLootPlayer(BasePlayer looter, BasePlayer target)
        {
            if (!target) return;
            if (gods[target.UserIDString] != null)
            {
                NextTick(() =>
                {
                    looter.EndLooting();
                    Reply(covalence.Players.GetPlayer(looter.UserIDString), Lang("NoLooting", looter.UserIDString));
                });
            }
        }

        static void ModifyMetabolism(BasePlayer player, bool isGod)
        {
            if (isGod)
            {
                player.health = 100;
                player.metabolism.bleeding.max = 0;
                player.metabolism.bleeding.value = 0;
                player.metabolism.calories.min = 500;
                player.metabolism.calories.value = 500;
                player.metabolism.dirtyness.max = 0;
                player.metabolism.dirtyness.value = 0;
                player.metabolism.heartrate.min = 0.5f;
                player.metabolism.heartrate.max = 0.5f;
                player.metabolism.heartrate.value = 0.5f;
                player.metabolism.hydration.min = 250;
                player.metabolism.hydration.value = 250;
                player.metabolism.oxygen.min = 1;
                player.metabolism.oxygen.value = 1;
                player.metabolism.poison.max = 0;
                player.metabolism.poison.value = 0;
                player.metabolism.radiation_level.max = 0;
                player.metabolism.radiation_level.value = 0;
                player.metabolism.radiation_poison.max = 0;
                player.metabolism.radiation_poison.value = 0;
                player.metabolism.temperature.min = 32;
                player.metabolism.temperature.max = 32;
                player.metabolism.temperature.value = 32;
                player.metabolism.wetness.max = 0;
                player.metabolism.wetness.value = 0;
            }
            else
            {
                player.metabolism.bleeding.min = 0;
                player.metabolism.bleeding.max = 1;
                player.metabolism.calories.min = 0;
                player.metabolism.calories.max = 500;
                player.metabolism.comfort.min = 0;
                player.metabolism.comfort.max = 1;
                player.metabolism.dirtyness.min = 0;
                player.metabolism.dirtyness.max = 100;
                player.metabolism.heartrate.min = 0;
                player.metabolism.heartrate.max = 1;
                player.metabolism.hydration.min = 0;
                player.metabolism.hydration.max = 250;
                player.metabolism.oxygen.min = 0;
                player.metabolism.oxygen.max = 1;
                player.metabolism.poison.min = 0;
                player.metabolism.poison.max = 100;
                player.metabolism.radiation_level.min = 0;
                player.metabolism.radiation_level.max = 100;
                player.metabolism.radiation_poison.min = 0;
                player.metabolism.radiation_poison.max = 500;
                player.metabolism.temperature.min = -100;
                player.metabolism.temperature.max = 100;
                player.metabolism.wetness.min = 0;
                player.metabolism.wetness.max = 1;
            }
            player.metabolism.SendChangesToClient();
        }

        object OnRunPlayerMetabolism(PlayerMetabolism metabolism, BaseCombatEntity entity)
        {
            if (!(entity is BasePlayer)) return null;

            var player = entity.ToPlayer();
            player.SetPlayerFlag(BasePlayer.PlayerFlags.NoSprint, false);
            if (infiniteRun && gods[player.UserIDString] != null) return true;

            return null;
        }

        void EnableGodmode(BasePlayer player)
        {
            var info = new PlayerInfo(covalence.Players.GetPlayer(player.UserIDString));
            storedData.Gods.Add(info);
            gods[player.UserIDString] = info;
            ModifyMetabolism(player, true);
            if (prefixEnabled && !player.displayName.Contains(prefixFormat))
                displayName.SetValue(player, $"{prefixFormat} {player.displayName}");
            else
                displayName.SetValue(player, player.displayName.Replace(prefixFormat, "").Trim());
        }

        void DisableGodmode(BasePlayer player)
        {
            storedData.Gods.RemoveWhere(info => info.UserId == player.UserIDString);
            gods.Remove(player.UserIDString);
            ModifyMetabolism(player, false);
            displayName.SetValue(player, player.displayName.Replace(prefixFormat, "").Trim());
        }

        #endif
        #endregion

        #region Chat Commands

        [Command("god")]
        void God(IPlayer player, string command, string[] args)
        {
            if (!IsAllowed(player, permGod)) return;

            if (args.Length == 0)
            {
                if (gods[player.Id] != null)
                {
                    DisableGodmode(player);
                    Unsubscribe(nameof(OnRunPlayerMetabolism));
                    Reply(player, Lang("Disabled", player.Id));
                }
                else
                {
                    EnableGodmode(player);
                    Subscribe(nameof(OnRunPlayerMetabolism));
                    Reply(player, Lang("Enabled", player.Id));
                }

                return;
            }

            var target = covalence.Players.FindPlayer(args[0]);
            if (target == null)
                Reply(player, Lang("PlayerNotFound", player.Id));
            else
                ToggleGodmode(player, target);
        }

        [Command("gods")]
        void Godlist(IPlayer player, string command, string[] args)
        {
            if (!IsAllowed(player, permGod)) return;

            Reply(player, Lang("GodList", player.Id));
            if (gods.Count == 0)
                Reply(player, Lang("GodListNone", player.Id));
            else
                foreach (var god in gods) Reply(player, $"{god.Value.Name} [{god.Value.UserId}]");
        }

        #endregion

        void InformPlayers(IPlayer victim, IPlayer attacker)
        {
            if (!Equals(victim, attacker)) return;
            if (victim == attacker) return;

            if (!playerInformHistory.ContainsKey(attacker)) playerInformHistory.Add(attacker, 0);
            if (!playerInformHistory.ContainsKey(victim)) playerInformHistory.Add(victim, 0);

            if (GetTimestamp() - playerInformHistory[attacker] > 15)
            {
                Reply(victim, Lang("InformVictim", victim.Id), attacker.Name);
                playerInformHistory[victim] = GetTimestamp();
            }

            if (GetTimestamp() - playerInformHistory[victim] > 15)
            {
                Reply(attacker, Lang("InformAttacker", attacker.Id), victim.Name);
                playerInformHistory[victim] = GetTimestamp();
            }
        }

        static void NullifyDamage(ref HitInfo info)
        {
            info.damageTypes = new Rust.DamageTypeList();
            info.HitMaterial = 0;
            info.PointStart = Vector3.zero;
        }

        void ToggleGodmode(IPlayer player, IPlayer target)
        {
            if (gods[target.Id] != null)
            {
                DisableGodmode(target);
                Reply(player, Lang("DisabledFor", player.Id), target.Name);
                Reply(target, Lang("DisabledBy", player.Id), player.Name);
            }
            else
            {
                EnableGodmode(target);
                Reply(player, Lang("EnabledFor", player.Id), target.Name);
                Reply(target, Lang("EnabledBy", player.Id), player.Name);
            }
        }

        #region Helpers

        long GetTimestamp() => Convert.ToInt64((DateTime.UtcNow.Subtract(epoch)).TotalSeconds);

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        bool IsAllowed(IPlayer player, string perm)
        {
            if (permission.UserHasPermission(player.Id, perm)) return true;
            Reply(player, Lang("NotAllowed", player.Id));
            return false;
        }

        bool IsAdmin(string id) => permission.UserHasGroup(id, "admin");

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        static void Reply(IPlayer player, string message, params object[] args) => player.Reply(string.Format(message, args));

        #endregion
    }
}