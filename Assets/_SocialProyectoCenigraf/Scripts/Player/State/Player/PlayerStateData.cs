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

        public Vector2 Position => position;
        public bool YSortEnabled => ySortEnabled;
        public Vector2 ColliderSize => colliderSize;
        public Vector2 ColliderOffset => colliderOffset;

        public PlayerStateData(
            Vector2 position,
            bool ySortEnabled,
            Vector2 colliderSize,
            Vector2 colliderOffset)
        {
            this.position = position;
            this.ySortEnabled = ySortEnabled;
            this.colliderSize = colliderSize;
            this.colliderOffset = colliderOffset;
        }

        public PlayerStateData WithPosition(Vector2 value) =>
            new PlayerStateData(value, ySortEnabled, colliderSize, colliderOffset);

        public PlayerStateData WithYSortEnabled(bool value) =>
            new PlayerStateData(position, value, colliderSize, colliderOffset);

        public PlayerStateData WithColliderSize(Vector2 value) =>
            new PlayerStateData(position, ySortEnabled, value, colliderOffset);

        public PlayerStateData WithColliderOffset(Vector2 value) =>
            new PlayerStateData(position, ySortEnabled, colliderSize, value);
    }
}
