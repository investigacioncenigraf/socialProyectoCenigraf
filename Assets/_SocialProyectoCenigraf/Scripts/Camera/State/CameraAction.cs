using UnityEngine;

namespace SocialProyectoCenigraf.CameraSystem.State
{
    public enum CameraActionType
    {
        SetPosition,
        SetTargetPosition,
        SetDelayEnabled,
        SetDelaySeconds,
        SetSmoothingEnabled,
        SetSmoothingTime,
        SetPixelSnapEnabled,
        SetZoom
    }

    public readonly struct CameraVector2Payload
    {
        public Vector2 Value { get; }

        public CameraVector2Payload(Vector2 value)
        {
            Value = value;
        }
    }

    public readonly struct CameraBoolPayload
    {
        public bool Value { get; }

        public CameraBoolPayload(bool value)
        {
            Value = value;
        }
    }

    public readonly struct CameraFloatPayload
    {
        public float Value { get; }

        public CameraFloatPayload(float value)
        {
            Value = value;
        }
    }

    public readonly struct CameraAction
    {
        public CameraActionType Type { get; }
        public CameraVector2Payload Vector2Payload { get; }
        public CameraBoolPayload BoolPayload { get; }
        public CameraFloatPayload FloatPayload { get; }

        private CameraAction(
            CameraActionType type,
            CameraVector2Payload vector2Payload = default,
            CameraBoolPayload boolPayload = default,
            CameraFloatPayload floatPayload = default)
        {
            Type = type;
            Vector2Payload = vector2Payload;
            BoolPayload = boolPayload;
            FloatPayload = floatPayload;
        }

        public static CameraAction SetPosition(Vector2 position) =>
            new CameraAction(
                CameraActionType.SetPosition,
                vector2Payload: new CameraVector2Payload(position));

        public static CameraAction SetTargetPosition(Vector2 position) =>
            new CameraAction(
                CameraActionType.SetTargetPosition,
                vector2Payload: new CameraVector2Payload(position));

        public static CameraAction SetDelayEnabled(bool enabled) =>
            new CameraAction(
                CameraActionType.SetDelayEnabled,
                boolPayload: new CameraBoolPayload(enabled));

        public static CameraAction SetDelaySeconds(float seconds) =>
            new CameraAction(
                CameraActionType.SetDelaySeconds,
                floatPayload: new CameraFloatPayload(seconds));

        public static CameraAction SetSmoothingEnabled(bool enabled) =>
            new CameraAction(
                CameraActionType.SetSmoothingEnabled,
                boolPayload: new CameraBoolPayload(enabled));

        public static CameraAction SetSmoothingTime(float seconds) =>
            new CameraAction(
                CameraActionType.SetSmoothingTime,
                floatPayload: new CameraFloatPayload(seconds));

        public static CameraAction SetPixelSnapEnabled(bool enabled) =>
            new CameraAction(
                CameraActionType.SetPixelSnapEnabled,
                boolPayload: new CameraBoolPayload(enabled));

        public static CameraAction SetZoom(float zoom) =>
            new CameraAction(
                CameraActionType.SetZoom,
                floatPayload: new CameraFloatPayload(zoom));
    }
}
