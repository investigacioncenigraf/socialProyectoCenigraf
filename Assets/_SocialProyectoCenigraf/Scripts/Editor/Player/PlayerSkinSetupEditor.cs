#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using SocialProyectoCenigraf.Player.State;
using SocialProyectoCenigraf.Player.Visual;
using SocialProyectoCenigraf.World.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SocialProyectoCenigraf.Editor.Player
{
    public static class PlayerSkinSetupEditor
    {
        private const string DemoSkinId = "Demo";
        private const string DemoSpritesDirectory =
            "Assets/_SocialProyectoCenigraf/Asset/Image/Entities/Player/Skin/Demo";
        private const string PlayerDataDirectory =
            "Assets/_SocialProyectoCenigraf/Data/Player";
        private const string SkinDataDirectory = PlayerDataDirectory + "/Skins";
        private const string DemoDefinitionPath =
            SkinDataDirectory + "/Demo_PlayerSkinDefinition.asset";
        private const string CatalogPath =
            SkinDataDirectory + "/PlayerSkinCatalog.asset";
        private const string LayerOrderProfilePath =
            SkinDataDirectory + "/Default_PlayerLayerOrderProfile.asset";

        private static readonly LayerSetup[] LayerSetups =
        {
            new("Shadow", "shadowRenderer", 0),
            new("LeftLeg", "leftLegRenderer", 10),
            new("RightLeg", "rightLegRenderer", 20),
            new("Body", "bodyRenderer", 30),
            new("BodyAccessory", "bodyAccessoryRenderer", 40),
            new("LeftHand", "leftHandRenderer", 40),
            new("RightHand", "rightHandRenderer", 50),
            new("Head", "headRenderer", 60)
        };

        [MenuItem("Tools/Cenigraf/Player/Configure Demo Layered Skin")]
        public static void ConfigureDemoLayeredSkin()
        {
            EnsureFolder(PlayerDataDirectory);
            EnsureFolder(SkinDataDirectory);

            PlayerSkinDefinition definition = CreateOrUpdateDefinition();
            PlayerSkinCatalog catalog = CreateOrUpdateCatalog(definition);
            PlayerLayerOrderProfile layerOrderProfile =
                CreateOrLoadLayerOrderProfile();
            ConfigurePlayerInActiveScene(
                catalog,
                definition,
                layerOrderProfile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Demo layered player skin configured: Redux SkinId 'Demo', " +
                "Idle/Walk Front/Back, four frames and eight synchronized layers.");
        }

        private static PlayerSkinDefinition CreateOrUpdateDefinition()
        {
            PlayerSkinDefinition definition =
                AssetDatabase.LoadAssetAtPath<PlayerSkinDefinition>(
                    DemoDefinitionPath);

            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PlayerSkinDefinition>();
                AssetDatabase.CreateAsset(definition, DemoDefinitionPath);
            }

            SerializedObject serializedDefinition = new(definition);
            serializedDefinition.FindProperty("skinId").stringValue = DemoSkinId;

            SetSpriteArray(
                serializedDefinition.FindProperty("shadowFrames"),
                LoadFrames("Demo_ShadowAnimation.png"));
            SetSpriteArray(
                serializedDefinition.FindProperty("leftLegFrames"),
                LoadFrames("Demo_LeftLegAnimation.png"));
            SetSpriteArray(
                serializedDefinition.FindProperty("rightLegFrames"),
                LoadFrames("Demo_RightLegAnimation.png"));
            SetSpriteArray(
                serializedDefinition.FindProperty("bodyFrames"),
                LoadFrames("Demo_BodyAnimation.png"));
            SetSpriteArray(
                serializedDefinition.FindProperty("bodyAccessoryFrames"),
                LoadFrames("Demo_AccesoriesBodyAnimation 1.png"));
            SetSpriteArray(
                serializedDefinition.FindProperty("leftHandFrames"),
                LoadFrames("Demo_LeftHandAnimation.png"));
            SetSpriteArray(
                serializedDefinition.FindProperty("rightHandFrames"),
                LoadFrames("Demo_RightHandAnimation.png"));
            SetSpriteArray(
                serializedDefinition.FindProperty("headFrames"),
                LoadFrames("Demo_HeadAnimation.png"));

            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static PlayerSkinCatalog CreateOrUpdateCatalog(
            PlayerSkinDefinition definition)
        {
            PlayerSkinCatalog catalog =
                AssetDatabase.LoadAssetAtPath<PlayerSkinCatalog>(CatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PlayerSkinCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject serializedCatalog = new(catalog);
            SerializedProperty skins = serializedCatalog.FindProperty("skins");

            bool containsDefinition = false;

            for (int index = 0; index < skins.arraySize; index++)
            {
                if (skins.GetArrayElementAtIndex(index).objectReferenceValue ==
                    definition)
                {
                    containsDefinition = true;
                    break;
                }
            }

            if (!containsDefinition)
            {
                int newIndex = skins.arraySize;
                skins.InsertArrayElementAtIndex(newIndex);
                skins.GetArrayElementAtIndex(newIndex).objectReferenceValue =
                    definition;
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static PlayerLayerOrderProfile CreateOrLoadLayerOrderProfile()
        {
            PlayerLayerOrderProfile profile =
                AssetDatabase.LoadAssetAtPath<PlayerLayerOrderProfile>(
                    LayerOrderProfilePath);

            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<PlayerLayerOrderProfile>();
            AssetDatabase.CreateAsset(profile, LayerOrderProfilePath);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigurePlayerInActiveScene(
            PlayerSkinCatalog catalog,
            PlayerSkinDefinition definition,
            PlayerLayerOrderProfile layerOrderProfile)
        {
            PlayerStateStore store = UnityEngine.Object.FindFirstObjectByType<
                PlayerStateStore>(FindObjectsInactive.Include);

            if (store == null)
            {
                throw new InvalidOperationException(
                    "The active scene does not contain a PlayerStateStore.");
            }

            GameObject player = store.gameObject;
            Transform visual = player.transform.Find("Visual");

            if (visual == null)
            {
                throw new InvalidOperationException(
                    "The Player requires a child GameObject named 'Visual'.");
            }

            SpriteRenderer previousRenderer =
                visual.GetComponentInChildren<SpriteRenderer>(true);
            Material playerMaterial = previousRenderer != null
                ? previousRenderer.sharedMaterial
                : null;
            int sortingLayerId = previousRenderer != null
                ? previousRenderer.sortingLayerID
                : 0;

            Transform layeredSkin = FindOrCreateChild(visual, "LayeredSkin");
            layeredSkin.localPosition = Vector3.zero;
            layeredSkin.localRotation = Quaternion.identity;
            layeredSkin.localScale = Vector3.one;

            SortingGroup sortingGroup =
                GetOrAddComponent<SortingGroup>(layeredSkin.gameObject);
            sortingGroup.sortingLayerID = sortingLayerId;

            PlayerLayeredSkinController controller =
                GetOrAddComponent<PlayerLayeredSkinController>(player);
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("skinCatalog").objectReferenceValue =
                catalog;
            serializedController.FindProperty("layerOrderProfile")
                .objectReferenceValue = layerOrderProfile;
            serializedController.FindProperty("skinRoot").objectReferenceValue =
                layeredSkin;

            foreach (LayerSetup setup in LayerSetups)
            {
                Transform layer = FindOrCreateChild(layeredSkin, setup.Name);
                layer.localPosition = Vector3.zero;
                layer.localRotation = Quaternion.identity;
                layer.localScale = Vector3.one;

                SpriteRenderer renderer =
                    GetOrAddComponent<SpriteRenderer>(layer.gameObject);
                renderer.sharedMaterial = playerMaterial;
                renderer.sortingOrder = setup.SortingOrder;
                renderer.sprite = definition.GetSprite(
                    ParseLayer(setup.Name),
                    PlayerAnimationType.IdleFront,
                    0);

                serializedController.FindProperty(setup.ControllerProperty)
                    .objectReferenceValue = renderer;
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedStore = new(store);
            SerializedProperty serializedState =
                serializedStore.FindProperty("state");
            serializedState.FindPropertyRelative("skinId").stringValue = DemoSkinId;
            serializedState.FindPropertyRelative("facingDirection").enumValueIndex =
                (int)PlayerFacingDirection.DownRight;
            serializedState.FindPropertyRelative(
                    "animationFrameDurationMilliseconds")
                .intValue =
                PlayerStateData.DefaultAnimationFrameDurationMilliseconds;
            serializedState.FindPropertyRelative("framesPerAnimation").intValue =
                PlayerStateData.DefaultFramesPerAnimation;
            serializedStore.ApplyModifiedPropertiesWithoutUndo();

            if (previousRenderer != null &&
                !previousRenderer.transform.IsChildOf(layeredSkin))
            {
                previousRenderer.gameObject.SetActive(false);
            }

            WorldYSort worldYSort = player.GetComponent<WorldYSort>();
            SerializedObject serializedYSort = new(worldYSort);
            serializedYSort.FindProperty("targetSortingGroup")
                .objectReferenceValue = sortingGroup;
            serializedYSort.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(player);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Selection.activeGameObject = player;
        }

        private static Sprite[] LoadFrames(string fileName)
        {
            string assetPath = $"{DemoSpritesDirectory}/{fileName}";
            ConfigureTextureImporter(assetPath);

            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => ParseFrameIndex(sprite.name))
                .ToArray();

            if (frames.Length != 16)
            {
                throw new InvalidOperationException(
                    $"'{assetPath}' must contain exactly 16 sprites, but " +
                    $"contains {frames.Length}.");
            }

            return frames;
        }

        private static void ConfigureTextureImporter(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                return;
            }

            bool changed = importer.filterMode != FilterMode.Point ||
                           importer.textureCompression !=
                               TextureImporterCompression.Uncompressed ||
                           importer.mipmapEnabled;

            if (!changed)
            {
                return;
            }

            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static void SetSpriteArray(
            SerializedProperty property,
            IReadOnlyList<Sprite> sprites)
        {
            property.arraySize = sprites.Count;

            for (int index = 0; index < sprites.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    sprites[index];
            }
        }

        private static int ParseFrameIndex(string spriteName)
        {
            int separator = spriteName.LastIndexOf('_');
            return separator >= 0 &&
                   int.TryParse(spriteName[(separator + 1)..], out int index)
                ? index
                : int.MaxValue;
        }

        private static PlayerSkinLayer ParseLayer(string layerName) =>
            Enum.Parse<PlayerSkinLayer>(layerName);

        private static Transform FindOrCreateChild(
            Transform parent,
            string childName)
        {
            Transform child = parent.Find(childName);

            if (child != null)
            {
                return child;
            }

            GameObject childObject = new(childName);
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : Undo.AddComponent<T>(target);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = folderPath[..folderPath.LastIndexOf('/')];
            string folderName = folderPath[(folderPath.LastIndexOf('/') + 1)..];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private readonly struct LayerSetup
        {
            public LayerSetup(
                string name,
                string controllerProperty,
                int sortingOrder)
            {
                Name = name;
                ControllerProperty = controllerProperty;
                SortingOrder = sortingOrder;
            }

            public string Name { get; }
            public string ControllerProperty { get; }
            public int SortingOrder { get; }
        }
    }
}
#endif
