/*
TODO:
- Figure out how to slower player's descent
- Figure out how to change player's camera angle?
- Figure out how to change player's animation to... OnLadder?
*/

using System;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Parachute", "Wulf/lukespragg", "0.1")]
    [Description("Deploys a paracute and slows a player's descent.")]

    class Parachute : CovalencePlugin
    {
        #region Initialization

        void Loaded()
        {
            #if !RUST
            throw new NotSupportedException("This plugin does not support this game");
            #endif

            permission.RegisterPermission("parachute.allowed", this);
        }

        #endregion

        #region Parachute Deployment

        void DeployChute(BasePlayer player)
        {
            if (!HasPermission(player.UserIDString, "parachute.allowed")) return;

            var playerPos = player.transform.position;
            var playerRot = player.transform.rotation;

            // Create parachute
            var chute = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab", playerPos, playerRot);
            chute.gameObject.Identity();
            chute.Spawn();
            chute.SetParent(player, "parachute_attach");

            // Make player float
            var oldBody = player.GetComponent<Rigidbody>();
            oldBody.AddForce(0, 1, 1);
            //oldBody.AddForce(0, -5, 0);
            oldBody.isKinematic = true;

            var currentVelocity = oldBody.velocity;
            var oppositeForce = -currentVelocity;
            oldBody.AddRelativeForce(oppositeForce.x, oppositeForce.y, oppositeForce.z);

            //oldBody.constraints = RigidbodyConstraints.FreezePosition;
            //UnityEngine.Object.Destroy(oldBody);

            /*if (oldBody == null)
            {
                Puts("Old body destroyed!");
                var newBody = player.transform.gameObject.AddComponent<Rigidbody>();
                newBody.AddForce(Vector3.up * 100f);
                newBody.useGravity = true;
                newBody.isKinematic = true;
                newBody.mass = 0.1f;
                newBody.interpolation = RigidbodyInterpolation.None;
                newBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }*/

            // Set player view
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
            player.SendConsoleCommand("graphics.fov 100");
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustPressed(BUTTON.JUMP) && !player.IsOnGround())
            {
                var playerPos = player.transform.position;
                var groundPos = GetGroundPosition(playerPos);
                var distance = Vector3.Distance(playerPos, groundPos);

                if (distance > 30f) DeployChute(player);
            }
        }

        #endregion

        #region Parachute Removal

        void KillChute(BasePlayer player)
        {
            // Remove parachute
            foreach (var child in player.children.ToArray().Where(child => child.name.EndsWith("parachute.prefab")))
            {
                player.RemoveChild(child);
                child.Kill();
            }

            // Restore player view
            player.SendConsoleCommand("graphics.fov 75");
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
        }

        void OnPlayerLanded(BasePlayer player) => KillChute(player);

        void OnEntityDeath(BaseEntity entity)
        {
            var player = entity as BasePlayer;
            if (player) KillChute(player);
        }

        #endregion

        #region Helper Methods

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        bool HasPermission(string steamId, string perm) => permission.UserHasPermission(steamId, perm);

        static Vector3 GetGroundPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;
            var groundLayer = LayerMask.GetMask("Terrain", "World", "Construction");
            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, groundLayer)) sourcePos.y = hitInfo.point.y;
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }

        #endregion
    }
}
