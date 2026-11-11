using SocialProyectoCenigraf.Player.Movement;
using UnityEngine;

namespace SocialProyectoCenigraf.World.Rendering
{
    /// <summary>
    /// Colored overlay that hides a room/corridor and fades away when the local player
    /// enters the reveal zone.
    ///
    /// Two independent areas (both are in local units, so the Transform scale multiplies them):
    ///  - Cover area (coverSize / coverOffset): what is painted by the SpriteRenderer.
    ///  - Reveal zone (BoxCollider2D, trigger): what the player must touch. It is never painted
    ///    and this script never changes its size or offset. Make it bigger than the cover area
    ///    so the room is revealed as the player approaches.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    [AddComponentMenu("Cenigraf/World/Shadow Area")]
    public sealed class ShadowArea : MonoBehaviour
    {
        private const float MinSize = 0.01f;
        private const string GeneratedSpriteName = "ShadowArea_Sprite";

        [Header("Cover area (painted by the SpriteRenderer)")]
        [SerializeField] private Vector2 coverSize = Vector2.one;
        [SerializeField] private Vector2 coverOffset = Vector2.zero;
        [SerializeField] private Color shadowColor = new(0f, 0f, 0f, 1f);
        [SerializeField] private int sortingOrder = 32000;
        [Tooltip("Turn off when a WorldYSort drives the sorting order of this area, so the two " +
                 "do not fight over it every frame.")]
        [SerializeField] private bool controlSortingOrder = true;

