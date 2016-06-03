using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Hooks", "Oxide Team", "0.1.0")]
    [Description("Tests all of the available Oxide hooks")]

    class Hooks : CovalencePlugin
    {
        #region Hook Verification

        int hookCount;
        int hooksVerified;
        Dictionary<string, bool> hooksRemaining = new Dictionary<string, bool>();

        public void HookCalled(string hook)
        {
            if (!hooksRemaining.ContainsKey(hook)) return;
            hookCount--;
            hooksVerified++;
            PrintWarning($"{hook} is working");
            hooksRemaining.Remove(hook);
            PrintWarning(hookCount == 0 ? "All hooks verified!" : $"{hooksVerified} hooks verified, {hookCount} hooks remaining");
        }

        #endregion

        #region Plugin Hooks (universal)

        private void Init()
        {
            hookCount = hooks.Count;
            hooksRemaining = hooks.Keys.ToDictionary(k => k, k => true);
            PrintWarning("{0} hook to test!", hookCount);
            HookCalled("Init");
        }

        protected override void LoadDefaultConfig() => HookCalled("LoadDefaultConfig");

        private void Loaded() => HookCalled("Loaded");

        private void Unloaded() => HookCalled("Unloaded");

        private void OnFrame() => HookCalled("OnFrame");

        #endregion

        #region Server Hooks (universal)

        private void OnServerInitialized() => HookCalled("OnServerInitialized");

        private void OnServerSave() => HookCalled("OnServerSave");

        private void OnServerShutdown() => HookCalled("OnServerShutdown");

        #endregion

        #region Player Hooks (covalence)

        private object CanUserLogin(string name, string id)
        {
            HookCalled("CanUserLogin");
            PrintWarning($"{name} ({id}) is attempting to login");
            return null;
        }

        private void OnUserApproved(string name, string id)
        {
            HookCalled("OnUserApproved");
            PrintWarning($"{name} ({id}) has been approved");
        }

        private object OnUserChat(IPlayer player, string message)
        {
            HookCalled("OnUserChat");
            PrintWarning($"{player.Name} said: {message}");
            return null;
        }

        private void OnUserConnected(IPlayer player)
        {
            HookCalled("OnUserConnected");
            PrintWarning($"{player.Name} ({player.Id}) connected from {player.ConnectedPlayer.Address}");
            if (player.ConnectedPlayer.IsAdmin()) PrintWarning($"{player.Name} is admin");
        }

        private void OnUserDisconnected(IPlayer player, string reason)
        {
            HookCalled("OnUserDisconnected");
            PrintWarning($"{player.Name} ({player.Id}) disconnected for: {reason ?? "Unknown"}");
        }

        private void OnUserInit(IPlayer player)
        {
            HookCalled("OnUserInit");
            PrintWarning($"{player.Name ?? "Unnamed"} initialized");
        }

        private void OnUserSpawn(IPlayer player)
        {
            HookCalled("OnUserSpawn");
            PrintWarning($"{player.Name} is spawning now");
        }

        private void OnUserSpawned(IPlayer player)
        {
            HookCalled("OnUserSpawned");
            PrintWarning($"{player.Name} spawned at {player.ConnectedPlayer.Character.Position()}");
        }

        private void OnUserRespawn(IPlayer player)
        {
            HookCalled("OnUserRespawn");
            PrintWarning($"{player.Name} is respawning now");
        }

        private void OnUserRespawned(IPlayer player)
        {
            HookCalled("OnUserRespawned");
            PrintWarning($"{player.Name} respawned at {player.ConnectedPlayer.Character.Position()}");
        }

        #endregion

#if HIDEHOLDOUT

        #region Server Hooks

        private void OnServerCommand(string command)
        {
            HookCalled("OnServerCommand");
        }

        #endregion

        #region Player Hooks

        private void OnChatCommand(PlayerInfos player, string command)
        {
            HookCalled("OnChatCommand");
        }

        private void OnPlayerDeath(PlayerInfos player)
        {
            PrintWarning("OnPlayerDeath");
        }

        #endregion

#endif

#if HURTWORLD

        #region Player Hooks

        private object OnChatCommand(PlayerSession session, string command)
        {
            HookCalled("OnChatCommand");
            return null;
        }

        private void OnPlayerDeath(PlayerSession session, EntityEffectSourceData source)
        {
            HookCalled("OnPlayerDeath");
        }

        #endregion

        #region Vehicle Hooks

        private object CanEnterVehicle(PlayerSession session, CharacterMotorSimple passenger)
        {
            HookCalled("CanEnterVehicle");
            return null;
        }

        private object CanExitVehicle(PlayerSession session, CharacterMotorSimple passenger)
        {
            HookCalled("CanExitVehicle");
            return null;
        }

        private void OnEnterVehicle(PlayerSession session, CharacterMotorSimple passenger)
        {
            HookCalled("OnEnterVehicle");
        }

        private void OnExitVehicle(PlayerSession session, CharacterMotorSimple passenger)
        {
            HookCalled("OnExitVehicle");
        }

        #endregion

#endif

#if REIGNOFKINGS

        #region Entity Hooks

        private void OnEntityHealthChange(CodeHatch.Networking.Events.Entities.EntityDamageEvent e)
        {
            HookCalled("OnEntityHealthChange");
        }

        private void OnEntityDeath(CodeHatch.Networking.Events.Entities.EntityDeathEvent e)
        {
            HookCalled("OnEntityDeath");
        }

        #endregion

        #region Structure Hooks

        private void OnCubePlacement(CodeHatch.Blocks.Networking.Events.CubePlaceEvent evt)
        {
            HookCalled("OnCubePlacement");
        }

        private void OnCubeTakeDamage(CodeHatch.Blocks.Networking.Events.CubeDamageEvent evt)
        {
            HookCalled("OnCubeTakeDamage");
        }

        private void OnCubeDestroyed(CodeHatch.Blocks.Networking.Events.CubeDestroyEvent evt)
        {
            HookCalled("OnCubeDestroyed");
        }

        #endregion

#endif

#if RUST

        #region Server Hooks

        private void OnTick() => HookCalled("OnTick");

        private void OnTerrainInitialized() => HookCalled("OnTerrainInitialized");

        private object OnRunCommand(ConsoleSystem.Arg arg)
        {
            HookCalled("OnRunCommand");
            // TODO: Print command messages
            return null;
        }

        #endregion

        #region Player Hooks

        private bool CanBypassQueue(Network.Connection connection)
        {
            HookCalled("CanBypassQueue");
            return true;
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            HookCalled("OnServerCommand");
            return null;
        }

        private bool CanEquipItem(PlayerInventory inventory, Item item)
        {
            HookCalled("CanEquipItem");
            return true;
        }

        private bool CanWearItem(PlayerInventory inventory, Item item)
        {
            HookCalled("CanWearItem");
            return true;
        }

        private void OnFindSpawnPoint()
        {
            HookCalled("OnFindSpawnPoint");
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            HookCalled("OnPlayerInput");
        }

        private void OnRunPlayerMetabolism(PlayerMetabolism metabolism)
        {
            HookCalled("OnRunPlayerMetabolism");
            // TODO: Print new metabolism values
        }

        #endregion

        #region Entity Hooks

        private void OnAirdrop(CargoPlane plane, UnityEngine.Vector3 location)
        {
            HookCalled("OnAirdrop");
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            HookCalled("OnEntityTakeDamage");
        }

        private void OnEntityBuilt(Planner planner, UnityEngine.GameObject go)
        {
            HookCalled("OnEntityBuilt");
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            HookCalled("OnEntityDeath");
            // TODO: Print player died
            // TODO: Automatically respawn admin after X time
        }

        private void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        {
            HookCalled("OnEntityEnter");
        }

        private void OnEntityLeave(TriggerBase trigger, BaseEntity entity)
        {
            HookCalled("OnEntityLeave");
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            HookCalled("OnEntitySpawned");
        }

        #endregion

        #region Item Hooks

        private void OnItemCraft(ItemCraftTask item)
        {
            HookCalled("OnItemCraft");
            // TODO: Print item crafting
        }

        private void OnItemDeployed(Deployer deployer, BaseEntity entity)
        {
            HookCalled("OnItemDeployed");
            // TODO: Print item deployed
        }

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            HookCalled("OnCollectiblePickup");
            // TODO: Print item picked up
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            HookCalled("OnItemAddedToContainer");
            // TODO: Print item added
        }

        private void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            HookCalled("OnItemRemovedToContainer");
            // TODO: Print item removed
        }

        private void OnConsumableUse(Item item)
        {
            HookCalled("OnConsumableUse");
            // TODO: Print consumable item used
        }

        private void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            HookCalled("OnConsumeFuel");
            // TODO: Print fuel consumed
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            HookCalled("OnDispenserGather");
            // TODO: Print item to be gathered
        }

        private void OnCropGather(PlantEntity plant, Item item, BasePlayer player)
        {
            HookCalled("OnCropGather");
            // TODO: Print item to be gathered
        }

        private void OnSurveyGather(SurveyCharge survey, Item item)
        {
            HookCalled("OnSurveyGather");
        }

        private void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            HookCalled("OnQuarryGather");
        }

        private void OnQuarryEnabled() => HookCalled("OnQuarryEnabled");

        private void OnTrapArm(BearTrap trap)
        {
            HookCalled("OnTrapArm");
        }

        private void OnTrapSnapped(BaseTrapTrigger trap, UnityEngine.GameObject go)
        {
            HookCalled("OnTrapSnapped");
        }

        private void OnTrapTrigger(BaseTrap trap, UnityEngine.GameObject go)
        {
            HookCalled("OnTrapTrigger");
        }

        #endregion

        #region Structure Hooks

        private void CanUseDoor(BasePlayer player, BaseLock door)
        //private void CanUseDoor(BasePlayer player, CodeLock door)
        //private void CanUseDoor(BasePlayer player, KeyLock door)
        {
            HookCalled("CanUseDoor");
        }

        private void OnStructureDemolish(BuildingBlock block, BasePlayer player)
        {
            HookCalled("OnStructureDemolish");
        }

        private void OnStructureRepair(BuildingBlock block, BasePlayer player)
        {
            HookCalled("OnStructureRepair");
        }

        private void OnStructureRotate(BuildingBlock block, BasePlayer player)
        {
            HookCalled("OnStructureRotate");
        }

        private void OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            HookCalled("OnStructureUpgrade");
        }

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            HookCalled("OnHammerHit");
        }

        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            HookCalled("OnWeaponFired");
        }

        private void OnMeleeThrown(BasePlayer player, Item item)
        {
            HookCalled("OnMeleeThrown");
        }

        private void OnItemThrown(BasePlayer player, BaseEntity entity)
        {
            HookCalled("OnItemThrown");
        }

        private void OnSignLocked(Signage sign, BasePlayer player)
        {
            HookCalled("OnSignLocked");
        }

        private void OnSignUpdated(Signage sign, BasePlayer player)
        {
            HookCalled("OnSignUpdated");
        }

        private void OnDoorClosed(Door door, BasePlayer player)
        {
            HookCalled("OnDoorClosed");
        }

        private void OnDoorOpened(Door door, BasePlayer player)
        {
            HookCalled("OnDoorOpened");
        }

        private void OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            HookCalled("OnCupboardAuthorize");
        }

        private void OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            HookCalled("OnCupboardDeauthorize");
        }

        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            HookCalled("OnRocketLaunched");
        }

        private void OnTrapArm(BearTrap trap, BasePlayer player)
        {
            HookCalled("OnTrapArm");
        }

        private void OnTrapDisarm(Landmine trap, BasePlayer player)
        {
            HookCalled("OnTrapDisarm");
        }

        private void OnTrapSnapped(BearTrap trap, UnityEngine.GameObject go)
        {
            HookCalled("OnTrapSnapped");
        }

        private void OnTrapTrigger(BearTrap trap, UnityEngine.GameObject go)
        {
            HookCalled("OnTrapTrigger");
        }

        private void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            HookCalled("OnPlayerAttack");
        }

        #endregion

#endif

#if SEVENDAYS

        #region Server Hooks

        private void OnRunCommand(ClientInfo client, string[] args)
        {
            HookCalled("OnRunCommand");
        }

        #endregion

        #region Player Hooks

        private void OnExperienceGained()
        {
            HookCalled("OnExperienceGained");
        }

        #endregion

        #region Entity Hooks

        private void OnAirdrop(UnityEngine.Vector3 location)
        {
            HookCalled("OnAirdrop");
        }

        private void OnEntitySpawned(Entity entity)
        {
            HookCalled("OnEntitySpawned");
        }

        private void OnEntityTakeDamage(EntityAlive entity, DamageSource source)
        {
            HookCalled("OnEntityTakeDamage");
        }

        private void OnEntityDeath(Entity entity, DamageResponse response)
        {
            HookCalled("OnEntityDeath");
        }

        #endregion

#endif

#if UNTURNED

        #region Server Hooks

        private void OnRunCommand(Steamworks.CSteamID steamId, string command, string arg)
        {
            HookCalled("OnRunCommand");
        }

        #endregion

#endif
    }
}
