using System.Collections.Generic;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("HandyMan", "MrMan", "1.0.2")]
    [Description("Provides AOE repair functionality to players in their buildable areas")]

    class HandyMan : RustPlugin
    {
        #region Members

        const string permAllowed = "handyman.allowed";

        readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("HandyMan");
        Dictionary<ulong, bool> playerPrefs = new Dictionary<ulong, bool>(); // Stores player preference values - on or off.

        ConfigData configData; // Structure containing the configuration data once read
        readonly Dictionary<string, HashSet<uint>> categories = new Dictionary<string, HashSet<uint>>(); // Structure containing the configured categories of structures configured to be affected by HandyMan

        PluginTimers RepairMessageTimer; // Timer to control HandyMan chats
        bool _allowHandyManFixMessage = true; // Indicator allowing for handyman fix messages
        bool _allowAOERepair = true; // Indicator for allowing AOE repair

        #endregion

        /// <summary>
        /// Class defined to deal with configuration structure
        /// </summary>
        class ConfigData
        {
            // Controls the range at which the AOE repair will work
            public float RepairRange { get; set; }
            public bool DefaultHandyManOn { get; set; }
            // Contains a list of possible affected structures
            public Dictionary<string, HashSet<string>> Categories { get; set; }
            public float HandyManChatInterval { get; set; }
        }

        /// <summary>
        /// Responsible for loading default configuration
        /// Also creates the initial configuration file
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                DefaultHandyManOn = true,
                HandyManChatInterval = 30,
                RepairRange = 50,
                // Specifies the structure category dictionary
                Categories = new Dictionary<string, HashSet<string>>
                {
                    {
                        "foundation", new HashSet<string>
                        {
                            "assets/prefabs/building core/foundation.triangle/foundation.triangle.prefab",
                            "assets/prefabs/building core/foundation.steps/foundation.steps.prefab",
                            "assets/prefabs/building core/foundation/foundation.prefab"
                        }
                    },
                    {
                        "wall", new HashSet<string>
                        {
                            "assets/prefabs/building/wall.external.high.stone/wall.external.high.stone.prefab",
                            "assets/prefabs/building/wall.external.high.wood/wall.external.high.wood.prefab",
                            "assets/prefabs/building core/wall.frame/wall.frame.prefab",
                            "assets/prefabs/building core/wall.window/wall.window.prefab",
                            "assets/prefabs/building core/wall.doorway/wall.doorway.prefab",
                            "assets/prefabs/building core/wall/wall.prefab"
                        }
                    },
                    {
                        "floor", new HashSet<string>
                        {
                            "assets/prefabs/building core/floor.frame/floor.frame.prefab",
                            "assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
                            "assets/prefabs/building core/floor/floor.prefab"
                        }
                    },
                    {
                        "other", new HashSet<string>
                        {
                            "assets/prefabs/building/gates.external.high/gates.external.high.stone/gates.external.high.stone.prefab",
                            "assets/prefabs/building/gates.external.high/gates.external.high.wood/gates.external.high.wood.prefab",
                            "assets/prefabs/building core/roof/roof.prefab",
                            "assets/prefabs/building core/stairs.l/block.stair.lshape.prefab",
                            "assets/prefabs/building core/pillar/pillar.prefab",
                            "assets/prefabs/building core/stairs.u/block.stair.ushape.prefab"
                        }
                    }
                }
            };
            // Creates a config file - Note sync is turned on so changes in the file should be taken into account, overriding what is coded here
            Config.WriteObject(config, true);
        }

        /// <summary>
        /// Responsible for loading the configured list of structures that will be affected
        /// Takes the text description given in the configuration and converts this to an internal system ID for the prefab
        /// </summary>
        internal void LoadAffectedStructures()
        {
            configData = Config.ReadObject<ConfigData>();
            foreach (var category in configData.Categories)
            {
                var data = new HashSet<uint>();
                // Cycle through the categories configured
                foreach (var prefab in category.Value)
                {
                    // Get the ID of the item based on the description configured
                    var prefabId = StringPool.Get(prefab);
                    // Checks to see if the prefab identity could be found based on the configured description of the structure - if not, skip to the next one
                    if (prefabId <= 0) continue;
                    // Prefab was found, so add it to our data
                    data.Add(prefabId);
                }
                // Add the items we found from the list above to our categories construct - essentially we've just eliminated any structures that we could not found and converted the
                // text description to the prefabID
                categories.Add(category.Key, data);
            }

        }

        #region Oxide Hooks

        /// <summary>
        /// Called when plugin initalizes
        /// This section is used to "prep" the plugin and any related / config data
        /// </summary>
        void Init()
        {
            // Read the configuration data
            LoadAffectedStructures();

            LoadMessages();
            permission.RegisterPermission(permAllowed, this);
            playerPrefs = dataFile.ReadObject<Dictionary<ulong, bool>>();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Hired", "HandyMan has been hired"},
                {"Fired", "HandyMan has been fired"},
                {"Fix", "You fix this one, I'll get the rest"},
                {"NotAllowed", "You are not allowed to build here - I can't repair for you"},
                {"IFixed", "Fixed some damage over here..."},
                {"FixDone", "Guess I fixed them all..."},
                {"MissingFix", "I'm telling you... it disappeared... I can't find anything to fix"},
                {"Unavailable", "Sorry, I'm not talking new clients right now"}
            }, this);
        }

        void OnPlayerInit(BasePlayer player) => playerPrefs[player.userID] = false;
        void OnServerSave() => dataFile.WriteObject(playerPrefs);

        /// <summary>
        /// TODO: Investigate entity driven repair
        /// Currently only building structures are driving repair. I want to allow things like high external walls to also
        /// drive repair, but they don't seem to fire under OnStructureRepair. I suspect this would be a better trigger as it would
        /// allow me to check my entity configuration rather than fire on simple repair.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="info"></param>
        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            //PrintToChat(player, info.HitEntity.LookupPrefabName());
        }

        // Called when any structure is repaired
        void OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            // Checks to see if the player preference data list contains this player
            if (!playerPrefs.ContainsKey(player.userID))
            {
                // Create a default entry for this player based on the default HandyMan configuration state
                playerPrefs[player.userID] = configData.DefaultHandyManOn;
                dataFile.WriteObject(playerPrefs);
            }

            // Check if repair should fire - This is to prevent a recursive / infinate loop when all structures in range fire this method.
            // This also checks if the player has turned HandyMan on
            if (_allowAOERepair && playerPrefs[player.userID])
            {
                // Calls our custom method for this
                Repair(entity, player);
            }
        }
        #endregion

        #region Help Text

        /// <summary>
        /// Responsible for publishing help for handyman on request
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("SendHelpText")]
        void SendHelpText(BasePlayer player)
        {
            //PrintToChat(player, Lang("Help", player.UserIDString)));
        }

        #endregion

        #region Structure Methods

        /// <summary>
        /// Executes the actual repair logic
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="player"></param>
        void Repair(BaseCombatEntity entity, BasePlayer player)
        {
            // Set message timer to prevent user spam
            ConfigureMessageTimer();

            // Checks to see if the player can build
            if (player.CanBuild())
            {
                // Player can build - check if we can display our fix message
                if (_allowHandyManFixMessage)
                {
                    // Display our fix message
                    PrintToChat(player, Lang("Fix", player.UserIDString));
                    _allowHandyManFixMessage = false;
                }

                // Envoke the AOE repair set
                RepairAOE(entity, player);
            }
            else
            {
                PrintToChat(player, Lang("NotAllowed", player.UserIDString));
            }
        }

        /// <summary>
        /// Contains the actual AOE repair logic
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="player"></param>
        void RepairAOE(BaseCombatEntity entity, BasePlayer player)
        {
            // This needs to be set to false in order to prevent the subsequent repairs from triggering the AOE repair.
            // If you don't do this - you create an infinate repair loop.
            _allowAOERepair = false;

            // Gets the position of the block we just hit
            var position = new OBB(entity.transform, entity.bounds).ToBounds().center;
            //sets up the collection for the blocks that will be affected
            var blocks = Pool.GetList<BaseCombatEntity>();

            // Gets a list of entities within a specified range of the current target
            Vis.Entities(position, configData.RepairRange, blocks, 270532864);

            //check if we have blocks - we should always have at least 1
            if (blocks.Count > 0)
            {
                var hasRepaired = false;

                // Cycle through our block list - figure out which ones need repairing
                foreach (var item in blocks)
                {
                    // Check to see if the block has been damaged before repairing.
                    if (item.Health() < item.MaxHealth())
                    {
                        // Repair
                        item.DoRepair(player);
                        hasRepaired = true;
                    }
                }
                Pool.FreeList(ref blocks);

                // Checks to see if any blocks were repaired
                PrintToChat(player, hasRepaired ? Lang("IFixed", player.UserIDString) : Lang("FixDone", player.UserIDString));
            }
            else
            {
                PrintToChat(player, Lang("MissingFix", player.UserIDString));
            }
            _allowAOERepair = true;
        }

        /// <summary>
        /// Responsible for preventing spam to the user by setting a timer to prevent messages from Handyman for a set duration.
        /// </summary>
        void ConfigureMessageTimer()
        {
            // Checks if our timer exists
            if (RepairMessageTimer != null) return;

            // Create it
            RepairMessageTimer = new PluginTimers(this);
            //set it to fire every xx seconds based on configuration
            RepairMessageTimer.Every(configData.HandyManChatInterval, RepairMessageTimer_Elapsed);
        }

        /// <summary>
        /// Timer for our repair message elapsed - set allow to true
        /// </summary>
        void RepairMessageTimer_Elapsed()
        {
            // Set the allow message to true so the next message will show
            _allowHandyManFixMessage = true;
        }

        #endregion

        #region Command

        [ChatCommand("handyman")]
        void ChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowed(player.UserIDString, permAllowed))
            {
                PrintToChat(player, Lang("Unavailable", player.UserIDString));
                return;
            }

            // Checks to see if the player preference data list contains this player
            if (!playerPrefs.ContainsKey(player.userID))
            {
                // Create a default entry for this player
                playerPrefs[player.userID] = true;
            }

            playerPrefs[player.userID] = !playerPrefs[player.userID];
            dataFile.WriteObject(playerPrefs);

            PrintToChat(player, playerPrefs[player.userID] ? Lang("Hired", player.UserIDString) : Lang("Fired", player.UserIDString));
        }

        #endregion

        #region Helpers

        bool IsAllowed(string id, string perm) => permission.UserHasPermission(id, perm);

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
