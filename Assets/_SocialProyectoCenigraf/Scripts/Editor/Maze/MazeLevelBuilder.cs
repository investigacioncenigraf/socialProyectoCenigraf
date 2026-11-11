using System.Collections.Generic;
using System.IO;
using SocialProyectoCenigraf.Dialogue;
using SocialProyectoCenigraf.Items;
using SocialProyectoCenigraf.Rewards;
using SocialProyectoCenigraf.World.Loot;
using SocialProyectoCenigraf.World.Maze;
using SocialProyectoCenigraf.World.NPC;
using SocialProyectoCenigraf.World.Rendering;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SocialProyectoCenigraf.EditorTools.Maze
{
    /// <summary>
    /// Creates the "MazeLevel" hierarchy in the open scene: 52 rooms, 87 connectors, plugs, fogs
    /// and guides, all wired to a MazeLevelGenerator. Does not save the scene.
    /// </summary>
    public static class MazeLevelBuilder
    {
        private const string RootName = "MazeLevel";
        private const string FontGuid = "64792600f61ce40d5bd80714a733aaa8";
        private const string PlugPrefabPath =
            "Assets/_SocialProyectoCenigraf/Prefabs/InScene/Maze/Plug.prefab";
        private const string ShadowPrefabPath =
            "Assets/_SocialProyectoCenigraf/Prefabs/InScene/Maze/ShadowAreaY.prefab";
        private const string RoomPrefabPath =
            "Assets/_SocialProyectoCenigraf/Prefabs/InScene/Maze/Room.prefab";
        /// <summary>
        /// How far below its own base a plug is sorted. The player's sort point is at his feet but
        /// his sprite is about this tall, and a solid plug stops his feet right at its base, so
        /// without this he would always be drawn over the plug he is standing against.
        /// </summary>
        private const float PlugSortPointDrop = 1.8f;

        private const string IndicatorGuid = "532eee3a7a8cd5b429f8a96dec4347d5";
        private const string ArrowGuid = "69580e264c4e48e0b17d6da94171a5c4";

        [MenuItem("Tools/Cenigraf/Maze/Build Maze Level in Open Scene")]
        public static void Build()
        {
            GameObject existing = GameObject.Find(RootName);
            MazeLevelGenerator previousSettings = null;
            GameObject savedSettings = null;

            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Maze Level",
                        "A 'MazeLevel' object already exists. Delete it and build it again?\n" +
                        "(Generator settings are kept.)",
                        "Rebuild", "Cancel"))
                {
                    return;
                }

                previousSettings = existing.GetComponent<MazeLevelGenerator>();

                if (previousSettings != null)
                {
                    savedSettings = new GameObject("MazeSettingsBackup") { hideFlags = HideFlags.HideAndDontSave };
                    MazeLevelGenerator copy = savedSettings.AddComponent<MazeLevelGenerator>();
                    EditorUtility.CopySerialized(previousSettings, copy);
                }

                Undo.DestroyObjectImmediate(existing);
            }

            TMP_FontAsset font = LoadByGuid<TMP_FontAsset>(FontGuid);
            Sprite arrow = LoadFirstSprite(ArrowGuid);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Maze Level");
            MazeLevelGenerator generator = root.AddComponent<MazeLevelGenerator>();
            MazeLayout layout = generator.Layout;

            if (savedSettings != null)
            {
                MazeLevelGenerator oldGenerator = savedSettings.GetComponent<MazeLevelGenerator>();
                EditorUtility.CopySerialized(oldGenerator, generator);
                Object.DestroyImmediate(savedSettings);
                layout = generator.Layout;
            }

            Sprite indicatorSprite = LoadNamedSprite(IndicatorGuid, "teclaE_0");
            GameObject plugPrefab = EnsurePlugPrefab(layout);
            GameObject shadowPrefab = EnsureShadowPrefab(2f);
            GameObject roomPrefab = EnsureRoomPrefab(layout, font, arrow, indicatorSprite, shadowPrefab);

            Transform roomsRoot = new GameObject("Rooms").transform;
            roomsRoot.SetParent(root.transform, false);
            Transform connectorsRoot = new GameObject("Connectors").transform;
            connectorsRoot.SetParent(root.transform, false);

            List<MazeRoom> rooms = new List<MazeRoom>();
            List<MazeConnector> connectors = new List<MazeConnector>();
            List<GameObject> fogs = new List<GameObject>();

            foreach (MazeCell cell in layout.Cells())
            {
                rooms.Add(BuildRoom(layout, cell, roomsRoot, roomPrefab, font, arrow, indicatorSprite, fogs));
            }

            foreach (MazeEdge edge in layout.Edges())
            {
                connectors.Add(BuildConnector(layout, edge, connectorsRoot, plugPrefab, shadowPrefab, fogs));
            }

            generator.BindScene(rooms.ToArray(), connectors.ToArray());

            SerializedObject so = new SerializedObject(generator);
            MazeQuestionCatalog catalog = FindQuestionCatalog();

            if (catalog != null)
            {
                so.FindProperty("questionCatalog").objectReferenceValue = catalog;
            }

            AssignExitContent(so);
            GameObject teleport = FindTeleportPrefab();

            if (teleport != null)
            {
                so.FindProperty("teleportPrefab").objectReferenceValue = teleport;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (GameObject fog in fogs)
            {
                SceneVisibilityManager.instance.Hide(fog, false);
            }

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;

            Debug.Log($"[Maze] Built {rooms.Count} rooms and {connectors.Count} connectors. " +
                      (teleport != null ? $"Teleport prefab: {teleport.name}." : "Assign the Teleport prefab on the generator.") +
                      " Remember to save the scene.");
        }

        [MenuItem("Tools/Cenigraf/Maze/Create ShadowAreaY Prefab")]
        public static void CreateShadowPrefab()
        {
            GameObject asset = EnsureShadowPrefab(2f);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>
        /// Creates (once) the ShadowAreaY prefab: a 2x2 piece of fog built like an ObjectDecoration,
        /// so a WorldYSort sorts it by its base against the player instead of a fixed order.
        /// </summary>
        private static GameObject EnsureShadowPrefab(float side)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ShadowPrefabPath);

            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(ShadowPrefabPath);

            GameObject root = new GameObject("ShadowAreaY");
            ShadowArea shadow = root.AddComponent<ShadowArea>();
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            renderer.sortingLayerID = SortingLayer.NameToID("WorldDynamic");

            GameObject sortPoint = new GameObject("SortPoint");
            sortPoint.transform.SetParent(root.transform, false);
            sortPoint.transform.localPosition = new Vector3(0f, -side * 0.5f, 0f);

            AddYSort(root, sortPoint.transform, renderer);

            SerializedObject so = new SerializedObject(shadow);
            so.FindProperty("coverSize").vector2Value = Vector2.one * side;
            so.FindProperty("coverOffset").vector2Value = Vector2.zero;
            so.FindProperty("shadowColor").colorValue = new Color32(0x2B, 0x2B, 0x2B, 0xFF);
            so.FindProperty("controlSortingOrder").boolValue = false;
            so.FindProperty("autoRevealByPlayerZone").boolValue = false;
            so.FindProperty("findLocalPlayerAutomatically").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            BoxCollider2D zone = root.GetComponent<BoxCollider2D>();

            if (zone != null)
            {
                zone.isTrigger = true;
                zone.size = Vector2.one * side;
                zone.offset = Vector2.zero;
                zone.enabled = false;
            }

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, ShadowPrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[Maze] ShadowAreaY prefab created at {ShadowPrefabPath}.");
            return asset;
        }

        [MenuItem("Tools/Cenigraf/Maze/Assign Teleport Prefab to Generator")]
        public static void AssignTeleport()
        {
            MazeLevelGenerator generator = Object.FindFirstObjectByType<MazeLevelGenerator>();
            GameObject teleport = FindTeleportPrefab();

            if (generator == null || teleport == null)
            {
                Debug.LogWarning("[Maze] Generator or Teleport prefab not found.");
                return;
            }

            Undo.RecordObject(generator, "Assign Teleport");
            generator.SetTeleportPrefab(teleport);
            EditorUtility.SetDirty(generator);
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            Debug.Log($"[Maze] Teleport prefab assigned: {teleport.name}. Save the scene.");
        }

        [MenuItem("Tools/Cenigraf/Maze/Show or Hide Fogs in Scene View")]
        public static void ToggleFogVisibility()
        {
            GameObject root = GameObject.Find(RootName);

            if (root == null)
            {
                return;
            }

            ShadowArea[] fogs = root.GetComponentsInChildren<ShadowArea>(true);
            SceneVisibilityManager manager = SceneVisibilityManager.instance;
            bool anyHidden = false;

            foreach (ShadowArea fog in fogs)
            {
                if (manager.IsHidden(fog.gameObject))
                {
                    anyHidden = true;
                    break;
                }
            }

            foreach (ShadowArea fog in fogs)
            {
                if (anyHidden) manager.Show(fog.gameObject, false);
                else manager.Hide(fog.gameObject, false);
            }
        }

        /// <summary>
        /// Creates (once) the Room prefab: the fog, the guide (question, answers, arrows) and the
        /// terminal of a room. Every room in the level is an instance of it, so editing the prefab
        /// changes all 52 rooms at once.
        /// </summary>
        private static GameObject EnsureRoomPrefab(
            MazeLayout layout, TMP_FontAsset font, Sprite arrow, Sprite indicatorSprite, GameObject shadowPrefab)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);

            if (existing != null)
            {
                return UpgradeRoomFog(existing, layout, shadowPrefab);
            }

            EnsureFolder(RoomPrefabPath);

            GameObject root = new GameObject("Room");
            MazeRoom room = root.AddComponent<MazeRoom>();

            List<GameObject> prefabFogs = new List<GameObject>();
            ShadowArea fog = BuildFog(root.transform, "Fog", Vector2.one * layout.roomSize, prefabFogs, shadowPrefab);
            MazeRoomGuide guide = BuildGuide(root.transform, font, arrow);
            MazeRoomTerminal terminal = BuildTerminal(root.transform, indicatorSprite);
            room.Bind(Vector2Int.zero, fog, guide, terminal);

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, RoomPrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[Maze] Room prefab created at {RoomPrefabPath}.");
            return asset;
        }

        /// <summary>
        /// Replaces the hand-made fog of an older Room prefab with a ShadowAreaY instance, keeping
        /// everything else the user may have set up in it.
        /// </summary>
        private static GameObject UpgradeRoomFog(GameObject prefab, MazeLayout layout, GameObject shadowPrefab)
        {
            if (shadowPrefab == null)
            {
                return prefab;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(RoomPrefabPath);

            try
            {
                ShadowArea current = contents.GetComponentInChildren<ShadowArea>(true);

                if (current != null && current.GetComponent<WorldYSort>() != null)
                {
                    return prefab;
                }

                if (current != null)
                {
                    Object.DestroyImmediate(current.gameObject);
                }

                List<GameObject> ignored = new List<GameObject>();
                ShadowArea fog = BuildFog(
                    contents.transform, "Fog", Vector2.one * layout.roomSize, ignored, shadowPrefab);

                MazeRoom room = contents.GetComponent<MazeRoom>();

                if (room != null)
                {
                    SerializedObject so = new SerializedObject(room);
                    so.FindProperty("fog").objectReferenceValue = fog;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(contents, RoomPrefabPath);
                Debug.Log("[Maze] Room prefab: its fog is now a ShadowAreaY instance.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
        }

        private static void EnsureFolder(string assetPath)
        {
            string folder = Path.GetDirectoryName(assetPath);

            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }
        }

        private static MazeRoom BuildRoom(
            MazeLayout layout, MazeCell cell, Transform parent, GameObject prefab,
            TMP_FontAsset font, Sprite arrow, Sprite indicatorSprite, List<GameObject> fogs)
        {
            Vector2 center = layout.RoomCenter(cell);
            string roomName = $"Room_{cell.Column}_{cell.Row}";

            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.name = roomName;
                instance.transform.position = new Vector3(center.x, center.y, 0f);

                MazeRoom prefabRoom = instance.GetComponent<MazeRoom>();

                if (prefabRoom != null)
                {
                    SerializedObject roomSo = new SerializedObject(prefabRoom);
                    roomSo.FindProperty("cell").vector2IntValue = new Vector2Int(cell.Column, cell.Row);
                    roomSo.ApplyModifiedPropertiesWithoutUndo();

                    ShadowArea instanceFog = instance.GetComponentInChildren<ShadowArea>(true);

                    if (instanceFog != null)
                    {
                        fogs.Add(instanceFog.gameObject);
                    }

                    return prefabRoom;
                }

                Object.DestroyImmediate(instance);
            }

            GameObject go = new GameObject(roomName);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            MazeRoom room = go.AddComponent<MazeRoom>();

            ShadowArea fog = BuildFog(go.transform, "Fog", Vector2.one * layout.roomSize, fogs, null);
            MazeRoomGuide guide = BuildGuide(go.transform, font, arrow);
            MazeRoomTerminal terminal = BuildTerminal(go.transform, indicatorSprite);
            room.Bind(new Vector2Int(cell.Column, cell.Row), fog, guide, terminal);
            return room;
        }

        private static WorldYSort AddYSort(GameObject owner, Transform sortPoint, SpriteRenderer target)
        {
            WorldYSort ySort = owner.AddComponent<WorldYSort>();
            SerializedObject so = new SerializedObject(ySort);
            so.FindProperty("sortPoint").objectReferenceValue = sortPoint;
            so.FindProperty("targetRenderer").objectReferenceValue = target;
            so.FindProperty("updateContinuously").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            return ySort;
        }

        private static MazeRoomTerminal BuildTerminal(Transform parent, Sprite indicatorSprite)
        {
            GameObject go = new GameObject("Terminal");
            go.transform.SetParent(parent, false);
            MazeRoomTerminal terminal = go.AddComponent<MazeRoomTerminal>();
            NPCInteractionIndicatorController indicator = go.AddComponent<NPCInteractionIndicatorController>();

            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(go.transform, false);
            SpriteRenderer visual = visualObject.AddComponent<SpriteRenderer>();

            GameObject indicatorObject = new GameObject("InteractionIndicator");
            indicatorObject.transform.SetParent(go.transform, false);
            SpriteRenderer indicatorRenderer = indicatorObject.AddComponent<SpriteRenderer>();
            indicatorRenderer.sprite = indicatorSprite;
            indicatorRenderer.enabled = false;

            GameObject zoneObject = new GameObject("InteractionZone");
            zoneObject.transform.SetParent(go.transform, false);
            BoxCollider2D zoneCollider = zoneObject.AddComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
            zoneCollider.size = Vector2.one * 4f;
            NPCInteractionZone zone = zoneObject.AddComponent<NPCInteractionZone>();

            SerializedObject indicatorSo = new SerializedObject(indicator);
            indicatorSo.FindProperty("indicatorObject").objectReferenceValue = indicatorObject;
            indicatorSo.FindProperty("indicatorRenderer").objectReferenceValue = indicatorRenderer;
            indicatorSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject zoneSo = new SerializedObject(zone);
            zoneSo.FindProperty("indicatorController").objectReferenceValue = indicator;
            zoneSo.ApplyModifiedPropertiesWithoutUndo();

            Transform terminalSortPoint = new GameObject("SortPoint").transform;
            terminalSortPoint.SetParent(go.transform, false);
            WorldYSort terminalSort = AddYSort(go, terminalSortPoint, visual);

            terminal.Bind(visual, zone, indicator, indicatorRenderer, terminalSort, terminalSortPoint);
            return terminal;
        }

        private static MazeConnector BuildConnector(
            MazeLayout layout, MazeEdge edge, Transform parent, GameObject plugPrefab, GameObject shadowPrefab,
            List<GameObject> fogs)
        {
            Vector2 center = layout.ConnectorCenter(edge);
            GameObject go = new GameObject($"Conn_{edge.From.Column}_{edge.From.Row}_{edge.Direction}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            MazeConnector connector = go.AddComponent<MazeConnector>();

            MazeDirection towardsTo = edge.Direction;
            MazePlug plugFrom = BuildPlug(layout, go.transform, "PlugFrom", layout.PlugCenter(edge.From, towardsTo), plugPrefab);
            MazePlug plugTo = BuildPlug(layout, go.transform, "PlugTo", layout.PlugCenter(edge.To, towardsTo.Opposite()), plugPrefab);
            ShadowArea fog = BuildFog(go.transform, "Fog", layout.ConnectorSize(edge), fogs, shadowPrefab);

            connector.Bind(new Vector2Int(edge.From.Column, edge.From.Row), edge.Direction, plugFrom, plugTo, fog);
            return connector;
        }

        /// <summary>
        /// Creates (once) the Plug prefab: a 2x2 blocker built like an ObjectDecoration, with its
        /// Visual, SortPoint and Collider as children. Every plug in the level is an instance of it,
        /// so editing the prefab (sprite, collider, sort point) changes all of them at once.
        /// </summary>
        private static GameObject EnsurePlugPrefab(MazeLayout layout)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlugPrefabPath);

            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(PlugPrefabPath);

            float side = layout.plugSize;
            GameObject root = new GameObject("Plug");

            GameObject visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(root.transform, false);

            GameObject square = new GameObject("Square");
            square.transform.SetParent(visualRoot.transform, false);
            SpriteRenderer renderer = square.AddComponent<SpriteRenderer>();
            renderer.sortingLayerID = SortingLayer.NameToID("WorldDynamic");

            GameObject sortPoint = new GameObject("SortPoint");
            sortPoint.transform.SetParent(root.transform, false);
            sortPoint.transform.localPosition = new Vector3(0f, -side * 0.5f - PlugSortPointDrop, 0f);

            GameObject colliderObject = new GameObject("Collider");
            colliderObject.transform.SetParent(root.transform, false);
            BoxCollider2D collider = colliderObject.AddComponent<BoxCollider2D>();

            WorldYSort ySort = AddYSort(root, sortPoint.transform, renderer);
            MazePlug plug = root.AddComponent<MazePlug>();
            plug.Bind(collider, renderer, Vector2.one * side, ySort);

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, PlugPrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[Maze] Plug prefab created at {PlugPrefabPath}.");
            return asset;
        }

        private static MazePlug BuildPlug(
            MazeLayout layout, Transform parent, string name, Vector2 worldCenter, GameObject prefab)
        {
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.name = name;
                instance.transform.position = new Vector3(worldCenter.x, worldCenter.y, 0f);

                MazePlug prefabPlug = instance.GetComponent<MazePlug>();

                if (prefabPlug != null)
                {
                    return prefabPlug;
                }

                Object.DestroyImmediate(instance);
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(worldCenter.x, worldCenter.y, 0f);

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            MazePlug plug = go.AddComponent<MazePlug>();

            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(go.transform, false);
            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();

            Transform sortPoint = new GameObject("SortPoint").transform;
            sortPoint.SetParent(go.transform, false);
            sortPoint.localPosition = new Vector3(0f, -layout.plugSize * 0.5f - PlugSortPointDrop, 0f);
            WorldYSort ySort = AddYSort(go, sortPoint, renderer);

            plug.Bind(collider, renderer, Vector2.one * layout.plugSize, ySort);
            return plug;
        }

        /// <summary>
        /// One piece of fog: a ShadowAreaY instance resized to the room or the path it covers. Its
        /// SortPoint sits on its lower edge, so a WorldYSort sorts it against the player by Y.
        /// The reveal zone stays switched off: the maze reveals the fog from code.
        /// </summary>
        private static ShadowArea BuildFog(
            Transform parent, string name, Vector2 size, List<GameObject> fogs, GameObject prefab)
        {
            GameObject go;

            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.name = name;
                go.transform.localPosition = Vector3.zero;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Vector3.zero;
                go.AddComponent<ShadowArea>();
            }

            ShadowArea fog = go.GetComponent<ShadowArea>();

            SerializedObject so = new SerializedObject(fog);
            so.FindProperty("coverSize").vector2Value = size;
            so.FindProperty("coverOffset").vector2Value = Vector2.zero;
            so.FindProperty("autoRevealByPlayerZone").boolValue = false;
            so.FindProperty("findLocalPlayerAutomatically").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            BoxCollider2D zone = go.GetComponent<BoxCollider2D>();

            if (zone != null)
            {
                zone.isTrigger = true;
                zone.size = size;
                zone.offset = Vector2.zero;
                zone.enabled = false;
            }

            Transform sortPoint = go.transform.Find("SortPoint");

            if (sortPoint != null)
            {
                sortPoint.localPosition = new Vector3(0f, -size.y * 0.5f, 0f);
            }

            fogs.Add(go);
            return fog;
        }

        private static MazeRoomGuide BuildGuide(Transform parent, TMP_FontAsset font, Sprite arrow)
        {
            GameObject go = new GameObject("Guide");
            go.transform.SetParent(parent, false);
            MazeRoomGuide guide = go.AddComponent<MazeRoomGuide>();

            GameObject bubbleObject = new GameObject("QuestionBubble");
            bubbleObject.transform.SetParent(go.transform, false);
            SpriteRenderer bubble = bubbleObject.AddComponent<SpriteRenderer>();
            bubble.color = Color.black;
            bubble.enabled = false;

            TMP_Text question = BuildText(go.transform, "Question", font, new Vector2(5.6f, 2.2f));
            TMP_Text[] answers = new TMP_Text[4];
            SpriteRenderer[] arrows = new SpriteRenderer[4];

            foreach (MazeDirection direction in MazeDirectionExtensions.All)
            {
                int index = (int)direction;
                answers[index] = BuildText(go.transform, $"Answer_{direction}", font, new Vector2(5.5f, 1.2f));

                GameObject arrowObject = new GameObject($"Arrow_{direction}");
                arrowObject.transform.SetParent(go.transform, false);
                SpriteRenderer renderer = arrowObject.AddComponent<SpriteRenderer>();
                renderer.sprite = arrow;
                renderer.enabled = false;
                arrows[index] = renderer;
            }

            guide.Bind(question, answers, arrows, bubble);
            return guide;
        }

        private static TMP_Text BuildText(Transform parent, string name, TMP_FontAsset font, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshPro text = go.AddComponent<TextMeshPro>();

            if (font != null)
            {
                text.font = font;
            }

            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = size;
            text.text = string.Empty;
            text.enabled = false;
            return text;
        }

        [MenuItem("Tools/Cenigraf/Maze/Assign Question Catalog to Generator")]
        public static void AssignQuestionCatalog()
        {
            MazeLevelGenerator generator = Object.FindFirstObjectByType<MazeLevelGenerator>();
            MazeQuestionCatalog catalog = FindQuestionCatalog();

            if (generator == null || catalog == null)
            {
                Debug.LogWarning("[Maze] Generator or question catalog not found.");
                return;
            }

            Undo.RecordObject(generator, "Assign Question Catalog");
            generator.SetQuestionCatalog(catalog);
            EditorUtility.SetDirty(generator);
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            Debug.Log($"[Maze] Question catalog assigned: {catalog.name} ({catalog.Count} questions). Save the scene.");
        }

        [MenuItem("Tools/Cenigraf/Maze/Assign Exit Dialogue and Reward to Generator")]
        public static void AssignExitContentMenu()
        {
            MazeLevelGenerator generator = Object.FindFirstObjectByType<MazeLevelGenerator>();

            if (generator == null)
            {
                Debug.LogWarning("[Maze] Generator not found.");
                return;
            }

            SerializedObject so = new SerializedObject(generator);
            AssignExitContent(so);
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            Debug.Log("[Maze] Exit dialogue and reward assigned. Save the scene.");
        }

        /// <summary>Fills the dialogue and the reward of the exit teleport, by asset name.</summary>
        private static void AssignExitContent(SerializedObject generator)
        {
            AssignIfFound<DialogueCatalog>(generator, "exitDialogueCatalog", "DialogueCatalog");
            AssignIfFound<RewardDefinition>(generator, "exitReward", "Reward_GemaOrientacion");
            AssignIfFound<ItemCatalog>(generator, "exitItemCatalog", null);
            AssignIfFound<ChestLootTable>(generator, "chestLootTable", null);

            SerializedProperty chest = generator.FindProperty("chestPrefab");

            if (chest != null && chest.objectReferenceValue == null)
            {
                chest.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_SocialProyectoCenigraf/Prefabs/InScene/Loot/Chest.prefab");
            }
        }

        private static void AssignIfFound<T>(SerializedObject target, string propertyName, string assetName)
            where T : Object
        {
            SerializedProperty property = target.FindProperty(propertyName);

            if (property == null || property.objectReferenceValue != null)
            {
                return;
            }

            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (assetName != null && Path.GetFileNameWithoutExtension(path) != assetName)
                {
                    continue;
                }

                T asset = AssetDatabase.LoadAssetAtPath<T>(path);

                if (asset != null)
                {
                    property.objectReferenceValue = asset;
                    return;
                }
            }
        }

        private static MazeQuestionCatalog FindQuestionCatalog()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:MazeQuestionCatalog"))
            {
                MazeQuestionCatalog catalog =
                    AssetDatabase.LoadAssetAtPath<MazeQuestionCatalog>(AssetDatabase.GUIDToAssetPath(guid));

                if (catalog != null && catalog.Count > 0)
                {
                    return catalog;
                }
            }

            return null;
        }

        private static GameObject FindTeleportPrefab()
        {
            foreach (string guid in AssetDatabase.FindAssets("Teleport t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetFileNameWithoutExtension(path).TrimEnd('_') == "Teleport")
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }

            return null;
        }

        private static T LoadByGuid<T>(string guid) where T : Object
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static Sprite LoadNamedSprite(string guid, string spriteName)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            Sprite first = null;

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    first = first != null ? first : sprite;

                    if (sprite.name == spriteName)
                    {
                        return sprite;
                    }
                }
            }

            return first;
        }

        private static Sprite LoadFirstSprite(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }

            return null;
        }
    }
}
