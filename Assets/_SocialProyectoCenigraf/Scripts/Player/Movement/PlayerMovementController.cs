using SocialProyectoCenigraf.Player.State;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SocialProyectoCenigraf.Player.Movement
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(PlayerStateStore))]
    public sealed class PlayerMovementController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 3f;

        [Header("Collision query")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField, Min(0.001f)] private float skinWidth = 0.02f;

        private const float ReconcileTolerance = 0.0001f;
        private const float DirectionThreshold = 0.0001f;
        private const int MaximumCastHits = 16;

        private readonly RaycastHit2D[] castHits = new RaycastHit2D[MaximumCastHits];

        private Rigidbody2D body;
        private BoxCollider2D bodyCollider;
        private PlayerStateStore store;
        private ContactFilter2D collisionFilter;
        private InputAction moveAction;
        private Vector2 moveInput;
        private bool hasPendingPhysicalMove;

        public Vector2 MoveInput => moveInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<BoxCollider2D>();
            store = GetComponent<PlayerStateStore>();

            collisionFilter = new ContactFilter2D();
            collisionFilter.SetLayerMask(collisionMask);
            collisionFilter.useTriggers = false;

        }

        private void OnEnable()
        {
            moveAction = InputSystem.actions?.FindAction("Player/Move");

            if (moveAction == null)
            {
                Debug.LogError(
                    "The project-wide input action 'Player/Move' was not found.",
                    this);
            }
        }

        private void Update()
        {
            moveInput = moveAction == null
                ? Vector2.zero
                : Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        }

        private void FixedUpdate()
        {
            ReconcilePreviousPhysicalMove();

            if (moveInput.sqrMagnitude > Mathf.Epsilon)
            {
                Vector2 facingDirection = moveInput;

                if (store.State.ForceFrontAnimationOnHorizontalMovement &&
                    Mathf.Abs(moveInput.x) > DirectionThreshold &&
                    Mathf.Abs(moveInput.y) <= DirectionThreshold)
                {
                    // A negative Y component selects the Front animation while
                    // preserving the real horizontal movement direction.
                    facingDirection.y = -1f;
                }

                store.Dispatch(
                    PlayerAction.SetFacingFromMovement(facingDirection));
            }

            Vector2 requestedDisplacement = moveInput * (moveSpeed * Time.fixedDeltaTime);
            if (requestedDisplacement.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Vector2 currentPosition = store.State.Position;
            Vector2 allowedPosition = ResolveAllowedPosition(
                currentPosition,
                requestedDisplacement);

            store.Dispatch(PlayerAction.SetPosition(allowedPosition));
            body.MovePosition(store.State.Position);
            hasPendingPhysicalMove = true;
        }

        private Vector2 ResolveAllowedPosition(
            Vector2 currentPosition,
            Vector2 requestedDisplacement)
        {
            Vector2 resolvedPosition = currentPosition;

            // Resolve each axis independently so the player can slide along walls.
            resolvedPosition += ResolveAxis(
                resolvedPosition,
                new Vector2(requestedDisplacement.x, 0f));

            resolvedPosition += ResolveAxis(
                resolvedPosition,
                new Vector2(0f, requestedDisplacement.y));

            return resolvedPosition;
        }

        private Vector2 ResolveAxis(Vector2 originPosition, Vector2 displacement)
        {
            float requestedDistance = displacement.magnitude;
            if (requestedDistance <= Mathf.Epsilon)
            {
                return Vector2.zero;
            }

            Vector2 direction = displacement / requestedDistance;
            Vector2 scale = transform.lossyScale;
            Vector2 castSize = new Vector2(
                bodyCollider.size.x * Mathf.Abs(scale.x),
                bodyCollider.size.y * Mathf.Abs(scale.y));
            Vector2 castOrigin = originPosition + bodyCollider.offset;
            float castDistance = requestedDistance + skinWidth;

            int hitCount = Physics2D.BoxCast(
                castOrigin,
                castSize,
                0f,
                direction,
                collisionFilter,
                castHits,
                castDistance);

            float allowedDistance = requestedDistance;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = castHits[index];

                if (hit.collider == null ||
                    hit.collider == bodyCollider ||
                    hit.rigidbody == body)
                {
                    continue;
                }

                allowedDistance = Mathf.Min(
                    allowedDistance,
                    Mathf.Max(0f, hit.distance - skinWidth));
            }

            return direction * allowedDistance;
        }

        private void ReconcilePreviousPhysicalMove()
        {
            if (!hasPendingPhysicalMove)
            {
                return;
            }

            hasPendingPhysicalMove = false;

            if ((body.position - store.State.Position).sqrMagnitude <=
                ReconcileTolerance * ReconcileTolerance)
            {
                return;
            }

            store.Dispatch(PlayerAction.ReconcilePosition(body.position));
        }

        private void OnDisable()
        {
            moveAction = null;
            moveInput = Vector2.zero;
            hasPendingPhysicalMove = false;
        }

        private void OnValidate()
        {
            collisionFilter.SetLayerMask(collisionMask);
            collisionFilter.useTriggers = false;
        }
    }
}
