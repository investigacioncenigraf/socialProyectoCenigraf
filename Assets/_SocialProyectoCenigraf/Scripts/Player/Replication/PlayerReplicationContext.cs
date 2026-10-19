using SocialProyectoCenigraf.Player.State;
using UnityEngine;

namespace SocialProyectoCenigraf.Player.Replication
{
    public enum PlayerControlAuthority
    {
        OfflineLocal,
        LocalOwner,
        RemoteReplica
    }

    /// <summary>
    /// Describes who is allowed to provide input for this player. A future
    /// networking package configures this component when the player is spawned.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStateStore))]
    public sealed class PlayerReplicationContext : MonoBehaviour
    {
        [SerializeField] private string playerId = "local-player";
        [SerializeField] private PlayerControlAuthority authority =
            PlayerControlAuthority.OfflineLocal;

        private PlayerStateStore store;

        public string PlayerId => playerId ?? string.Empty;
        public PlayerControlAuthority Authority => authority;
        public bool AcceptsLocalInput =>
            authority != PlayerControlAuthority.RemoteReplica;
        public bool IsRemoteReplica =>
            authority == PlayerControlAuthority.RemoteReplica;

        private void Awake()
        {
            store = GetComponent<PlayerStateStore>();
            ApplyIdentityToState();
        }

        public void Configure(string id, PlayerControlAuthority value)
        {
            playerId = id?.Trim() ?? string.Empty;
            authority = value;

            if (store == null)
            {
                store = GetComponent<PlayerStateStore>();
            }

            ApplyIdentityToState();
        }

        private void ApplyIdentityToState()
        {
            if (store != null && !string.IsNullOrWhiteSpace(playerId))
            {
                store.Dispatch(PlayerAction.SetPlayerId(playerId));
            }
        }
    }
}
