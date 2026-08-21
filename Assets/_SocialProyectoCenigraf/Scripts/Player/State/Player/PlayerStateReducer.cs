using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Player.State
{
    public static class PlayerStateReducer
    {
        public static PlayerStateData Reduce(
            PlayerStateData currentState,
            PlayerAction action)
        {
            switch (action.Type)
            {
                case PlayerActionType.SetPosition:
                case PlayerActionType.ReconcilePosition:
                    return currentState.WithPosition(action.PositionPayload.Value);

                case PlayerActionType.Translate:
                    return currentState.WithPosition(
                        currentState.Position + action.PositionPayload.Value);

                case PlayerActionType.SetRole:
                    return currentState.WithRole(action.RolePayload.RoleId);

                case PlayerActionType.SetYSortEnabled:
                    return currentState.WithYSortEnabled(action.BoolPayload.Value);

                case PlayerActionType.SetColliderSize:
                    Vector2 requestedSize = action.PositionPayload.Value;
                    Vector2 validSize = new Vector2(
                        Mathf.Max(0.01f, requestedSize.x),
                        Mathf.Max(0.01f, requestedSize.y));
                    return currentState.WithColliderSize(validSize);

                case PlayerActionType.SetColliderOffset:
                    return currentState.WithColliderOffset(action.PositionPayload.Value);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(action),
                        action.Type,
                        "Unknown player action.");
            }
        }
    }
}
