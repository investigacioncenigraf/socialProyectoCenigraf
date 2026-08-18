using UnityEngine;

namespace SocialProyectoCenigraf.Player.State
{
    public enum PlayerActionType
    {
        SetPosition,
        Translate,
        ReconcilePosition,
        SetYSortEnabled,
        SetColliderSize,
        SetColliderOffset
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

    public readonly struct PlayerAction
    {
        public PlayerActionType Type { get; }
        public PlayerPositionPayload PositionPayload { get; }
        public PlayerBoolPayload BoolPayload { get; }

        private PlayerAction(
            PlayerActionType type,
            PlayerPositionPayload positionPayload = default,
            PlayerBoolPayload boolPayload = default)
        {
            Type = type;
            PositionPayload = positionPayload;
            BoolPayload = boolPayload;
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
