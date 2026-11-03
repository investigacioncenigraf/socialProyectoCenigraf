#if UNITY_EDITOR
using System;
using System.Linq;
using SocialProyectoCenigraf.Dialogue;
using SocialProyectoCenigraf.Player.Visual;
using SocialProyectoCenigraf.World.NPC;
using SocialProyectoCenigraf.World.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SocialProyectoCenigraf.Editor.Player
{
    public static class NPCSkinSetupEditor
    {
        private const string NPCPrefabPath =
            "Assets/_SocialProyectoCenigraf/Prefabs/NPCs/NPCs.prefab";
        private const string DemoDefinitionPath =
            "Assets/_SocialProyectoCenigraf/Data/Player/Skins/" +
            "Demo_PlayerSkinDefinition.asset";
        private const string CatalogPath =
            "Assets/_SocialProyectoCenigraf/Data/Player/Skins/" +
            "PlayerSkinCatalog.asset";
        private const string LayerOrderProfilePath =
            "Assets/_SocialProyectoCenigraf/Data/Player/Skins/" +
            "Default_PlayerLayerOrderProfile.asset";
        private const string InteractionIconPath =
            "Assets/_SocialProyectoCenigraf/Asset/WorldScene/Colections/" +
            "teclaE.png";
        private const string DialogueCatalogPath =
            "Assets/_SocialProyectoCenigraf/Asset/Dialogue/" +
            "DialogueCatalog_Example.asset";
        private const string DefaultDialogueId =
            "recepcion_lobby_entrega_carnet";

        private static readonly LayerSetup[] Layers =
        {
            new("Shadow", "shadowRenderer", PlayerSkinLayer.Shadow),
            new("LeftLeg", "leftLegRenderer", PlayerSkinLayer.LeftLeg),
            new("RightLeg", "rightLegRenderer", PlayerSkinLayer.RightLeg),
            new("Body", "bodyRenderer", PlayerSkinLayer.Body),
            new(
                "BodyAccessory",
                "bodyAccessoryRenderer",
                PlayerSkinLayer.BodyAccessory),
            new("LeftHand", "leftHandRenderer", PlayerSkinLayer.LeftHand),
            new("RightHand", "rightHandRenderer", PlayerSkinLayer.RightHand),
            new("Head", "headRenderer", PlayerSkinLayer.Head)
        };

        [MenuItem("Tools/Cenigraf/NPC/Configure Layered Animated Skin")]
        public static void ConfigureNPCPrefab()
        {
            PlayerSkinDefinition definition =
                AssetDatabase.LoadAssetAtPath<PlayerSkinDefinition>(
                    DemoDefinitionPath);
            PlayerSkinCatalog catalog =
                AssetDatabase.LoadAssetAtPath<PlayerSkinCatalog>(CatalogPath);
            PlayerLayerOrderProfile layerOrderProfile =
                AssetDatabase.LoadAssetAtPath<PlayerLayerOrderProfile>(
                    LayerOrderProfilePath);

            if (definition == null || catalog == null || layerOrderProfile == null)
            {
                throw new InvalidOperationException(
                    "The Demo skin definition, catalog and layer order profile " +
                    "must exist before configuring the NPC prefab.");
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(NPCPrefabPath);

            try
            {
                ConfigureNPC(
                    prefabRoot,
                    definition,
                    catalog,
                    layerOrderProfile);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, NPCPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "NPC prefab configured with the same layered Demo animation " +
                "system used by the player.");
        }

        private static void ConfigureNPC(
            GameObject prefabRoot,
            PlayerSkinDefinition definition,
            PlayerSkinCatalog catalog,
            PlayerLayerOrderProfile layerOrderProfile)
        {
            Rigidbody2D rigidbody =
                prefabRoot.GetComponentInChildren<Rigidbody2D>(true);

            if (rigidbody == null)
            {
                throw new InvalidOperationException(
                    "The NPC prefab does not contain an NPC with a Rigidbody2D.");
            }

            GameObject npc = rigidbody.gameObject;
            ConfigureAnchoredPhysics(rigidbody);
            Transform visual = npc.transform.Find("Visual") ??
                CreateChild(npc.transform, "Visual");
            SpriteRenderer legacyRenderer =
                visual.GetComponentsInChildren<SpriteRenderer>(true)
                    .FirstOrDefault(renderer =>
                        renderer.transform.name == "Skin");
            Material sharedMaterial = legacyRenderer != null
                ? legacyRenderer.sharedMaterial
                : null;
            int sortingLayerId = legacyRenderer != null
                ? legacyRenderer.sortingLayerID
                : 0;

            Transform layeredSkin = visual.Find("LayeredSkin") ??
                CreateChild(visual, "LayeredSkin");
            layeredSkin.localPosition = Vector3.zero;
            layeredSkin.localRotation = Quaternion.identity;
            layeredSkin.localScale = Vector3.one;

            SortingGroup sortingGroup =
                GetOrAddComponent<SortingGroup>(layeredSkin.gameObject);
            sortingGroup.sortingLayerID = sortingLayerId;

            NPCLayeredSkinController controller =
                GetOrAddComponent<NPCLayeredSkinController>(npc);
            NPCLayerColorController colorController =
                GetOrAddComponent<NPCLayerColorController>(npc);
            NPCPlayerTrackingController trackingController =
                GetOrAddComponent<NPCPlayerTrackingController>(npc);
            NPCInteractionIndicatorController indicatorController =
                GetOrAddComponent<NPCInteractionIndicatorController>(npc);
            NPCDialogueInteractionController dialogueController =
                GetOrAddComponent<NPCDialogueInteractionController>(npc);
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("skinCatalog").objectReferenceValue =
                catalog;
            serializedController.FindProperty("layerOrderProfile")
                .objectReferenceValue = layerOrderProfile;
            serializedController.FindProperty("skinId").stringValue = "Demo";
            serializedController.FindProperty("skinRoot").objectReferenceValue =
                layeredSkin;
            serializedController.FindProperty("layerColorController")
                .objectReferenceValue = colorController;
            serializedController.FindProperty("animationType").enumValueIndex =
                (int)PlayerAnimationType.IdleFront;
            serializedController.FindProperty("playAnimation").boolValue = true;
            serializedController.FindProperty("frameDurationMilliseconds")
                .intValue = 300;
            serializedController.FindProperty("framesPerAnimation").intValue = 4;
            serializedController.FindProperty("startAtFrameOne").boolValue = true;

            foreach (LayerSetup layerSetup in Layers)
            {
                Transform layer = layeredSkin.Find(layerSetup.Name) ??
                    CreateChild(layeredSkin, layerSetup.Name);
                SpriteRenderer renderer =
                    GetOrAddComponent<SpriteRenderer>(layer.gameObject);
                renderer.sharedMaterial = sharedMaterial;
                renderer.sprite = definition.GetSprite(
                    layerSetup.Layer,
                    PlayerAnimationType.IdleFront,
                    1);
                renderer.sortingOrder = layerOrderProfile.GetSortingOrder(
                    PlayerAnimationType.IdleFront,
                    layerSetup.Layer);
                serializedController.FindProperty(layerSetup.PropertyName)
                    .objectReferenceValue = renderer;
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            SerializedObject serializedColors = new(colorController);

            foreach (LayerSetup layerSetup in Layers)
            {
                SpriteRenderer renderer = layeredSkin
                    .Find(layerSetup.Name)
                    .GetComponent<SpriteRenderer>();
                serializedColors.FindProperty(layerSetup.ColorPropertyName)
                    .objectReferenceValue = renderer;
            }

            serializedColors.ApplyModifiedPropertiesWithoutUndo();
            colorController.ApplyColors();
            EditorUtility.SetDirty(colorController);

            SerializedObject serializedTracking = new(trackingController);
            serializedTracking.FindProperty("trackingEnabled").boolValue = true;
            serializedTracking.FindProperty("requiresWarningZone").boolValue =
                true;
            serializedTracking.FindProperty("findPlayerAutomatically").boolValue =
                true;
            serializedTracking.FindProperty("horizontalDeadZone").floatValue =
                0.05f;
            serializedTracking.FindProperty("skinController")
                .objectReferenceValue = controller;
            serializedTracking.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trackingController);

            ConfigureIndicator(
                npc.transform,
                indicatorController,
                sharedMaterial,
                sortingLayerId);
            ConfigureZones(
                npc.transform,
                trackingController,
                indicatorController,
                dialogueController);

            if (legacyRenderer != null)
            {
                legacyRenderer.gameObject.SetActive(false);
            }

            WorldYSort worldYSort = npc.GetComponent<WorldYSort>();

            if (worldYSort != null)
            {
                SerializedObject serializedSort = new(worldYSort);
                serializedSort.FindProperty("targetSortingGroup")
                    .objectReferenceValue = sortingGroup;
                serializedSort.FindProperty("targetRenderer")
                    .objectReferenceValue = null;
                serializedSort.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(worldYSort);
            }
        }

        private static void ConfigureAnchoredPhysics(Rigidbody2D rigidbody)
        {
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.simulated = true;
            rigidbody.useFullKinematicContacts = false;
            rigidbody.gravityScale = 0f;
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;
            EditorUtility.SetDirty(rigidbody);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void ConfigureZones(
            Transform npc,
            NPCPlayerTrackingController trackingController,
            NPCInteractionIndicatorController indicatorController,
            NPCDialogueInteractionController dialogueController)
        {
            Transform interactionZone = npc.Find("InteractionZone") ??
                CreateChild(npc, "InteractionZone");
            interactionZone.localPosition = Vector3.zero;
            interactionZone.localRotation = Quaternion.identity;
            interactionZone.localScale = Vector3.one;
            BoxCollider2D interactionCollider =
                GetOrAddComponent<BoxCollider2D>(interactionZone.gameObject);
            interactionCollider.isTrigger = true;
            interactionCollider.size = new Vector2(2.5f, 2.5f);
            RemoveComponent<CircleCollider2D>(interactionZone.gameObject);
            NPCInteractionZone interactionController =
                GetOrAddComponent<NPCInteractionZone>(interactionZone.gameObject);
            SerializedObject serializedInteraction = new(interactionController);
            serializedInteraction.FindProperty("indicatorController")
                .objectReferenceValue = indicatorController;
            serializedInteraction.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interactionController);

            DialogueCatalog dialogueCatalog =
                AssetDatabase.LoadAssetAtPath<DialogueCatalog>(
                    DialogueCatalogPath);
            SerializedObject serializedDialogue = new(dialogueController);
            serializedDialogue.FindProperty("dialogueCatalog")
                .objectReferenceValue = dialogueCatalog;
            serializedDialogue.FindProperty("dialogueId").stringValue =
                DefaultDialogueId;
            serializedDialogue.FindProperty("interactionZone")
                .objectReferenceValue = interactionController;
            serializedDialogue.FindProperty("indicatorController")
                .objectReferenceValue = indicatorController;
            serializedDialogue.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dialogueController);

            Transform warningZone = npc.Find("WarningZone") ??
                CreateChild(npc, "WarningZone");
            warningZone.localPosition = Vector3.zero;
            warningZone.localRotation = Quaternion.identity;
            warningZone.localScale = Vector3.one;
            BoxCollider2D warningCollider =
                GetOrAddComponent<BoxCollider2D>(warningZone.gameObject);
            warningCollider.isTrigger = true;
            warningCollider.size = new Vector2(5f, 5f);
            RemoveComponent<CircleCollider2D>(warningZone.gameObject);
            NPCWarningZone warningController =
                GetOrAddComponent<NPCWarningZone>(warningZone.gameObject);
            SerializedObject serializedWarning = new(warningController);
            serializedWarning.FindProperty("trackingController")
                .objectReferenceValue = trackingController;
            serializedWarning.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(warningController);
        }

        private static void ConfigureIndicator(
            Transform npc,
            NPCInteractionIndicatorController indicatorController,
            Material sharedMaterial,
            int sortingLayerId)
        {
            Transform indicator = npc.Find("InteractionIndicator") ??
                CreateChild(npc, "InteractionIndicator");
            indicator.localPosition = new Vector3(0f, 1.25f, 0f);
            indicator.localRotation = Quaternion.identity;
            indicator.localScale = Vector3.one;

            SpriteRenderer indicatorRenderer =
                GetOrAddComponent<SpriteRenderer>(indicator.gameObject);
            indicatorRenderer.sprite = AssetDatabase
                .LoadAllAssetsAtPath(InteractionIconPath)
                .OfType<Sprite>()
                .FirstOrDefault();
            indicatorRenderer.sharedMaterial = sharedMaterial;
            indicatorRenderer.sortingLayerID = sortingLayerId;
            indicatorRenderer.sortingOrder = 32000;
            indicatorRenderer.enabled = false;

            SerializedObject serializedIndicator = new(indicatorController);
            serializedIndicator.FindProperty("indicatorObject")
                .objectReferenceValue = indicator.gameObject;
            serializedIndicator.FindProperty("indicatorRenderer")
                .objectReferenceValue = indicatorRenderer;
            serializedIndicator.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(indicatorController);
        }

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            return target.TryGetComponent(out T component)
                ? component
                : target.AddComponent<T>();
        }

        private static void RemoveComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();

            if (component != null)
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private readonly struct LayerSetup
        {
            public LayerSetup(
                string name,
                string propertyName,
                PlayerSkinLayer layer,
                string colorPropertyName = null)
            {
                Name = name;
                PropertyName = propertyName;
                Layer = layer;
                ColorPropertyName = colorPropertyName ?? propertyName;
            }

            public string Name { get; }
            public string PropertyName { get; }
            public PlayerSkinLayer Layer { get; }
            public string ColorPropertyName { get; }
        }
    }
}
#endif
