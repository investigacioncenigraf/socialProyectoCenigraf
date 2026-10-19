using System;
using SocialProyectoCenigraf.Player.State;
using UnityEngine;

namespace SocialProyectoCenigraf.Player.Replication
{
    /// <summary>
    /// Network-technology-neutral boundary around the Redux player store.
    /// It does not connect, send packets or choose a transport.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStateStore))]
    [RequireComponent(typeof(PlayerReplicationContext))]
    public sealed class PlayerReplicationBridge : MonoBehaviour
    {
        private PlayerStateStore store;
        private PlayerReplicationContext context;

        public event Action<PlayerReplicatedStateData> LocalSnapshotChanged;

        private void Awake()
        {
            store = GetComponent<PlayerStateStore>();
            context = GetComponent<PlayerReplicationContext>();
        }

        private void OnEnable()
        {
            store.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (store != null)
            {
                store.StateChanged -= HandleStateChanged;
            }
        }

        public PlayerReplicatedStateData CaptureSnapshot() =>
            PlayerReplicatedStateData.FromState(store.State);

        public bool TryApplyRemoteSnapshot(PlayerReplicatedStateData snapshot)
        {
            if (!context.IsRemoteReplica)
            {
                Debug.LogWarning(
                    "A remote snapshot can only be applied to a RemoteReplica.",
                    this);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(context.PlayerId) &&
                !string.Equals(
                    context.PlayerId,
                    snapshot.PlayerId,
                    StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    $"Snapshot '{snapshot.PlayerId}' does not belong to " +
                    $"player '{context.PlayerId}'.",
                    this);
                return false;
            }

            store.Dispatch(PlayerAction.ApplyReplicatedState(snapshot));
            return true;
        }

        private void HandleStateChanged(
            PlayerStateData state,
            PlayerAction action)
        {
            if (!context.AcceptsLocalInput || !IsReplicatedAction(action.Type))
            {
                return;
            }

            LocalSnapshotChanged?.Invoke(
                PlayerReplicatedStateData.FromState(state));
        }

        private static bool IsReplicatedAction(PlayerActionType type)
        {
            return type == PlayerActionType.SetPosition ||
                   type == PlayerActionType.Translate ||
                   type == PlayerActionType.ReconcilePosition ||
                   type == PlayerActionType.SetRole ||
                   type == PlayerActionType.SetSkin ||
                   type == PlayerActionType.SetFacingFromMovement ||
                   type == PlayerActionType.SetAppearanceColors ||
                   type == PlayerActionType.SetPlayerId ||
                   type == PlayerActionType.SetIsMoving;
        }
    }
}
