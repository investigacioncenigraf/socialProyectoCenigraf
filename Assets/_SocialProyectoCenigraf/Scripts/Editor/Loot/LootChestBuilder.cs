using System.IO;
using SocialProyectoCenigraf.Items;
using SocialProyectoCenigraf.World.Loot;
using SocialProyectoCenigraf.World.NPC;
using SocialProyectoCenigraf.World.Rendering;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SocialProyectoCenigraf.EditorTools.Loot
{
    /// <summary>Creates the chest prefab and the loot table that feeds it.</summary>
    public static class LootChestBuilder
    {
        private const string TablePath =
            "Assets/_SocialProyectoCenigraf/Data/Loot/ChestLootTable.asset";
        private const string PrefabPath =
            "Assets/_SocialProyectoCenigraf/Prefabs/InScene/Loot/Chest.prefab";
        private const string FontGuid = "64792600f61ce40d5bd80714a733aaa8";
        private const string IndicatorGuid = "532eee3a7a8cd5b429f8a96dec4347d5";

        [MenuItem("Tools/Cenigraf/Loot/Create Chest Prefab and Loot Table")]
        public static void Create()
        {
            ChestLootTable table = EnsureTable();
            GameObject prefab = EnsurePrefab(table);

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        private static ChestLootTable EnsureTable()
        {
            ChestLootTable existing = AssetDatabase.LoadAssetAtPath<ChestLootTable>(TablePath);

            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(TablePath);

            ChestLootTable table = ScriptableObject.CreateInstance<ChestLootTable>();
            SerializedObject so = new SerializedObject(table);
            so.FindProperty("catalog").objectReferenceValue = FindCatalog();

            SerializedProperty levels = so.FindProperty("levels");
            levels.arraySize = 10;

            // level, items (min,max), weights C/U/R/E, nuggets (min,max)
            int[,] rows =
            {
                { 0, 0, 0,   0,   0,  0,  0,   0   },
                { 1, 1, 100, 0,   0,  0,  1,   3   },
                { 1, 1, 100, 0,   0,  0,  2,   5   },
                { 1, 2, 100, 0,   0,  0,  3,   8   },
                { 1, 2, 95,  5,   0,  0,  5,   12  },
                { 2, 2, 90,  10,  0,  0,  8,   18  },
                { 2, 2, 70,  30,  0,  0,  12,  25  },
                { 2, 3, 45,  55,  0,  0,  20,  40  },
                { 3, 3, 0,   90,  10, 0,  40,  80  },
                { 5, 5, 0,   0,   80, 20, 125, 175 }
            };

            for (int level = 0; level < 10; level++)
            {
                SerializedProperty entry = levels.GetArrayElementAtIndex(level);
                entry.FindPropertyRelative("minItems").intValue = rows[level, 0];
                entry.FindPropertyRelative("maxItems").intValue = rows[level, 1];
                entry.FindPropertyRelative("commonWeight").intValue = rows[level, 2];
                entry.FindPropertyRelative("uncommonWeight").intValue = rows[level, 3];
                entry.FindPropertyRelative("rareWeight").intValue = rows[level, 4];
                entry.FindPropertyRelative("epicWeight").intValue = rows[level, 5];
                entry.FindPropertyRelative("minNuggets").intValue = rows[level, 6];
                entry.FindPropertyRelative("maxNuggets").intValue = rows[level, 7];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(table, TablePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Loot] Loot table created at {TablePath}.");
            return table;
        }

        private static GameObject EnsurePrefab(ChestLootTable table)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(PrefabPath);

            GameObject root = new GameObject("Chest");

            // Solid body, off for now: the player walks over the chest.
            BoxCollider2D body = root.AddComponent<BoxCollider2D>();
            body.size = Vector2.one;
            body.enabled = false;

            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(root.transform, false);
            SpriteRenderer visual = visualObject.AddComponent<SpriteRenderer>();
            visual.sortingLayerID = SortingLayer.NameToID("Foreground");

            GameObject sortPoint = new GameObject("SortPoint");
            sortPoint.transform.SetParent(root.transform, false);
            sortPoint.transform.localPosition = new Vector3(0f, -0.5f, 0f);

            WorldYSort ySort = root.AddComponent<WorldYSort>();
            SerializedObject sortSo = new SerializedObject(ySort);
            sortSo.FindProperty("sortPoint").objectReferenceValue = sortPoint.transform;
            sortSo.FindProperty("targetRenderer").objectReferenceValue = visual;
            sortSo.FindProperty("updateContinuously").boolValue = false;
            sortSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject indicatorObject = new GameObject("InteractionIndicator");
            indicatorObject.transform.SetParent(root.transform, false);
            indicatorObject.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            SpriteRenderer indicatorRenderer = indicatorObject.AddComponent<SpriteRenderer>();
            indicatorRenderer.sprite = LoadIndicatorSprite();
            indicatorRenderer.sortingLayerID = SortingLayer.NameToID("Foreground");
            indicatorRenderer.enabled = false;

            NPCInteractionIndicatorController indicator =
                root.AddComponent<NPCInteractionIndicatorController>();
            SerializedObject indicatorSo = new SerializedObject(indicator);
            indicatorSo.FindProperty("indicatorObject").objectReferenceValue = indicatorObject;
            indicatorSo.FindProperty("indicatorRenderer").objectReferenceValue = indicatorRenderer;
            indicatorSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject zoneObject = new GameObject("InteractionZone");
            zoneObject.transform.SetParent(root.transform, false);
            BoxCollider2D zoneCollider = zoneObject.AddComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
            zoneCollider.size = Vector2.one * 2.5f;
            NPCInteractionZone zone = zoneObject.AddComponent<NPCInteractionZone>();
            SerializedObject zoneSo = new SerializedObject(zone);
            zoneSo.FindProperty("indicatorController").objectReferenceValue = indicator;
            zoneSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject labelObject = new GameObject("LootLabel");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                AssetDatabase.GUIDToAssetPath(FontGuid));

            if (font != null)
            {
                label.font = font;
            }

            label.fontSize = 1.4f;
            label.alignment = TextAlignmentOptions.Bottom;
            label.rectTransform.sizeDelta = new Vector2(6f, 3f);
            label.text = string.Empty;
            label.enabled = false;

            Renderer labelRenderer = label.GetComponent<Renderer>();

            if (labelRenderer != null)
            {
                labelRenderer.sortingLayerID = SortingLayer.NameToID("Foreground");
                labelRenderer.sortingOrder = 20000;
            }

            LootChest chest = root.AddComponent<LootChest>();
            chest.Bind(visual, zone, indicator, label);
            chest.Configure(1, table);

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[Loot] Chest prefab created at {PrefabPath}.");
            return asset;
        }

        private static ItemCatalog FindCatalog()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ItemCatalog"))
            {
                ItemCatalog catalog =
                    AssetDatabase.LoadAssetAtPath<ItemCatalog>(AssetDatabase.GUIDToAssetPath(guid));

                if (catalog != null)
                {
                    return catalog;
                }
            }

            return null;
        }

        private static Sprite LoadIndicatorSprite()
        {
            string path = AssetDatabase.GUIDToAssetPath(IndicatorGuid);

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }

            return null;
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
    }
}
