using System;
using UnityEngine;

namespace SocialProyectoCenigraf.CameraSystem.State
{
    [Serializable]
    public struct CameraStateData
    {
        [SerializeField] private Vector2 position;
        [SerializeField] private Vector2 targetPosition;
        [SerializeField] private bool delayEnabled;
        [SerializeField] private float delaySeconds;
        [SerializeField] private bool smoothingEnabled;
        [SerializeField] private float smoothingTime;
        [SerializeField] private bool pixelSnapEnabled;
        [SerializeField] private float zoom;

        public Vector2 Position => position;
        public Vector2 TargetPosition => targetPosition;
        public bool DelayEnabled => delayEnabled;
        public float DelaySeconds => delaySeconds;
        public bool SmoothingEnabled => smoothingEnabled;
        public float SmoothingTime => smoothingTime;
        public bool PixelSnapEnabled => pixelSnapEnabled;
        public float Zoom => zoom;

        public CameraStateData(
            Vector2 position,
            Vector2 targetPosition,
            bool delayEnabled,
            float delaySeconds,
            bool smoothingEnabled,
            float smoothingTime,
            bool pixelSnapEnabled,
            float zoom)
        {
            this.position = position;
            this.targetPosition = targetPosition;
            this.delayEnabled = delayEnabled;
            this.delaySeconds = delaySeconds;
            this.smoothingEnabled = smoothingEnabled;
            this.smoothingTime = smoothingTime;
            this.pixelSnapEnabled = pixelSnapEnabled;
            this.zoom = zoom;
        }

        public CameraStateData WithPosition(Vector2 value) =>
            new CameraStateData(
                value,
                targetPosition,
                delayEnabled,
                delaySeconds,
                smoothingEnabled,
                smoothingTime,
                pixelSnapEnabled,
                zoom);

        public CameraStateData WithTargetPosition(Vector2 value) =>
            new CameraStateData(
                position,
                value,
                delayEnabled,
                delaySeconds,
                smoothingEnabled,
                smoothingTime,
                pixelSnapEnabled,
                zoom);

        public CameraStateData WithDelayEnabled(bool value) =>
            new CameraStateData(
                position,
                targetPosition,
                value,
                delaySeconds,
                smoothingEnabled,
                smoothingTime,
                pixelSnapEnabled,
                zoom);

        public CameraStateData WithDelaySeconds(float value) =>
            new CameraStateData(
                position,
                targetPosition,
                delayEnabled,
                value,
                smoothingEnabled,
                smoothingTime,
                pixelSnapEnabled,
                zoom);

        public CameraStateData WithSmoothingEnabled(bool value) =>
            new CameraStateData(
                position,
                targetPosition,
                delayEnabled,
                delaySeconds,
                value,
                smoothingTime,
                pixelSnapEnabled,
                zoom);

        public CameraStateData WithSmoothingTime(float value) =>
            new CameraStateData(
                position,
                targetPosition,
                delayEnabled,
                delaySeconds,
                smoothingEnabled,
                value,
                pixelSnapEnabled,
                zoom);

        public CameraStateData WithPixelSnapEnabled(bool value) =>
            new CameraStateData(
                position,
                targetPosition,
                delayEnabled,
                delaySeconds,
                smoothingEnabled,
                smoothingTime,
                value,
                zoom);

        public CameraStateData WithZoom(float value) =>
            new CameraStateData(
                position,
                targetPosition,
                delayEnabled,
                delaySeconds,
                smoothingEnabled,
                smoothingTime,
                pixelSnapEnabled,
                value);
    }
}
