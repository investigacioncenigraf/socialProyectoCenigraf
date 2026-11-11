using System;
using System.Collections.Generic;
using SocialProyectoCenigraf.Dialogue;
using SocialProyectoCenigraf.Items;
using SocialProyectoCenigraf.World.Loot;
using SocialProyectoCenigraf.Player.State;
using SocialProyectoCenigraf.Rewards;
using SocialProyectoCenigraf.World.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace SocialProyectoCenigraf.World.Maze
{
    /// <summary>
    /// Builds a new random route every play and applies it to the rooms, connectors and plugs
    /// that already exist in the scene (created with Tools/Cenigraf/Maze/Build Maze Level).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Cenigraf/Maze/Maze Level Generator")]
    public sealed class MazeLevelGenerator : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private MazeLayout layout = new MazeLayout();

        [Header("Route")]
        [Min(2)] [SerializeField] private int routeSteps = 7;
        [Tooltip("Questions of this level. Without it the rooms fall back to the times table below.")]
        [SerializeField] private MazeQuestionCatalog questionCatalog;
        [Tooltip("Used only when there is no catalog.")]
        [Min(1)] [SerializeField] private int timesTable = 1;
        [Tooltip("Chance that a room with enough free exits shows 3 answers (correct + 2 wrong) instead of 2.")]
        [Range(0f, 1f)] [SerializeField] private float threeAnswerChance = 0.8f;
        [Tooltip("The route tries to have at least this many question rooms that can show 3 answers.")]
        [Min(0)] [SerializeField] private int minThreeAnswerRooms = 4;

        [Header("Seed (one per play)")]
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int fixedSeed = 12345;
        [SerializeField] private int currentSeed;

        [Header("Plugs")]
        [Tooltip("One colour for plugs and fog, so a closed path looks the same as the unexplored dark.")]
        [SerializeField] private Color levelShadeColor = new Color32(0x2B, 0x2B, 0x2B, 0xFF);
        [Tooltip("Optional. Empty = solid colour.")]
        [SerializeField] private Sprite plugSprite;
        [Tooltip("Layer shared by the fog, the plugs, the terminals and the player. Inside it every " +
                 "one of them is sorted by its own lower edge, so who is in front depends on Y.")]
        [SerializeField] private string mazeSortingLayerName = "Foreground";
        [Tooltip("Added to every Y-based order of the maze, to clear the level art drawn in the " +
                 "same layer. Order = -Y x 100 + this.")]
        [SerializeField] private int mazeSortBase = 10000;
        [Tooltip("Extra units to sort a plug below its own SortPoint. The Plug prefab already places " +
                 "its SortPoint low enough to cover the player; use this only to nudge every plug at once.")]
        [Min(0f)] [SerializeField] private float extraPlugSortDrop;


        [Header("Room terminal (centre of every question room)")]
        [SerializeField] private MazeTerminalStyle terminalStyle = new MazeTerminalStyle();
        [Tooltip("After a room is activated, an answer path loses its fog when the player reaches its " +
                 "mouth. The trigger is the path itself, grown by this much along its own direction " +
                 "only, so walking around the room never reveals it by accident.")]
        [Min(0f)] [SerializeField] private float pathRevealMargin = 0.4f;

        [Header("Room guides")]
        [SerializeField] private MazeGuideStyle guideStyle = new MazeGuideStyle();

        [Header("Chests")]
        [Tooltip("One chest per room, except the first and the last one.")]
        [SerializeField] private GameObject chestPrefab;
        [SerializeField] private ChestLootTable chestLootTable;
        [Tooltip("Level of the chest a room gives when the previous one was answered without mistakes.")]
        [Min(1)] [SerializeField] private int baseChestLevel = 3;
        [Min(1)] [SerializeField] private int maxChestLevel = 9;
        [Tooltip("Weights of levels 1, 2 and 3 for the chest of the first room, where nothing has been answered yet.")]
        [SerializeField] private Vector3 firstChestWeights = new Vector3(60f, 30f, 10f);

        [Header("Goal")]
        [Tooltip("Prefab placed at the centre of the last room.")]
        [SerializeField] private GameObject teleportPrefab;
        [Tooltip("Scene the exit teleport sends the player to.")]
        [SerializeField] private string exitScenePath =
            "Assets/_SocialProyectoCenigraf/Scenes/Salas/Propuesta/Templo_De_Los_4_Caminos.unity";
        [Tooltip("Id of the teleport the player arrives at in that scene.")]
        [SerializeField] private string exitTeleportId = "002";
        [Tooltip("Dialogue played at the exit teleport. The journey starts when it ends.")]
        [SerializeField] private DialogueCatalog exitDialogueCatalog;
        [SerializeField] private string exitDialogueId = "laberinto_salida_orientacion";
        [Tooltip("Handed over after arriving at the destination scene, not here.")]
        [SerializeField] private RewardDefinition exitReward;
        [SerializeField] private ItemCatalog exitItemCatalog;
        [SerializeField] private string exitItemId = "gema_orientacion";

        [Header("Camera")]
        [SerializeField] private bool adjustCameraBounds = true;
        [SerializeField] private float cameraMargin = 6f;

        [Header("Scene references (filled by the builder)")]
        [SerializeField] private MazeRoom[] rooms = Array.Empty<MazeRoom>();
        [SerializeField] private MazeConnector[] connectors = Array.Empty<MazeConnector>();

        private readonly Dictionary<MazeCell, MazeRoom> roomByCell = new Dictionary<MazeCell, MazeRoom>();
        private readonly List<MazePlug> passagePlugs = new List<MazePlug>();
        private readonly List<MazeRoom> passageRooms = new List<MazeRoom>();
        private readonly HashSet<MazeRoom> revealedRooms = new HashSet<MazeRoom>();

        private struct LockedPlug
        {
            public MazePlug Plug;
            public MazeConnector Connector;
        }

        private readonly Dictionary<MazeRoom, List<LockedPlug>> lockedPlugsByRoom = new Dictionary<MazeRoom, List<LockedPlug>>();
        private readonly List<MazeConnector> armedConnectors = new List<MazeConnector>();
        private readonly HashSet<MazeEdge> correctConnectors = new HashSet<MazeEdge>();
        private readonly Dictionary<MazeConnector, MazeRoom> armedConnectorRoom =
            new Dictionary<MazeConnector, MazeRoom>();
        private readonly Dictionary<MazeRoom, int> wrongTurnsByRoom = new Dictionary<MazeRoom, int>();
        private int chestCeiling;

        private GameObject teleportInstance;
        private PlayerStateStore playerStore;
        private Collider2D playerCollider;
        private Vector2 pendingStartPosition;
        private bool hasPendingStart;

        public MazeLayout Layout => layout;
        public int CurrentSeed => currentSeed;
        public MazeRoute CurrentRoute { get; private set; }

        /// <summary>Raised when the player takes a wrong exit. Penalty hook (not used yet).</summary>
        public event Action<MazeCell> WrongAnswerChosen;

        /// <summary>Used by the editor to fill the question catalog on an existing generator.</summary>
        public void SetQuestionCatalog(MazeQuestionCatalog catalog)
        {
            questionCatalog = catalog;
        }

        /// <summary>Used by the editor to fill the Teleport prefab on an existing generator.</summary>
        public void SetTeleportPrefab(GameObject prefab)
        {
            teleportPrefab = prefab;
        }

        /// <summary>Used by the editor builder.</summary>
        public void BindScene(MazeRoom[] sceneRooms, MazeConnector[] sceneConnectors)
        {
            rooms = sceneRooms;
            connectors = sceneConnectors;
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            currentSeed = useFixedSeed ? fixedSeed : Guid.NewGuid().GetHashCode();
            Generate(currentSeed, true);
        }

        private void Start()
        {
            if (!hasPendingStart || playerStore == null)
            {
                return;
            }

            playerStore.Dispatch(PlayerAction.ReconcilePosition(pendingStartPosition));
            hasPendingStart = false;
        }

        private void Update()
        {
            if (!Application.isPlaying || playerCollider == null)
            {
                return;
            }

            Bounds bounds = playerCollider.bounds;
            Rect playerRect = new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);

            for (int i = 0; i < armedConnectors.Count; i++)
            {
                MazeConnector connector = armedConnectors[i];

                if (connector == null || !connector.FogCovered)
                {
                    continue;
                }

                Vector2 center = layout.ConnectorCenter(connector.Edge);
                Vector2 size = layout.ConnectorSize(connector.Edge);

                // Grown only along the path, never across it: the player has to be lined up with
                // the corridor and standing at its mouth.
                if (connector.Edge.IsHorizontal)
                {
                    size.x += pathRevealMargin * 2f;
                }
                else
                {
                    size.y += pathRevealMargin * 2f;
                }

                if (new Rect(center - size * 0.5f, size).Overlaps(playerRect))
                {
                    bool correct = correctConnectors.Contains(connector.Edge);
                    connector.RevealFog();

                    if (!correct && armedConnectorRoom.TryGetValue(connector, out MazeRoom source))
                    {
                        wrongTurnsByRoom.TryGetValue(source, out int wrong);
                        wrongTurnsByRoom[source] = wrong + 1;
                    }

                    // The sound lands as the player steps into the path, before reaching the room,
                    // so a right answer is heard before it is seen.
                    if (correct)
                    {
                        SocialProyectoCenigraf.Audio.RetroFeedbackAudio.PlayDiscovery();
                    }
                }
            }

            for (int i = 0; i < passagePlugs.Count; i++)
            {
                MazeRoom room = passageRooms[i];

                if (room == null || revealedRooms.Contains(room))
                {
                    continue;
                }

                if (passagePlugs[i].WorldRect.Overlaps(playerRect))
                {
                    revealedRooms.Add(room);
                    room.Reveal();
                    PlaceChest(room);
                }
            }
        }

        [ContextMenu("Preview plan (current seed)")]
        private void PreviewPlan()
        {
            Generate(useFixedSeed ? fixedSeed : currentSeed, false);
        }

        private void Generate(int seed, bool playing)
        {
            roomByCell.Clear();
            passagePlugs.Clear();
            passageRooms.Clear();
            revealedRooms.Clear();
            armedConnectors.Clear();
            lockedPlugsByRoom.Clear();
            correctConnectors.Clear();
            armedConnectorRoom.Clear();
            wrongTurnsByRoom.Clear();
            chestCeiling = baseChestLevel;

            foreach (MazeRoom room in rooms)
            {
                if (room != null)
                {
                    roomByCell[room.Cell] = room;
                    room.Activated -= HandleRoomActivated;
                    room.Activated += HandleRoomActivated;
                }
            }

            if (!MazeRouteGenerator.TryGenerate(layout, routeSteps, seed, out MazeRoute route, 500, minThreeAnswerRooms))
            {
                Debug.LogError($"[Maze] Could not build a route with seed {seed}.", this);
                return;
            }

            CurrentRoute = route;

            for (int i = 0; i < route.Count - 1; i++)
            {
                correctConnectors.Add(MazeEdge.Between(
                    route[i], MazeLevelPlanner.DirectionBetween(route[i], route[i + 1])));
            }

            if (playing)
            {
                foreach (MazeRoom room in rooms)
                {
                    if (room != null && !room.HasTerminal)
                    {
                        Debug.LogError(
                            "[Maze] The rooms have no Terminal yet. Run Tools > Cenigraf > Maze > " +
                            "Build Maze Level in Open Scene (your generator settings are kept).",
                            this);
                        break;
                    }
                }
            }

            System.Random random = new System.Random(seed ^ 0x5F3759DF);
            IMazeQuestionProvider questions =
                questionCatalog != null && questionCatalog.Count > 0
                    ? new MazeCatalogQuestionProvider(questionCatalog, random)
                    : (IMazeQuestionProvider)new TimesTableQuestionProvider(timesTable);

            MazeLevelPlan plan = MazeLevelPlanner.Build(
                layout, route, questions, random, threeAnswerChance);

            int layerId = SortingLayer.NameToID(mazeSortingLayerName);

            foreach (MazeConnector connector in connectors)
            {
                if (connector == null)
                {
                    continue;
                }

                connector.ApplyFogLayer(layerId, levelShadeColor, mazeSortBase);
                connector.ApplyStyle(levelShadeColor, plugSprite, extraPlugSortDrop, layerId, mazeSortBase);

                if (plan.Connectors.TryGetValue(connector.Edge, out MazeConnectorPlan connectorPlan))
                {
                    connector.Apply(connectorPlan);
                    RegisterPassage(connector.PlugFrom, connectorPlan.PlugFrom, connector.Edge.From);
                    RegisterPassage(connector.PlugTo, connectorPlan.PlugTo, connector.Edge.To);
                    RegisterLocked(connector, connector.PlugFrom, connectorPlan.PlugFrom, connector.Edge.From);
                    RegisterLocked(connector, connector.PlugTo, connectorPlan.PlugTo, connector.Edge.To);
                }
            }

            foreach (MazeRoom room in rooms)
            {
                if (room != null)
                {
                    room.ApplyFogLayer(layerId, levelShadeColor, mazeSortBase);
                }

                if (room != null && plan.Rooms.TryGetValue(room.Cell, out MazeRoomPlan roomPlan))
                {
                    room.Apply(roomPlan, guideStyle, terminalStyle, layout.roomSize, layerId, mazeSortBase);
                }
            }

            Debug.Log($"[Maze] seed={seed} route={route}");

            if (!playing)
            {
                return;
            }

            if (roomByCell.TryGetValue(route.Start, out MazeRoom startRoom))
            {
                startRoom.Reveal();
                revealedRooms.Add(startRoom);
            }

            PlaceTeleport(route.End);
            PlacePlayer(route.Start);
            BindGuides();
            ApplyCameraBounds();
        }

        /// <summary>
        /// The chest of a room pays for the room before it: it starts at the ceiling and loses one
        /// level per wrong path opened there. A room answered clean raises the ceiling by one, so a
        /// run without mistakes keeps finding better chests.
        /// </summary>
        private void PlaceChest(MazeRoom room)
        {
            if (chestPrefab == null || chestLootTable == null || CurrentRoute == null)
            {
                return;
            }

            int index = CurrentRoute.IndexOf(room.Cell);

            // No chest in the start room, and none in the room with the teleport.
            if (index <= 0 || index >= CurrentRoute.Count - 1)
            {
                return;
            }

            int level;

            if (index == 1)
            {
                level = RollFirstChestLevel();
            }
            else
            {
                MazeRoom previous = roomByCell[CurrentRoute[index - 1]];
                wrongTurnsByRoom.TryGetValue(previous, out int wrong);

                if (wrong == 0)
                {
                    chestCeiling = Mathf.Min(maxChestLevel, chestCeiling + 1);
                }

                level = Mathf.Clamp(chestCeiling - wrong, 1, maxChestLevel);
            }

            room.SpawnChest(chestPrefab, chestLootTable, level, index, CurrentRoute.Count - 1);
        }

        /// <summary>Nothing has been answered yet, so the first chest is luck, and modest.</summary>
        private int RollFirstChestLevel()
        {
            float total = Mathf.Max(0.0001f,
                firstChestWeights.x + firstChestWeights.y + firstChestWeights.z);
            float roll = UnityEngine.Random.Range(0f, total);

            if (roll < firstChestWeights.x)
            {
                return 1;
            }

            return roll < firstChestWeights.x + firstChestWeights.y ? 2 : 3;
        }

        private void RegisterPassage(MazePlug plug, MazePlugState state, MazeCell cell)
        {
            if (plug == null || state != MazePlugState.Passage || !roomByCell.TryGetValue(cell, out MazeRoom room))
            {
                return;
            }

            passagePlugs.Add(plug);
            passageRooms.Add(room);
        }

        private void RegisterLocked(MazeConnector connector, MazePlug plug, MazePlugState state, MazeCell cell)
        {
            if (plug == null || state != MazePlugState.Locked || !roomByCell.TryGetValue(cell, out MazeRoom room))
            {
                return;
            }

            if (!lockedPlugsByRoom.TryGetValue(room, out List<LockedPlug> list))
            {
                list = new List<LockedPlug>();
                lockedPlugsByRoom[room] = list;
            }

            list.Add(new LockedPlug { Plug = plug, Connector = connector });
        }

        private void HandleRoomActivated(MazeRoom room)
        {
            if (!lockedPlugsByRoom.TryGetValue(room, out List<LockedPlug> list))
            {
                return;
            }

            foreach (LockedPlug locked in list)
            {
                locked.Plug.SetState(MazePlugState.Inactive);

                if (!armedConnectors.Contains(locked.Connector))
                {
                    armedConnectors.Add(locked.Connector);
                    armedConnectorRoom[locked.Connector] = room;
                }
            }

            list.Clear();
        }

        private void PlaceTeleport(MazeCell cell)
        {
            if (teleportPrefab == null)
            {
                return;
            }

            if (teleportInstance != null)
            {
                Destroy(teleportInstance);
            }

            Vector2 center = layout.RoomCenter(cell);
            teleportInstance = Instantiate(teleportPrefab, new Vector3(center.x, center.y, 0f), Quaternion.identity);

            SocialProyectoCenigraf.World.Teleport.SceneTeleport exit =
                teleportInstance.GetComponentInChildren<SocialProyectoCenigraf.World.Teleport.SceneTeleport>(true);

            if (exit != null)
            {
                exit.ConfigureDestination(exitScenePath, exitTeleportId);
                exit.ConfigureTravelDialogue(exitDialogueCatalog, exitDialogueId);
                exit.ConfigureArrivalReward(exitReward, exitItemCatalog, exitItemId);
                exit.SetPortalActive(true);
            }
            else
            {
                Debug.LogWarning("[Maze] The teleport prefab has no SceneTeleport component.", this);
            }
        }

        private void PlacePlayer(MazeCell cell)
        {
            playerStore = FindFirstObjectByType<PlayerStateStore>();

            if (playerStore == null)
            {
                Debug.LogWarning("[Maze] No PlayerStateStore found: the player was not placed.", this);
                return;
            }

            Vector2 center = layout.RoomCenter(cell);
            Transform playerTransform = playerStore.transform;
            playerTransform.position = new Vector3(center.x, center.y, playerTransform.position.z);

            Rigidbody2D body = playerStore.GetComponent<Rigidbody2D>();

            if (body != null)
            {
                body.position = center;
            }

            playerCollider = playerStore.GetComponentInChildren<Collider2D>();
            ApplyPlayerSorting(playerStore);
            pendingStartPosition = center;
            hasPendingStart = true;
            Physics2D.SyncTransforms();
        }

        /// <summary>
        /// Puts the player in the same layer as the plugs and the terminals, above the fog, so the
        /// dark never cuts him in half while he walks into a path that is still covered.
        /// </summary>
        private void ApplyPlayerSorting(PlayerStateStore store)
        {
            int layerId = SortingLayer.NameToID(mazeSortingLayerName);
            SortingGroup group = store.GetComponentInChildren<SortingGroup>(true);

            if (group != null)
            {
                group.sortingLayerID = layerId;
            }

            // Renderers of the player that are not inside the sorting group would stay behind.
            foreach (Renderer renderer in store.GetComponentsInChildren<Renderer>(true))
            {
                if (group == null || !renderer.transform.IsChildOf(group.transform))
                {
                    renderer.sortingLayerID = layerId;
                }
            }

            WorldYSort sorting = store.GetComponentInChildren<WorldYSort>(true);

            if (sorting != null)
            {
                sorting.SetOrderOffset(mazeSortBase);
                sorting.RefreshSortingOrder();
            }

            Debug.Log(
                $"[Maze] sorting in '{mazeSortingLayerName}': fog, plugs, terminals and player are " +
                $"Y-sorted from base {mazeSortBase} (player group: " +
                $"{(group != null ? group.name : "none")}), text {guideStyle.guideSortingOrder}.",
                this);
        }

        private void BindGuides()
        {
            if (playerStore == null)
            {
                return;
            }

            foreach (MazeRoom room in rooms)
            {
                if (room != null)
                {
                    room.BindPlayer(playerStore.transform, layout.roomSize);
                }
            }
        }

        private void ApplyCameraBounds()
        {
            if (!adjustCameraBounds)
            {
                return;
            }

            CameraBounds bounds = FindFirstObjectByType<CameraBounds>();

            if (bounds == null)
            {
                return;
            }

            Rect level = layout.LevelRect;
            bounds.minX = level.xMin - cameraMargin;
            bounds.maxX = level.xMax + cameraMargin;
            bounds.minY = level.yMin - cameraMargin;
            bounds.maxY = level.yMax + cameraMargin;
        }

        /// <summary>Penalty hook for later. Nothing calls it yet.</summary>
        public void ReportWrongAnswer(MazeCell room)
        {
            WrongAnswerChosen?.Invoke(room);
        }
    }
}
