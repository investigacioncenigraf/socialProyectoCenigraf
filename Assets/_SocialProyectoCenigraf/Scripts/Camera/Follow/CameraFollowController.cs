using System.Collections.Generic;
using SocialProyectoCenigraf.CameraSystem.State;
using SocialProyectoCenigraf.Player.State;
using UnityEngine;

namespace SocialProyectoCenigraf.CameraSystem.Follow
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CameraStateStore))]
    public sealed class CameraFollowController : MonoBehaviour
    {
        [Header("Target state")]
        [SerializeField] private PlayerStateStore playerStateStore;

        [Header("Initial delay settings")]
        [SerializeField] private bool delayEnabled = true;
        [SerializeField, Min(0f)] private float delaySeconds = 0.3f;

        [Header("Initial smoothing settings")]
        [SerializeField] private bool smoothingEnabled = true;
        [SerializeField, Min(0.001f)] private float smoothingTime = 0.15f;

        [Header("Rendering stability")]
        [SerializeField] private bool pixelSnapEnabled = true;

        [Header("Initial zoom")]
        [Tooltip("Smaller values bring an orthographic camera closer; larger values move it farther away.")]
        [SerializeField, Min(0.01f)] private float zoom = 5f;

        private readonly List<PositionSample> positionHistory =
            new List<PositionSample>();

        private CameraStateStore cameraStateStore;
        private UnityEngine.Camera attachedCamera;
        private CameraBounds cameraBounds;
        private Vector2 smoothingVelocity;
        private Vector2 continuousPosition;
        private float cameraZ;

        private readonly struct PositionSample
        {
            public float Time { get; }
            public Vector2 Position { get; }

            public PositionSample(float time, Vector2 position)
            {
                Time = time;
                Position = position;
            }
        }

        private void Awake()
        {
            cameraStateStore = GetComponent<CameraStateStore>();
            attachedCamera = GetComponent<UnityEngine.Camera>();
            cameraBounds = GetComponent<CameraBounds>();
            cameraZ = transform.position.z;
            continuousPosition = transform.position;

            if (playerStateStore == null)
            {
                playerStateStore = FindFirstObjectByType<PlayerStateStore>();
            }

            if (playerStateStore == null)
            {
                Debug.LogError(
                    $"{nameof(CameraFollowController)} could not find a " +
                    $"{nameof(PlayerStateStore)}.",
                    this);
                enabled = false;
                return;
            }

            if (attachedCamera == null || !attachedCamera.orthographic)
            {
                Debug.LogError(
                    $"{nameof(CameraFollowController)} requires an orthographic Camera component.",
                    this);
                enabled = false;
                return;
            }

            Vector2 cameraPosition = transform.position;
            Vector2 targetPosition = playerStateStore.State.Position;

            cameraStateStore.Initialize(new CameraStateData(
                cameraPosition,
                targetPosition,
                delayEnabled,
                Mathf.Max(0f, delaySeconds),
                smoothingEnabled,
                Mathf.Max(0.001f, smoothingTime),
                pixelSnapEnabled,
                Mathf.Max(0.01f, zoom)));

            attachedCamera.orthographicSize = cameraStateStore.State.Zoom;

            AddPositionSample(targetPosition);
        }

        private void OnEnable()
        {
            if (playerStateStore != null)
            {
                playerStateStore.StateChanged += HandlePlayerStateChanged;
            }
        }

        private void OnDisable()
        {
            if (playerStateStore != null)
            {
                playerStateStore.StateChanged -= HandlePlayerStateChanged;
            }
        }

        private void LateUpdate()
        {
            CameraStateData state = cameraStateStore.State;
            attachedCamera.orthographicSize = state.Zoom;
            Vector2 desiredPosition = state.DelayEnabled
                ? GetDelayedPosition(state.DelaySeconds)
                : state.TargetPosition;

            Vector2 nextPosition;

            if (state.SmoothingEnabled)
            {
                nextPosition = Vector2.SmoothDamp(
                    continuousPosition,
                    desiredPosition,
                    ref smoothingVelocity,
                    state.SmoothingTime,
                    Mathf.Infinity,
                    Time.deltaTime);
            }
            else
            {
                smoothingVelocity = Vector2.zero;
                nextPosition = desiredPosition;
            }

            if (cameraBounds != null && cameraBounds.isActiveAndEnabled)
            {
                nextPosition = cameraBounds.ConstrainPosition(
                    nextPosition, state.Zoom, attachedCamera.aspect);
            }

            continuousPosition = nextPosition;

            Vector2 renderedPosition = state.PixelSnapEnabled
                ? SnapToScreenPixel(nextPosition)
                : nextPosition;

            if (cameraBounds != null && cameraBounds.isActiveAndEnabled)
            {
                renderedPosition = cameraBounds.ConstrainPosition(
                    renderedPosition, state.Zoom, attachedCamera.aspect);
            }

            // The state changes before its physical representation (Transform).
            cameraStateStore.Dispatch(CameraAction.SetPosition(renderedPosition));

            Vector2 statePosition = cameraStateStore.State.Position;
            transform.position = new Vector3(
                statePosition.x,
                statePosition.y,
                cameraZ);

            TrimHistory(state.DelaySeconds);
        }

        private Vector2 SnapToScreenPixel(Vector2 position)
        {
            if (attachedCamera == null ||
                !attachedCamera.orthographic ||
                attachedCamera.pixelHeight <= 0)
            {
                return position;
            }

            float unitsPerPixel =
                attachedCamera.orthographicSize * 2f /
                attachedCamera.pixelHeight;

            if (unitsPerPixel <= Mathf.Epsilon)
            {
                return position;
            }

            return new Vector2(
                Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel,
                Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel);
        }

        private void HandlePlayerStateChanged(
            PlayerStateData playerState,
            PlayerAction action)
        {
            if (playerState.Position == cameraStateStore.State.TargetPosition)
            {
                return;
            }

            cameraStateStore.Dispatch(
                CameraAction.SetTargetPosition(playerState.Position));

            AddPositionSample(playerState.Position);
        }

        private void AddPositionSample(Vector2 position)
        {
            float sampleTime = Time.time;

            if (positionHistory.Count > 0 &&
                Mathf.Approximately(
                    positionHistory[positionHistory.Count - 1].Time,
                    sampleTime))
            {
                positionHistory[positionHistory.Count - 1] =
                    new PositionSample(sampleTime, position);
                return;
            }

            positionHistory.Add(new PositionSample(sampleTime, position));
        }

        private Vector2 GetDelayedPosition(float seconds)
        {
            if (positionHistory.Count == 0 || seconds <= 0f)
            {
                return cameraStateStore.State.TargetPosition;
            }

            float targetTime = Time.time - seconds;

            if (targetTime <= positionHistory[0].Time)
            {
                return positionHistory[0].Position;
            }

            for (int index = 1; index < positionHistory.Count; index++)
            {
                PositionSample next = positionHistory[index];
                if (next.Time < targetTime)
                {
                    continue;
                }

                PositionSample previous = positionHistory[index - 1];
                float duration = next.Time - previous.Time;
                float interpolation = duration <= Mathf.Epsilon
                    ? 1f
                    : Mathf.Clamp01((targetTime - previous.Time) / duration);

                return Vector2.Lerp(
                    previous.Position,
                    next.Position,
                    interpolation);
            }

            return positionHistory[positionHistory.Count - 1].Position;
        }

        private void TrimHistory(float activeDelaySeconds)
        {
            float retention = Mathf.Max(1f, activeDelaySeconds + 0.5f);
            float oldestUsefulTime = Time.time - retention;

            while (positionHistory.Count > 2 &&
                   positionHistory[1].Time < oldestUsefulTime)
            {
                positionHistory.RemoveAt(0);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            delaySeconds = Mathf.Max(0f, delaySeconds);
            smoothingTime = Mathf.Max(0.001f, smoothingTime);
        }
#endif
    }
}
