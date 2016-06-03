using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Possession", "Wulf/lukespragg", 0.1, ResourceId = 0)]
    [Description("")]

    class Possession : RustPlugin
    {
        void Loaded()
        {
            permission.RegisterPermission("possession.allowed", this);
        }

        #region Chat Commands

        [ChatCommand("spectate")]
        void HideChat(BasePlayer player, string command, string[] args)
        {
            // Check if player is already hidden
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.Spectating))
            {
                SendReply(player, "You're already spectating!");
                return;
            }

            var ray = new Ray(player.eyes.position, player.eyes.HeadForward());
            var target = FindObject(ray, 3) as BasePlayer; // TODO: Make distance (3) configurable
            if (!target) return;

            // Change to normal view
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
        }

        #endregion

        #region Death Handling

        void OnEntityDeath(BaseEntity entity)
        {
            // TODO: Check if player was being spectated, and stop spectating
        }

        #endregion

        #region Spectate Blocking

        object OnRunCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.connection != null && arg.cmd.namefull == "global.spectate") return true;
            return null;
        }

        object OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player.IsSpectating() && input.WasJustPressed(BUTTON.JUMP) || input.WasJustPressed(BUTTON.DUCK)) return true;
            //if (player.IsSpectating() player.Push(new Vector3());)
            return null;
        }

        #endregion

        #region Helper Methods

        static BaseEntity FindObject(Ray ray, float distance)
        {
            RaycastHit hit;
            return !Physics.Raycast(ray, out hit, distance) ? null : hit.GetEntity();
        }

        bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        #endregion
    }
}
