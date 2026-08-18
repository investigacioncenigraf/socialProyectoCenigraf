using System;
using UnityEngine;

namespace SocialProyectoCenigraf.CameraSystem.State
{
    public static class CameraStateReducer
    {
        public static CameraStateData Reduce(
            CameraStateData currentState,
            CameraAction action)
        {
            switch (action.Type)
            {
                case CameraActionType.SetPosition:
                    return currentState.WithPosition(action.Vector2Payload.Value);

                case CameraActionType.SetTargetPosition:
                    return currentState.WithTargetPosition(action.Vector2Payload.Value);

                case CameraActionType.SetDelayEnabled:
                    return currentState.WithDelayEnabled(action.BoolPayload.Value);

                case CameraActionType.SetDelaySeconds:
                    return currentState.WithDelaySeconds(
                        Mathf.Max(0f, action.FloatPayload.Value));

                case CameraActionType.SetSmoothingEnabled:
                    return currentState.WithSmoothingEnabled(action.BoolPayload.Value);

                case CameraActionType.SetSmoothingTime:
                    return currentState.WithSmoothingTime(
                        Mathf.Max(0.001f, action.FloatPayload.Value));

                case CameraActionType.SetPixelSnapEnabled:
                    return currentState.WithPixelSnapEnabled(
                        action.BoolPayload.Value);

                case CameraActionType.SetZoom:
                    return currentState.WithZoom(
                        Mathf.Max(0.01f, action.FloatPayload.Value));

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(action),
                        action.Type,
                        "Unknown camera action.");
            }
        }
    }
}
