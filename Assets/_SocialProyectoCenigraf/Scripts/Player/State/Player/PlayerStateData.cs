using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Player.State
{
    [Serializable]
    public struct PlayerStateData
    {
        public const int DefaultAnimationFrameDurationMilliseconds = 300;
        public const int DefaultFramesPerAnimation = 4;
        public const string DefaultSkinId = "Demo";
        public const string DefaultRoleId = "visitor";
        public const bool DefaultForceFrontAnimationOnHorizontalMovement = true;

        [SerializeField] private Vector2 position;
        [SerializeField] private string roleId;
        [SerializeField] private string skinId;
        [SerializeField] private PlayerFacingDirection facingDirection;
        [SerializeField] private int animationFrameDurationMilliseconds;
        [SerializeField] private int framesPerAnimation;
        [SerializeField] private bool forceFrontAnimationOnHorizontalMovement;
        [SerializeField] private bool ySortEnabled;
        [SerializeField] private Vector2 colliderSize;
        [SerializeField] private Vector2 colliderOffset;

        public Vector2 Position => position;
        public string RoleId => string.IsNullOrWhiteSpace(roleId)
            ? DefaultRoleId
            : roleId;
        public string SkinId => string.IsNullOrWhiteSpace(skinId)
            ? DefaultSkinId
            : skinId;
        public PlayerFacingDirection FacingDirection => facingDirection;
        public int AnimationFrameDurationMilliseconds => animationFrameDurationMilliseconds;
        public int FramesPerAnimation => framesPerAnimation;
        public bool ForceFrontAnimationOnHorizontalMovement =>
            forceFrontAnimationOnHorizontalMovement;
        public bool YSortEnabled => ySortEnabled;
        public Vector2 ColliderSize => colliderSize;
        public Vector2 ColliderOffset => colliderOffset;

        public PlayerStateData(
            Vector2 position,
            bool ySortEnabled,
            Vector2 colliderSize,
            Vector2 colliderOffset,
            string roleId = DefaultRoleId,
            string skinId = DefaultSkinId,
            PlayerFacingDirection facingDirection = PlayerFacingDirection.DownRight,
            int animationFrameDurationMilliseconds = DefaultAnimationFrameDurationMilliseconds,
            int framesPerAnimation = DefaultFramesPerAnimation,
            bool forceFrontAnimationOnHorizontalMovement =
                DefaultForceFrontAnimationOnHorizontalMovement)
        {
            this.position = position;
            this.roleId = string.IsNullOrWhiteSpace(roleId)
                ? DefaultRoleId
                : roleId.Trim();
            this.skinId = string.IsNullOrWhiteSpace(skinId)
                ? DefaultSkinId
                : skinId;
            this.facingDirection = facingDirection;
            this.animationFrameDurationMilliseconds = animationFrameDurationMilliseconds;
            this.framesPerAnimation = framesPerAnimation;
            this.forceFrontAnimationOnHorizontalMovement =
                forceFrontAnimationOnHorizontalMovement;
            this.ySortEnabled = ySortEnabled;
            this.colliderSize = colliderSize;
            this.colliderOffset = colliderOffset;
        }

        public PlayerStateData WithPosition(Vector2 value) =>
            new PlayerStateData(
                value,
                ySortEnabled,
                colliderSize,
                colliderOffset,
                roleId,
                skinId,
                facingDirection,
                animationFrameDurationMilliseconds,
                framesPerAnimation,
                forceFrontAnimationOnHorizontalMovement);

        public PlayerStateData WithRole(string value) =>
            new PlayerStateData(
                position,
                ySortEnabled,
                colliderSize,
                colliderOffset,
                value,
                skinId,
                facingDirection,
                animationFrameDurationMilliseconds,
                framesPerAnimation,
                forceFrontAnimationOnHorizontalMovement);

        public PlayerStateData WithSkin(string value) =>
            new PlayerStateData(
                position,
                ySortEnabled,
                colliderSize,
                colliderOffset,
                roleId,
                value,
                facingDirection,
                animationFrameDurationMilliseconds,
                framesPerAnimation,
                forceFrontAnimationOnHorizontalMovement);

        public PlayerStateData WithFacingDirection(PlayerFacingDirection value) =>
            new PlayerStateData(
                position,
                ySortEnabled,
                colliderSize,
                colliderOffset,
                roleId,
                skinId,
                value,
                animationFrameDurationMilliseconds,
                framesPerAnimation,
                forceFrontAnimationOnHorizontalMovement);

        public PlayerStateData WithAnimationSettings(
            int frameDurationMilliseconds,
            int frameCount) =>
            new PlayerStateData(
                position,
                ySortEnabled,
                colliderSize,
                colliderOffset,
                roleId,
                skinId,
                facingDirection,
                frameDurationMilliseconds,
                frameCount,
                forceFrontAnimationOnHorizontalMovement);

        public PlayerStateData WithForceFrontAnimationOnHorizontalMovement(
            bool value) =>
            new PlayerStateData(
                position,
                ySortEnabled,
                colliderSize,
                colliderOffset,
                roleId,
                skinId,
                facingDirection,
                animationFrameDurationMilliseconds,
                framesPerAnimation,
                value);

        public PlayerStateData WithYSortEnabled(bool value) =>
            new PlayerStateData(
                position,
                value,
                colliderSize,
                colliderOffset,
                roleId,
                skinId,
                facingDirection,
                animationFrameDurationMilliseconds,
                framesPerAnimation,
                forceFrontAnimationOnHorizontalMovement);

        public PlayerStateData WithColliderSize(Vector2 value) =>
            new PlayerStateData(
                position,
                ySortEnabled,
                value,
                colliderOffset,
                roleId,
                skinId,
                facingDirection,
                animationFrameDurationMilliseconds,
                framesPerAnimation,
                forceFrontAnimationOnHorizontalMovement);

        public PlayerStateData WithColliderOffset(Vector2 value) =>
            new PlayerStateData(
                position,
                ySortEnabled,
                colliderSize,
                value,
                roleId,
                skinId,
                facingDirection,
                animationFrameDurationMilliseconds,
                framesPerAnimation,
                forceFrontAnimationOnHorizontalMovement);
    }
}