        [Header("Reveal zone = the BoxCollider2D on this object (invisible)")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private bool findLocalPlayerAutomatically = true;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.3f;
        [SerializeField, Min(0f)] private float fadeInSeconds = 0.3f;

        [Header("Discovery")]
        [Tooltip("If enabled, the first time the player enters the reveal zone the area fades out " +
                 "and stays gone for the rest of the session; the renderer and this component are then disabled.")]
        [SerializeField] private bool revealPermanentlyOnFirstEntry = true;

        [Header("Reveal source")]
        [Tooltip("When disabled, the BoxCollider2D zone is ignored and the area is only revealed " +
                 "through RevealNow() (used by the maze level, where doors reveal the rooms).")]
        [SerializeField] private bool autoRevealByPlayerZone = true;

        private bool discovered;
        private bool forcedReveal;
        private bool hasBeenEntered;
        private BoxCollider2D areaCollider;
        private SpriteRenderer areaRenderer;
        private Collider2D playerCollider;
        private float nextPlayerSearchTime;
        private float currentAlpha;
        private bool playerInside;

        private Sprite generatedSprite;
        private Texture2D generatedTexture;

        private Vector2 syncedSize = new(float.NaN, float.NaN);
        private Vector2 syncedOffset = new(float.NaN, float.NaN);

        /// <summary>Size of the painted cover area (not the reveal zone), in local units.</summary>
        public Vector2 Dimensions
        {
            get => coverSize;
            set
            {
                coverSize = new Vector2(Mathf.Max(MinSize, value.x), Mathf.Max(MinSize, value.y));
                SyncVisual(false);
            }
        }

        private bool UsesGeneratedSprite => generatedSprite != null && areaRenderer != null
                                            && areaRenderer.sprite == generatedSprite;

        /// <summary>True once the local player has entered this area at least once (this session).</summary>
        public bool HasBeenEntered => hasBeenEntered;

        /// <summary>True once the area was revealed, by the player entering it or by RevealNow().</summary>
        public bool IsRevealed => forcedReveal || discovered;

        /// <summary>
        /// Reveals the area from code. With <paramref name="instant"/> it disappears at once,
        /// otherwise it fades out using Fade Out Seconds.
        /// </summary>
        public void RevealNow(bool instant = false)
        {
            forcedReveal = true;
            hasBeenEntered = true;

            if (!Application.isPlaying)
            {
                return;
            }

            CacheComponents();
            enabled = true;

            if (instant)
            {
                currentAlpha = 0f;
                ApplyColor(0f);

                if (areaRenderer != null)
                {
                    areaRenderer.enabled = false;
                }

                enabled = false;
            }
        }

        /// <summary>Sets the fog colour (alpha is kept as the fog opacity).</summary>
        public void SetShadowColor(Color color)
        {
            shadowColor = color;

            if (areaRenderer == null)
            {
                CacheComponents();
            }

            if (!Application.isPlaying)
            {
                ApplyColor(shadowColor.a);
            }
        }

        /// <summary>Covers the area again (used when the maze is regenerated).</summary>
        public void CoverAgain()
        {
            forcedReveal = false;
            discovered = false;
            hasBeenEntered = false;
            playerInside = false;
            currentAlpha = shadowColor.a;
            CacheComponents();

            if (areaRenderer != null)
            {
                areaRenderer.enabled = true;
            }

            enabled = true;
            ApplyColor(currentAlpha);
        }

        private void Reset()
        {
            CacheComponents();
            // First add only. Existing objects keep whatever the user set.
            coverSize = new Vector2(4f, 3f);
            areaCollider.size = new Vector2(8f, 7f); // bigger than the cover: reveals early
            areaCollider.isTrigger = true;
        }

        private void Awake()
        {
            currentAlpha = shadowColor.a;
        }

        private void OnEnable()
        {
            CacheComponents();
            if (!areaCollider.isTrigger) areaCollider.isTrigger = true;
            EnsureSprite();
            SyncVisual(true);
            ApplyColor(Application.isPlaying ? currentAlpha : shadowColor.a);
        }

        private void OnDisable()
        {
            ReleaseGeneratedSprite();
        }

        private void Update()
        {
            SyncVisual(false);

            if (!Application.isPlaying)
            {
                // Edit mode: always show the full cover so it can be positioned.
                ApplyColor(shadowColor.a);
                return;
            }

            bool insideNow = false;

            if (autoRevealByPlayerZone)
            {
                if (playerTarget == null && findLocalPlayerAutomatically && Time.unscaledTime >= nextPlayerSearchTime)
                {
                    ResolvePlayer();
                    nextPlayerSearchTime = Time.unscaledTime + 0.5f;
                }

                insideNow = IsPlayerInside();
            }

            playerInside = insideNow;
            if (playerInside) hasBeenEntered = true;
            if (revealPermanentlyOnFirstEntry && playerInside) discovered = true;

            bool revealed = playerInside || forcedReveal || (revealPermanentlyOnFirstEntry && discovered);
            float targetAlpha = revealed ? 0f : shadowColor.a;
            float duration = revealed ? fadeOutSeconds : fadeInSeconds;
            if (duration <= Mathf.Epsilon)
            {
                currentAlpha = targetAlpha;
            }
            else
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha,
                    shadowColor.a * Time.deltaTime / duration);
            }

            ApplyColor(currentAlpha);

            // Fully revealed for good: stop rendering and stop updating (OnDisable frees the sprite).
            if ((forcedReveal || (revealPermanentlyOnFirstEntry && discovered)) && currentAlpha <= Mathf.Epsilon)
            {
                areaRenderer.enabled = false;
                enabled = false;
            }
        }

        private void CacheComponents()
        {
            if (areaCollider == null) areaCollider = GetComponent<BoxCollider2D>();
            if (areaRenderer == null) areaRenderer = GetComponent<SpriteRenderer>();
        }

