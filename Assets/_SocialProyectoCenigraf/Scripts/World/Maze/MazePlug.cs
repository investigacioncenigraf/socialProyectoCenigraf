using SocialProyectoCenigraf.World.Rendering;
using UnityEngine;

namespace SocialProyectoCenigraf.World.Maze
{
    /// <summary>
    /// A 2x2 plug at one end of a connector, right next to a room.
    /// Blocking = solid collider + visible. Passage = no collider (the player crosses it and the
    /// generator reveals the room it touches). Inactive = nothing.
    /// Colour and sprite are set for every plug from the MazeLevelGenerator.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cenigraf/Maze/Maze Plug")]
    public sealed class MazePlug : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D blockingCollider;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private WorldYSort ySort;
        [SerializeField] private Vector2 size = new Vector2(2f, 2f);

        [Tooltip("Scales the Visual so the sprite covers the whole plug. Turn it off for art that " +
                 "must keep its own proportions.")]
        [SerializeField] private bool stretchVisualToSize = true;

        [Header("Area en la vista Scene (solo al editar)")]
        [SerializeField] private Color editorAreaColor = new Color(1f, 0f, 0f, 0.35f);

        private static Sprite whiteSprite;

        /// <summary>
        /// Translucent red box so the plugs can be told apart while the level is being edited.
        /// Gizmos never reach a build, and this one also hides itself in play mode.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (Application.isPlaying || editorAreaColor.a <= 0f)
            {
                return;
            }

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            // Filled only while the plug has no art of its own, so a sprite is never hidden by it.
            if (visual == null || visual.sprite == null)
            {
                Gizmos.color = editorAreaColor;
                Gizmos.DrawCube(Vector3.zero, size);
            }

            Color outlineColor = editorAreaColor;
            outlineColor.a = Mathf.Max(0.9f, editorAreaColor.a);
            Gizmos.color = outlineColor;
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = previousMatrix;
        }

        private bool hidden;

        public MazePlugState State { get; private set; } = MazePlugState.Inactive;

        private void Reset()
        {
            AutoWire();
        }

        /// <summary>Finds the pieces of the Plug prefab by name (Visual / Collider / SortPoint).</summary>
        private void AutoWire()
        {
            if (blockingCollider == null)
            {
                blockingCollider = GetComponentInChildren<BoxCollider2D>(true);
            }

            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (ySort == null)
            {
                ySort = GetComponent<WorldYSort>();
            }
        }
        public Vector2 Size => size;

        /// <summary>World rectangle covered by this plug.</summary>
        public Rect WorldRect
        {
            get
            {
                Vector2 center = transform.position;
                return new Rect(center - size * 0.5f, size);
            }
        }

        /// <summary>Used by the editor builder to wire the plug.</summary>
        public void Bind(BoxCollider2D collider, SpriteRenderer renderer, Vector2 plugSize, WorldYSort worldYSort = null)
        {
            blockingCollider = collider;
            visual = renderer;
            size = plugSize;
            ySort = worldYSort;

            if (blockingCollider != null)
            {
                blockingCollider.size = size;
                blockingCollider.offset = Vector2.zero;
                blockingCollider.isTrigger = false;
            }
        }

        /// <param name="sortPointDrop">
        /// How far below its own base the plug is sorted, in world units. The player's sort point is
        /// at his feet but his sprite is about 1.8 units tall, and a solid plug stops his feet right
        /// at its base, so without this he would always be drawn over the plug he is standing against.
        /// </param>
        public void SetStyle(Color color, Sprite sprite, float sortPointDrop, int sortingLayerId, int orderBase)
        {
            if (visual == null)
            {
                return;
            }

            visual.sortingLayerID = sortingLayerId;

            // A sprite set on the Plug prefab wins: the generator then paints nothing and only sorts.
            bool prefabArt = sprite == null && visual.sprite != null && visual.sprite != SharedWhiteSprite();

            if (!prefabArt)
            {
                visual.sprite = sprite != null ? sprite : SharedWhiteSprite();
                visual.color = color;
            }

            FitVisualToSize();

            // With WorldYSort the order follows the plug's Y position; dropping the sort point by
            // d units is the same as adding d * OrdersPerUnit to the order.
            if (ySort != null)
            {
                ySort.SetOrderOffset(orderBase + Mathf.RoundToInt(sortPointDrop * ySort.OrdersPerUnit));
            }
        }

        /// <summary>Scales the Visual so the sprite covers exactly the plug area.</summary>
        private void FitVisualToSize()
        {
            if (!stretchVisualToSize || visual == null || visual.sprite == null)
            {
                return;
            }

            Vector2 spriteSize = visual.sprite.bounds.size;
            visual.transform.localScale = new Vector3(
                size.x / Mathf.Max(spriteSize.x, 0.0001f),
                size.y / Mathf.Max(spriteSize.y, 0.0001f),
                1f);
        }

        public void SetState(MazePlugState state)
        {
            State = state;
            bool blocking = state == MazePlugState.Blocking || state == MazePlugState.Locked;

            if (blockingCollider != null)
            {
                blockingCollider.isTrigger = false;
                blockingCollider.enabled = blocking;
            }

            ApplyVisibility();
        }

        /// <summary>
        /// Plugs draw above the fog so the player is never cut by it, so a plug inside a path that
        /// is still covered hides itself instead of being hidden by the fog.
        /// </summary>
        public void SetHidden(bool value)
        {
            hidden = value;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            if (visual == null)
            {
                return;
            }

            bool blocking = State == MazePlugState.Blocking || State == MazePlugState.Locked;
            visual.enabled = blocking && !hidden;
        }

        internal static Sprite SharedWhiteSprite()
        {
            if (whiteSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "MazePlug_WhitePixel",
                    filterMode = FilterMode.Point,
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();

                whiteSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                whiteSprite.name = "MazePlug_WhiteSprite";
                whiteSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            return whiteSprite;
        }
    }
}
