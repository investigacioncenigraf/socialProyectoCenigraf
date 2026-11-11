using System.Collections;
using SocialProyectoCenigraf.Controls;
using SocialProyectoCenigraf.World.Loot;
using SocialProyectoCenigraf.World.Rendering;
using UnityEngine;

namespace SocialProyectoCenigraf.World.Maze
{
    /// <summary>One room of the grid: its fog and its question guide.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cenigraf/Maze/Maze Room")]
    public sealed class MazeRoom : MonoBehaviour
    {
        [SerializeField] private Vector2Int cell;
        [SerializeField] private ShadowArea fog;
        [SerializeField] private MazeRoomGuide guide;
        [SerializeField] private MazeRoomTerminal terminal;

        [Tooltip("Tiempo que el jugador queda quieto cuando aparece la pregunta, para que alcance a leerla.")]
        [Min(0f)] [SerializeField] private float questionPauseSeconds = 1.5f;

        private LootChest chest;
        private int roomNumber;
        private int roomTotal;

        /// <summary>Raised once, when the player activates the room's terminal.</summary>
        public event System.Action<MazeRoom> Activated;

        public MazeCell Cell => new MazeCell(cell.x, cell.y);
        public MazeRoomGuide Guide => guide;
        public bool HasTerminal => terminal != null;

        /// <summary>True when the room is not hidden by fog (or its fog has been revealed).</summary>
        public bool IsRevealed => fog == null || !fog.gameObject.activeSelf || fog.IsRevealed;

        /// <summary>Used by the editor builder to wire the room.</summary>
        public void Bind(Vector2Int roomCell, ShadowArea shadow, MazeRoomGuide roomGuide, MazeRoomTerminal roomTerminal)
        {
            cell = roomCell;
            fog = shadow;
            guide = roomGuide;
            terminal = roomTerminal;
        }

        private void OnEnable()
        {
            if (terminal != null)
            {
                terminal.Used += HandleTerminalUsed;
            }
        }

        private void OnDisable()
        {
            GameplayInputLock.Release(this);

            if (terminal != null)
            {
                terminal.Used -= HandleTerminalUsed;
            }
        }

        /// <summary>
        /// Puts a chest in the middle of the room. Until the player opens it the terminal stays
        /// out of the way: first the reward of the previous room, then the next question.
        /// </summary>
        public void SpawnChest(GameObject prefab, ChestLootTable table, int level, int number, int total)
        {
            roomNumber = number;
            roomTotal = total;

            if (prefab == null || chest != null)
            {
                return;
            }

            GameObject instance = Instantiate(prefab, transform.position, Quaternion.identity, transform);
            instance.name = "Chest";
            chest = instance.GetComponent<LootChest>();

            if (chest == null)
            {
                Destroy(instance);
                return;
            }

            chest.Configure(level, table);
            chest.Opened += HandleChestOpened;
        }

        /// <summary>
        /// Opening the chest closes the previous stage: the band announces the next one and, when
        /// it leaves, the question and its answers appear.
        /// </summary>
        private void HandleChestOpened(LootChest opened)
        {
            opened.Opened -= HandleChestOpened;

            string subtitle = roomTotal > 0
                ? $"Sala {roomNumber} de {roomTotal}"
                : string.Empty;

            SocialProyectoCenigraf.UI.StageBanner.Show("Siguiente prueba", subtitle, () =>
            {
                opened.MarkAsEmptied();
                OpenStage();
            });
        }

        private void HandleTerminalUsed() => OpenStage();

        /// <summary>Shows the question of this room and opens the paths that answer it.</summary>
        private void OpenStage()
        {
            if (guide != null)
            {
                guide.SetUnlocked(true);
            }

            if (questionPauseSeconds > 0f && isActiveAndEnabled)
            {
                StartCoroutine(HoldPlayerWhileReading());
            }

            if (terminal != null)
            {
                terminal.SetGated(true);
            }

            Activated?.Invoke(this);
        }

        public void ApplyFogLayer(int sortingLayerId, Color color, int orderBase)
        {
            if (fog == null)
            {
                return;
            }

            fog.SetShadowColor(color);

            SpriteRenderer fogRenderer = fog.GetComponent<SpriteRenderer>();

            if (fogRenderer != null)
            {
                fogRenderer.sortingLayerID = sortingLayerId;
            }

            // The fog is sorted by its own lower edge, like any world object, so the player is
            // never cut by it when he walks up to a path that is still covered.
            WorldYSort fogSorting = fog.GetComponent<WorldYSort>();

            if (fogSorting != null)
            {
                fogSorting.SetOrderOffset(orderBase);
                fogSorting.RefreshSortingOrder();
            }
        }

        public void Apply(
            MazeRoomPlan plan,
            MazeGuideStyle style,
            MazeTerminalStyle terminalStyle,
            float roomSize,
            int sortingLayerId,
            int orderBase)
        {
            if (fog != null)
            {
                fog.gameObject.SetActive(plan.Fogged);

                if (plan.Fogged)
                {
                    fog.CoverAgain();
                }
            }

            if (terminal != null)
            {
                terminal.gameObject.SetActive(plan.HasQuestion && terminalStyle.useTerminal);

                if (plan.HasQuestion && terminalStyle.useTerminal)
                {
                    terminal.ApplyStyle(terminalStyle, sortingLayerId, orderBase);
                    terminal.ResetState();
                    terminal.SetHidden(plan.Fogged);
                }
            }

            if (guide != null)
            {
                guide.gameObject.SetActive(plan.HasQuestion);

                if (plan.HasQuestion)
                {
                    guide.ApplyStyle(style, roomSize);
                    guide.Configure(plan);
                }
            }
        }

        /// <summary>Fades the fog out. Called when the player crosses one of the room's doors.</summary>
        /// <summary>Hands the player to the pieces that need to know where he is.</summary>
        public void BindPlayer(Transform target, float size)
        {
            if (guide != null)
            {
                guide.SetPlayer(target);
            }

            if (terminal != null)
            {
                terminal.SetPlayer(target, size);
            }
        }

        /// <summary>Keeps the player still for a moment, so the question is read before moving.</summary>
        private IEnumerator HoldPlayerWhileReading()
        {
            GameplayInputLock.Acquire(this);

            float elapsed = 0f;

            while (elapsed < questionPauseSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            GameplayInputLock.Release(this);
        }

        public void Reveal()
        {
            if (fog != null && fog.gameObject.activeSelf)
            {
                fog.RevealNow(false);
            }

            if (terminal != null)
            {
                terminal.SetHidden(false);
            }
        }
    }
}