        // Not named DestroyObject: that name hides the obsolete member of Object.
        private static void DestroyGenerated(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        /// <summary>
        /// Uses a runtime white pixel when there is no sprite. A sprite named like ours that
        /// is not the one we created is a leftover saved inside the scene by older versions:
        /// it is replaced so its fixed size/pivot no longer paints a wrong area.
        /// The generated objects are HideAndDontSave, so they are never written into the scene.
        /// </summary>
        private void EnsureSprite()
        {
            if (areaRenderer == null) return;
            Sprite current = areaRenderer.sprite;
            bool staleGenerated = current != null && current != generatedSprite && current.name == GeneratedSpriteName;
            if (current != null && !staleGenerated) return; // user's own sprite (or ours): keep

            if (generatedTexture == null)
            {
                generatedTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "ShadowArea_WhitePixel",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                generatedTexture.SetPixel(0, 0, Color.white);
                generatedTexture.Apply();
            }

            BuildGeneratedSprite(ClampedSize(), coverOffset);
        }

        // Pivot is shifted so the painted rectangle is centered on coverOffset.
        private void BuildGeneratedSprite(Vector2 size, Vector2 offset)
        {
            if (generatedSprite != null) DestroyGenerated(generatedSprite);

            Vector2 pivot = new(0.5f - offset.x / size.x, 0.5f - offset.y / size.y);
            generatedSprite = Sprite.Create(generatedTexture, new Rect(0, 0, 1, 1), pivot, 1f);
            generatedSprite.name = GeneratedSpriteName;
            generatedSprite.hideFlags = HideFlags.HideAndDontSave;
            areaRenderer.sprite = generatedSprite;
        }

        private void ReleaseGeneratedSprite()
        {
            if (areaRenderer != null && generatedSprite != null && areaRenderer.sprite == generatedSprite)
                areaRenderer.sprite = null;
            DestroyGenerated(generatedSprite);
            DestroyGenerated(generatedTexture);
            generatedSprite = null;
            generatedTexture = null;
            syncedSize = new Vector2(float.NaN, float.NaN);
            syncedOffset = new Vector2(float.NaN, float.NaN);
        }

        private Vector2 ClampedSize() =>
            new(Mathf.Max(MinSize, coverSize.x), Mathf.Max(MinSize, coverSize.y));

        // Makes the renderer follow coverSize/coverOffset. The collider is never touched.
        private void SyncVisual(bool force)
        {
            CacheComponents();
            if (areaRenderer == null) return;

            Vector2 size = ClampedSize();
            if (!force && size == syncedSize && coverOffset == syncedOffset) return;

            if (UsesGeneratedSprite && generatedTexture != null)
                BuildGeneratedSprite(size, coverOffset);

            syncedSize = size;
            syncedOffset = coverOffset;

            if (areaRenderer.drawMode != SpriteDrawMode.Sliced) areaRenderer.drawMode = SpriteDrawMode.Sliced;
            if (areaRenderer.size != size) areaRenderer.size = size;
            if (controlSortingOrder && areaRenderer.sortingOrder != sortingOrder)
            {
                areaRenderer.sortingOrder = sortingOrder;
            }
        }

        private bool IsPlayerInside()
        {
            if (playerTarget == null || areaCollider == null || !areaCollider.enabled) return false;
            if (playerCollider != null && playerCollider.enabled && playerCollider.gameObject.activeInHierarchy)
                return areaCollider.bounds.Intersects(playerCollider.bounds);
            return areaCollider.OverlapPoint(playerTarget.position);
        }

        private void ResolvePlayer()
        {
            PlayerMovementController[] players = FindObjectsByType<PlayerMovementController>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (PlayerMovementController player in players)
            {
                if (player == null || !player.isActiveAndEnabled || !player.AcceptsLocalInput) continue;
                playerTarget = player.transform;
                playerCollider = player.GetComponent<Collider2D>() ?? player.GetComponentInChildren<Collider2D>();
                return;
            }
        }

        private void ApplyColor(float alpha)
        {
            if (areaRenderer == null) return;
            Color color = shadowColor;
            color.a = Mathf.Clamp01(alpha);
            if (areaRenderer.color != color) areaRenderer.color = color;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            coverSize = new Vector2(Mathf.Max(MinSize, coverSize.x), Mathf.Max(MinSize, coverSize.y));
            fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
            fadeInSeconds = Mathf.Max(0f, fadeInSeconds);
            // The renderer cannot be modified safely from OnValidate (SendMessage rules),
            // so the visual is refreshed on the next editor tick.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                SyncVisual(false);
                ApplyColor(Application.isPlaying ? currentAlpha : shadowColor.a);
            };
        }
#endif
    }
}
