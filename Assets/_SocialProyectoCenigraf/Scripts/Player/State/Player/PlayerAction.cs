using UnityEngine;

namespace SocialProyectoCenigraf.Player.State
{
    public enum PlayerActionType
    {
        SetPosition,
        Translate,
        ReconcilePosition,
        SetRole,
        SetSkin,
        SetFacingFromMovement,
        SetAnimationSettings,
        SetForceFrontAnimationOnHorizontalMovement,
        SetAppearanceColors,
        SetYSortEnabled,
        SetColliderSize,
        SetColliderOffset
    }

    public readonly struct PlayerRolePayload
    {
        public string RoleId { get; }

        public PlayerRolePayload(string roleId)
        {
            RoleId = roleId ?? string.Empty;
        }
    }

    public readonly struct PlayerSkinPayload
    {
        public string SkinId { get; }

        public PlayerSkinPayload(string skinId)
        {
            SkinId = skinId ?? string.Empty;
        }
    }

    public readonly struct PlayerMovementDirectionPayload
    {
        public Vector2 Value { get; }

        public PlayerMovementDirectionPayload(Vector2 value)
        {
            Value = value;
        }
    }

    public readonly struct PlayerAnimationSettingsPayload
    {
        public int FrameDurationMilliseconds { get; }
        public int FramesPerAnimation { get; }

        public PlayerAnimationSettingsPayload(
            int frameDurationMilliseconds,
            int framesPerAnimation)
        {
            FrameDurationMilliseconds = frameDurationMilliseconds;
            FramesPerAnimation = framesPerAnimation;
        }
    }

    public readonly struct PlayerBoolPayload
    {
        public bool Value { get; }

        public PlayerBoolPayload(bool value)
        {
            Value = value;
        }
    }

    public readonly struct PlayerPositionPayload
    {
        public Vector2 Value { get; }

        public PlayerPositionPayload(Vector2 value)
        {
            Value = value;
        }
    }

    public readonly struct PlayerAppearanceColorsPayload
    {
        public Color HeadColor { get; }
        public Color BodyColor { get; }
        public Color HandsColor { get; }

        public PlayerAppearanceColorsPayload(
            Color headColor,
            Color bodyColor,
            Color handsColor)
        {
            HeadColor = headColor;
            BodyColor = bodyColor;
            HandsColor = handsColor;
        }
    }

    public readonly struct PlayerAction
    {
        public PlayerActionType Type { get; }
        public PlayerPositionPayload PositionPayload { get; }
        public PlayerBoolPayload BoolPayload { get; }
        public PlayerRolePayload RolePayload { get; }
        public PlayerSkinPayload SkinPayload { get; }
        public PlayerMovementDirectionPayload MovementDirectionPayload { get; }
        public PlayerAnimationSettingsPayload AnimationSettingsPayload { get; }
        public PlayerAppearanceColorsPayload AppearanceColorsPayload { get; }

        private PlayerAction(
            PlayerActionType type,
            PlayerPositionPayload positionPayload = default,
            PlayerBoolPayload boolPayload = default,
            PlayerRolePayload rolePayload = default,
            PlayerSkinPayload skinPayload = default,
            PlayerMovementDirectionPayload movementDirectionPayload = default,
            PlayerAnimationSettingsPayload animationSettingsPayload = default,
            PlayerAppearanceColorsPayload appearanceColorsPayload = default)
        {
            Type = type;
            PositionPayload = positionPayload;
            BoolPayload = boolPayload;
            RolePayload = rolePayload;
            SkinPayload = skinPayload;
            MovementDirectionPayload = movementDirectionPayload;
            AnimationSettingsPayload = animationSettingsPayload;
            AppearanceColorsPayload = appearanceColorsPayload;
        }

        public static PlayerAction SetRole(string roleId)
        {
            return new PlayerAction(
                PlayerActionType.SetRole,
                rolePayload: new PlayerRolePayload(roleId));
        }

        public static PlayerAction SetSkin(string skinId)
        {
            return new PlayerAction(
                PlayerActionType.SetSkin,
                skinPayload: new PlayerSkinPayload(skinId));
        }

        public static PlayerAction SetFacingFromMovement(Vector2 movementDirection)
        {
            return new PlayerAction(
                PlayerActionType.SetFacingFromMovement,
                movementDirectionPayload: new PlayerMovementDirectionPayload(
                    movementDirection));
        }

        public static PlayerAction SetAnimationSettings(
            int frameDurationMilliseconds,
            int framesPerAnimation)
        {
            return new PlayerAction(
                PlayerActionType.SetAnimationSettings,
                animationSettingsPayload: new PlayerAnimationSettingsPayload(
                    frameDurationMilliseconds,
                    framesPerAnimation));
        }

        public static PlayerAction SetForceFrontAnimationOnHorizontalMovement(
            bool enabled)
        {
            return new PlayerAction(
                PlayerActionType.SetForceFrontAnimationOnHorizontalMovement,
                boolPayload: new PlayerBoolPayload(enabled));
        }

        public static PlayerAction SetAppearanceColors(
            Color headColor,
            Color bodyColor,
            Color handsColor)
        {
            return new PlayerAction(
                PlayerActionType.SetAppearanceColors,
                appearanceColorsPayload: new PlayerAppearanceColorsPayload(
                    headColor,
                    bodyColor,
                    handsColor));
        }

        public static PlayerAction SetPosition(Vector2 position)
        {
            return new PlayerAction(
                PlayerActionType.SetPosition,
                positionPayload: new PlayerPositionPayload(position));
        }

        public static PlayerAction Translate(Vector2 displacement)
        {
            return new PlayerAction(
                PlayerActionType.Translate,
                positionPayload: new PlayerPositionPayload(displacement));
        }

        public static PlayerAction ReconcilePosition(Vector2 physicalPosition)
        {
            return new PlayerAction(
                PlayerActionType.ReconcilePosition,
                positionPayload: new PlayerPositionPayload(physicalPosition));
        }

        public static PlayerAction SetYSortEnabled(bool enabled)
        {
            return new PlayerAction(
                PlayerActionType.SetYSortEnabled,
                boolPayload: new PlayerBoolPayload(enabled));
        }

        public static PlayerAction SetColliderSize(Vector2 size)
        {
            return new PlayerAction(
                PlayerActionType.SetColliderSize,
                positionPayload: new PlayerPositionPayload(size));
        }

        public static PlayerAction SetColliderOffset(Vector2 offset)
        {
            return new PlayerAction(
                PlayerActionType.SetColliderOffset,
                positionPayload: new PlayerPositionPayload(offset));
        }
    }
}
