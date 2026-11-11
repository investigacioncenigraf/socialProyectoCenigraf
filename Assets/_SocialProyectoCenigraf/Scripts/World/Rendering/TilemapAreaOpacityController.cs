using SocialProyectoCenigraf.Player.Movement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SocialProyectoCenigraf.World.Rendering
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap))]
    [AddComponentMenu("Cenigraf/World/Tilemap Area Opacity")]
    public sealed class TilemapAreaOpacityController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Tilemap targetTilemap;
        [SerializeField] private Collider2D opacityArea;
        [SerializeField] private Transform playerTarget;

        [Header("Opacity")]
        [SerializeField, Range(0f, 1f)] private float fadedOpacity = 0.35f;
        [SerializeField, Min(0f)] private float transitionSeconds = 0.2f;

        [Header("Player detection")]
        [SerializeField] private bool findLocalPlayerAutomatically = true;

        private Collider2D playerCollider;
        private float originalOpacity = 1f;
        private float currentOpacity = 1f;
        private bool isFaded;
        private float nextPlayerSearchTime;

        public bool IsFaded => isFaded;
        public float FadedOpacity => fadedOpacity;

        private void Reset()
        {
            FindReferences();
        }

        private void Awake()
        {
            FindReferences();
            CaptureOriginalOpacity();
        }

        private void OnEnable()
        {
            FindReferences();
            CaptureOriginalOpacity();
            ResolvePlayerTarget();
            RefreshImmediately();
        }

        private void Update()
        {
            if (targetTilemap == null || opacityArea == null)
            {
                return;
            }

            if (playerTarget == null && findLocalPlayerAutomatically &&
                Time.unscaledTime >= nextPlayerSearchTime)
            {
                ResolvePlayerTarget();
                nextPlayerSearchTime = Time.unscaledTime + 0.5f;
            }

            if (playerTarget == null)
            {
                return;
            }

            isFaded = IsPlayerInsideArea();
            float targetOpacity = isFaded
                ? Mathf.Min(originalOpacity, fadedOpacity)
                : originalOpacity;

            if (transitionSeconds <= Mathf.Epsilon)
            {
                SetOpacity(targetOpacity);
                return;
            }

            float opacityDistance = Mathf.Abs(
                originalOpacity - Mathf.Min(originalOpacity, fadedOpacity));
            float speed = opacityDistance / transitionSeconds;
            SetOpacity(Mathf.MoveTowards(
                currentOpacity,
                targetOpacity,
                speed * Time.deltaTime));
        }

        private void OnDisable()
        {
            if (targetTilemap != null)
            {
                SetOpacity(originalOpacity);
            }
        }

        public void RefreshImmediately()
        {
            if (targetTilemap == null || opacityArea == null ||
                playerTarget == null)
            {
                return;
            }

            isFaded = IsPlayerInsideArea();
            SetOpacity(isFaded
                ? Mathf.Min(originalOpacity, fadedOpacity)
                : originalOpacity);
        }

        private bool IsPlayerInsideArea()
        {
            if (!opacityArea.enabled || !opacityArea.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (playerCollider != null && playerCollider.enabled &&
                playerCollider.gameObject.activeInHierarchy)
            {
                return opacityArea.bounds.Intersects(playerCollider.bounds);
            }

            return opacityArea.OverlapPoint(playerTarget.position);
        }

        private void CaptureOriginalOpacity()
        {
            if (targetTilemap == null)
            {
                return;
            }

            originalOpacity = targetTilemap.color.a;
            currentOpacity = originalOpacity;
        }

        private void SetOpacity(float value)
        {
            currentOpacity = Mathf.Clamp01(value);
            Color color = targetTilemap.color;
            color.a = currentOpacity;
            targetTilemap.color = color;
        }

        private void ResolvePlayerTarget()
        {
            if (playerTarget != null)
            {
                ResolvePlayerCollider();
                return;
            }

            if (!findLocalPlayerAutomatically)
            {
                return;
            }

            PlayerMovementController[] players =
                FindObjectsByType<PlayerMovementController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            foreach (PlayerMovementController player in players)
            {
                if (player != null && player.AcceptsLocalInput &&
                    player.isActiveAndEnabled)
                {
                    playerTarget = player.transform;
                    ResolvePlayerCollider();
                    return;
                }
            }
        }

        private void ResolvePlayerCollider()
        {
            playerCollider = playerTarget.GetComponent<Collider2D>();

            if (playerCollider == null)
            {
                playerCollider = playerTarget.GetComponentInChildren<Collider2D>();
            }
        }

        private void FindReferences()
        {
            if (targetTilemap == null)
            {
                targetTilemap = GetComponent<Tilemap>();
            }

            if (opacityArea == null)
            {
                Transform areaTransform = transform.Find("OpacityArea");
                if (areaTransform != null)
                {
                    opacityArea = areaTransform.GetComponent<Collider2D>();
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            FindReferences();
            fadedOpacity = Mathf.Clamp01(fadedOpacity);
            transitionSeconds = Mathf.Max(0f, transitionSeconds);
        }

        private void OnDrawGizmosSelected()
        {
            if (opacityArea == null)
            {
                FindReferences();
            }

            if (opacityArea == null)
            {
                return;
            }

            Bounds areaBounds = opacityArea.bounds;
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            Gizmos.DrawWireCube(areaBounds.center, areaBounds.size);
        }
#endif
    }
}
