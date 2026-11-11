using System;
using SocialProyectoCenigraf.Controls;
using SocialProyectoCenigraf.Dialogue;
using SocialProyectoCenigraf.Mobile;
using SocialProyectoCenigraf.Player.Movement;
using SocialProyectoCenigraf.Rewards;
using SocialProyectoCenigraf.World.NPC;
using SocialProyectoCenigraf.World.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SocialProyectoCenigraf.World.Maze
{
    /// <summary>Look of every room terminal. Edited once on the MazeLevelGenerator.</summary>
    [Serializable]
    public sealed class MazeTerminalStyle
    {
        [Tooltip("Apagado: el cofre abre la prueba y no se dibuja ningún elemento en el centro.")]
        public bool useTerminal;
        public Color color = new Color32(0xD9, 0xD9, 0xD9, 0xFF);
        public float size = 2f;
        [Tooltip("Side of the square area where the interaction icon appears.")]
        public float interactionAreaSize = 4f;
        public float indicatorOffsetY = 1.5f;
        [Tooltip("How far above the terminal the interaction icon is drawn.")]
        public int indicatorOrderGap = 50;
        [Tooltip("Draw the terminal only while the player is inside its room.")]
        public bool onlyVisibleInsideRoom = true;
        [Tooltip("Cuánto tarda en aparecer o desaparecer, en segundos.")]
        [Min(0f)] public float fadeSeconds = 0.45f;
    }

    /// <summary>
    /// The element at the centre of a question room. The player must interact with it (E / mobile
    /// button) to open the room: its plugs disappear and the question, answers and arrows appear.
    /// Reuses the NPC interaction zone and indicator.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cenigraf/Maze/Maze Room Terminal")]
    public sealed class MazeRoomTerminal : MonoBehaviour, IMobileInteractionTarget
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private NPCInteractionZone interactionZone;
        [SerializeField] private NPCInteractionIndicatorController indicator;
        [SerializeField] private SpriteRenderer indicatorRenderer;
        [SerializeField] private WorldYSort ySort;
        [SerializeField] private Transform sortPoint;

        private MazeTerminalStyle style;
        private Color baseColor = Color.white;
        private Transform player;
        private float roomSize = 6f;
        private bool playerInside;
        private bool hidden;
        private bool gated;
        private float currentAlpha;
        private float targetAlpha;
        private bool used;

        /// <summary>Raised once, when the player interacts with the terminal.</summary>
        public event Action Used;

        public bool IsUsed => used;
        public Vector3 InteractionPosition => transform.position;

        /// <summary>Used by the editor builder.</summary>
        public void Bind(
            SpriteRenderer visualRenderer,
            NPCInteractionZone zone,
            NPCInteractionIndicatorController indicatorController,
            SpriteRenderer indicatorSpriteRenderer,
            WorldYSort worldYSort = null,
            Transform worldSortPoint = null)
        {
            ySort = worldYSort;
            sortPoint = worldSortPoint;
            visual = visualRenderer;
            interactionZone = zone;
            indicator = indicatorController;
            indicatorRenderer = indicatorSpriteRenderer;
        }

        public void ApplyStyle(MazeTerminalStyle newStyle, int sortingLayerId, int orderBase)
        {
            style = newStyle;

            if (style == null)
            {
                return;
            }

            int layerId = sortingLayerId;

            if (visual != null)
            {
                visual.sortingLayerID = layerId;

                // A sprite set on the Room prefab wins: it keeps its own art and its own colour,
                // and only its alpha changes once the terminal has been used.
                bool ownArt = visual.sprite != null && visual.sprite != MazePlug.SharedWhiteSprite();

                if (ownArt)
                {
                    baseColor = visual.color;
                    baseColor.a = 1f;

                    // Uniform fit so the art keeps its proportions inside the terminal square.
                    Vector2 artSize = visual.sprite.bounds.size;
                    float fit = style.size / Mathf.Max(Mathf.Max(artSize.x, artSize.y), 0.0001f);
                    visual.transform.localScale = new Vector3(fit, fit, 1f);
                }
                else
                {
                    baseColor = style.color;
                    visual.sprite = MazePlug.SharedWhiteSprite();
                    visual.transform.localScale = new Vector3(style.size, style.size, 1f);
                }

                // Sorted by Y together with the player; the sort point is the terminal's lower edge.
                if (sortPoint != null)
                {
                    sortPoint.localPosition = new Vector3(0f, -style.size * 0.5f, 0f);
                }

                if (ySort != null)
                {
                    ySort.SetOrderOffset(orderBase);
                }
                else
                {
                    visual.sortingOrder = orderBase;
                }
            }

            if (interactionZone != null)
            {
                BoxCollider2D area = interactionZone.GetComponent<BoxCollider2D>();

                if (area != null)
                {
                    area.size = Vector2.one * style.interactionAreaSize;
                    area.offset = Vector2.zero;
                }
            }

            if (indicatorRenderer != null)
            {
                indicatorRenderer.sortingLayerID = layerId;
                indicatorRenderer.sortingOrder =
                    (ySort != null ? ySort.CurrentSortingOrder : orderBase) + style.indicatorOrderGap;
                indicatorRenderer.transform.localPosition = new Vector3(0f, style.indicatorOffsetY, 0f);
            }

            ApplyVisual();
        }

        /// <summary>
        /// The terminal draws above the fog so the player is never cut by it, so it hides itself
        /// while its room is still covered.
        /// </summary>
        /// <summary>The room tells the terminal who the player is, to know when he is inside.</summary>
        public void SetPlayer(Transform target, float size)
        {
            player = target;
            roomSize = size;
            playerInside = target != null && IsInsideRoom(target.position);
            ApplyVisual();
        }

        private bool IsInsideRoom(Vector3 position)
        {
            float half = roomSize * 0.5f;
            Vector3 center = transform.position;
            return Mathf.Abs(position.x - center.x) <= half &&
                   Mathf.Abs(position.y - center.y) <= half;
        }

        /// <summary>While gated the terminal is neither seen nor usable: the chest comes first.</summary>
        public void SetGated(bool value)
        {
            gated = value;

            if (gated)
            {
                indicator?.Hide();
            }

            ApplyVisual();
        }

        public void SetHidden(bool value)
        {
            hidden = value;
            ApplyVisual();
        }

        /// <summary>Back to the "waiting for the player" state.</summary>
        public void ResetState()
        {
            used = false;

            if (interactionZone != null)
            {
                interactionZone.gameObject.SetActive(true);
            }

            ApplyVisual();
        }

        private void OnEnable()
        {
            MobileInteractionTargets.Register(this);
        }

        private void OnDisable()
        {
            MobileInteractionTargets.Unregister(this);
            indicator?.Hide();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            UpdatePlayerPresence();
            UpdateFade();

            if (used)
            {
                return;
            }

            bool unavailable = gated || DialoguePresenter.IsOpen || RewardWindow.IsOpen;
            indicator?.SetVisible(interactionZone != null && interactionZone.HasPlayerInside && !unavailable);

            if (GameplayInputLock.IsLocked || unavailable ||
                Time.frameCount == DialoguePresenter.LastClosedFrame ||
                Time.frameCount == RewardWindow.LastClosedFrame ||
                Keyboard.current == null ||
                !Keyboard.current.eKey.wasPressedThisFrame)
            {
                return;
            }

            Interact();
        }

        public bool Interact()
        {
            if (used || gated || interactionZone == null || !interactionZone.TryGetLocalPlayer(out _))
            {
                return false;
            }

            used = true;
            indicator?.Hide();

            if (interactionZone != null)
            {
                interactionZone.gameObject.SetActive(false);
            }

            ApplyVisual();
            Used?.Invoke();
            return true;
        }

        public bool CanInteractWith(PlayerMovementController player)
        {
            return !used &&
                   !gated &&
                   !GameplayInputLock.IsLocked &&
                   player != null &&
                   player.AcceptsLocalInput &&
                   player.isActiveAndEnabled &&
                   interactionZone != null &&
                   interactionZone.Contains(player) &&
                   !DialoguePresenter.IsOpen &&
                   !RewardWindow.IsOpen;
        }

        public void InteractWith(PlayerMovementController player)
        {
            if (CanInteractWith(player))
            {
                Interact();
            }
        }

        private void UpdatePlayerPresence()
        {
            if (style == null || !style.onlyVisibleInsideRoom || player == null)
            {
                return;
            }

            bool inside = IsInsideRoom(player.position);

            if (inside == playerInside)
            {
                return;
            }

            playerInside = inside;
            ApplyVisual();
        }

        /// <summary>Decides whether the terminal should be seen; the fade does the rest.</summary>
        private void ApplyVisual()
        {
            if (visual == null || style == null)
            {
                return;
            }

            bool insideItsRoom = !style.onlyVisibleInsideRoom || player == null || playerInside;
            targetAlpha = !hidden && !gated && insideItsRoom ? 1f : 0f;

            if (!Application.isPlaying || style.fadeSeconds <= 0f)
            {
                currentAlpha = targetAlpha;
            }

            ApplyAlpha();
        }

        /// <summary>The terminal fades in and out instead of popping on screen.</summary>
        private void UpdateFade()
        {
            if (visual == null || style == null || Mathf.Approximately(currentAlpha, targetAlpha))
            {
                return;
            }

            currentAlpha = style.fadeSeconds <= 0f
                ? targetAlpha
                : Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / style.fadeSeconds);

            ApplyAlpha();
        }

        private void ApplyAlpha()
        {
            bool visible = currentAlpha > 0.01f;

            if (visual.enabled != visible)
            {
                visual.enabled = visible;
            }

            Color color = baseColor;
            color.a = currentAlpha;
            visual.color = color;
        }
    }
}
