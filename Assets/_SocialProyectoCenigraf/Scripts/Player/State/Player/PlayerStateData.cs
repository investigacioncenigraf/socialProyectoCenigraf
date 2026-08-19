using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Player.State
{
    [Serializable]
    public struct PlayerStateData
    {
        [SerializeField] private Vector2 position;
        [SerializeField] private bool ySortEnabled;
        [SerializeField] private Vector2 colliderSize;
        [SerializeField] private Vector2 colliderOffset;
        [SerializeField] private PlayerRole role;

        public Vector2 Position => position;
        public bool YSortEnabled => ySortEnabled;
        public Vector2 ColliderSize => colliderSize;
        public Vector2 ColliderOffset => colliderOffset;
        public PlayerRole Role => role;

        public PlayerStateData(
            Vector2 position,
            bool ySortEnabled,
            Vector2 colliderSize,
            Vector2 colliderOffset,
            PlayerRole role)
        {
            this.position = position;
            this.ySortEnabled = ySortEnabled;
            this.colliderSize = colliderSize;
            this.colliderOffset = colliderOffset;
            this.role = role;
        }

        public PlayerStateData WithPosition(Vector2 value) =>
            new PlayerStateData(
                value,
                ySortEnabled,
                colliderSize,
                colliderOffset,
                role);

        public PlayerStateData WithYSortEnabled(bool value) =>
            new PlayerStateData(
                position,
                value,
                colliderSize,
                colliderOffset,
                role);

        public PlayerStateData WithColliderSize(Vector2 value) =>
            new PlayerStateData(
                position,
                ySortEnabled,
                value,
                colliderOffset,
                role);

        public PlayerStateData WithColliderOffset(Vector2 value) =>
            new PlayerStateData(
                position,
                ySortEnabled,
                colliderSize,
                value,
                role);

        public PlayerStateData WithRole(PlayerRole value) =>
            new PlayerStateData(
                position,
                ySortEnabled,
                colliderSize,
                colliderOffset,
                value);
    }
}
