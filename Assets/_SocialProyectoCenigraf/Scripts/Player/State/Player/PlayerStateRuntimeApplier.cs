using SocialProyectoCenigraf.World.Rendering;
using SocialProyectoCenigraf.Session.State;
using UnityEngine;

namespace SocialProyectoCenigraf.Player.State
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStateStore))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(WorldYSort))]
    public sealed class PlayerStateRuntimeApplier : MonoBehaviour
    {
        private const float MinimumColliderDimension = 0.01f;

        private PlayerStateStore store;
        private Rigidbody2D body;
        private BoxCollider2D bodyCollider;
        private WorldYSort worldYSort;

        private void Awake()
        {
            store = GetComponent<PlayerStateStore>();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<BoxCollider2D>();
            worldYSort = GetComponent<WorldYSort>();

            store.Initialize(new PlayerStateData(
                body.position,
                worldYSort.SortingEnabled,
                bodyCollider.size,
                bodyCollider.offset,
                GameSessionStore.Instance.State.SelectedRoleId));

            ApplyState(store.State);
        }

        private void OnEnable()
        {
            store.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            store.StateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(
            PlayerStateData state,
            PlayerAction action)
        {
            ApplyState(state);
        }

        private void ApplyState(PlayerStateData state)
        {
            worldYSort.SetSortingEnabled(state.YSortEnabled);

            bodyCollider.size = new Vector2(
                Mathf.Max(MinimumColliderDimension, state.ColliderSize.x),
                Mathf.Max(MinimumColliderDimension, state.ColliderSize.y));

            bodyCollider.offset = state.ColliderOffset;
        }
    }
}
