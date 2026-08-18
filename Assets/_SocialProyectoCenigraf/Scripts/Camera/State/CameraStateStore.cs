using System;
using UnityEngine;

namespace SocialProyectoCenigraf.CameraSystem.State
{
    [DisallowMultipleComponent]
    public sealed class CameraStateStore : MonoBehaviour
    {
        [Header("Current state (read only at runtime)")]
        [SerializeField] private CameraStateData state;

        public CameraStateData State => state;

        public event Action<CameraStateData, CameraAction> StateChanged;

        public void Initialize(CameraStateData initialState)
        {
            state = initialState;
        }

        public void Dispatch(CameraAction action)
        {
            CameraStateData nextState = CameraStateReducer.Reduce(state, action);

            if (StatesAreEqual(nextState, state))
            {
                return;
            }

            state = nextState;
            StateChanged?.Invoke(state, action);
        }

        public void SetDelayEnabled(bool enabled) =>
            Dispatch(CameraAction.SetDelayEnabled(enabled));

        public void SetDelaySeconds(float seconds) =>
            Dispatch(CameraAction.SetDelaySeconds(seconds));

        public void SetSmoothingEnabled(bool enabled) =>
            Dispatch(CameraAction.SetSmoothingEnabled(enabled));

        public void SetSmoothingTime(float seconds) =>
            Dispatch(CameraAction.SetSmoothingTime(seconds));

        public void SetPixelSnapEnabled(bool enabled) =>
            Dispatch(CameraAction.SetPixelSnapEnabled(enabled));

        public void SetZoom(float zoom) =>
            Dispatch(CameraAction.SetZoom(zoom));

        private static bool StatesAreEqual(
            CameraStateData first,
            CameraStateData second)
        {
            return first.Position == second.Position &&
                   first.TargetPosition == second.TargetPosition &&
                   first.DelayEnabled == second.DelayEnabled &&
                   Mathf.Approximately(first.DelaySeconds, second.DelaySeconds) &&
                   first.SmoothingEnabled == second.SmoothingEnabled &&
                   Mathf.Approximately(first.SmoothingTime, second.SmoothingTime) &&
                   first.PixelSnapEnabled == second.PixelSnapEnabled &&
                   Mathf.Approximately(first.Zoom, second.Zoom);
        }
    }
}
