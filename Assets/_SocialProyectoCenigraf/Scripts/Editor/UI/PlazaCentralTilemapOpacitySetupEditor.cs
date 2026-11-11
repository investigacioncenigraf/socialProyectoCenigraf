using SocialProyectoCenigraf.World.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SocialProyectoCenigraf.Editor.UI
{
    public static class PlazaCentralTilemapOpacitySetupEditor
    {
        private const string MenuPath =
            "Tools/Cenigraf/World/Configure Sala_ABC Tilemap Opacity";
        private const string TargetScenePath =
            "Assets/_SocialProyectoCenigraf/Scenes/Salas/Plaza_Central.unity";
        private const string TargetTilemapName = "Sala_ABC";
        private const string OpacityAreaName = "OpacityArea";
        private const string LegacyOpacityPointName = "OpacitySortPoint";

        [MenuItem(MenuPath)]
        public static void Configure()
        {
            Scene scene = SceneManager.GetActiveScene();

            if (scene.path != TargetScenePath)
            {
                scene = EditorSceneManager.OpenScene(
                    TargetScenePath,
                    OpenSceneMode.Single);
            }

            Tilemap targetTilemap = FindTargetTilemap();
            if (targetTilemap == null)
            {
                Debug.LogError(
                    $"No se encontro el Tilemap '{TargetTilemapName}' en " +
                    $"{TargetScenePath}.");
                return;
            }

            Transform opacityAreaTransform = targetTilemap.transform.Find(
                OpacityAreaName);

            if (opacityAreaTransform == null)
            {
                opacityAreaTransform = targetTilemap.transform.Find(
                    LegacyOpacityPointName);

                if (opacityAreaTransform != null)
                {
                    Undo.RecordObject(
                        opacityAreaTransform.gameObject,
                        "Convert opacity point to opacity area");
                    opacityAreaTransform.name = OpacityAreaName;
                }
                else
                {
                    GameObject areaObject = new(OpacityAreaName);
                    Undo.RegisterCreatedObjectUndo(
                        areaObject,
                        "Create tilemap opacity area");
                    opacityAreaTransform = areaObject.transform;
                    opacityAreaTransform.SetParent(
                        targetTilemap.transform,
                        false);
                }
            }

            opacityAreaTransform.localPosition = Vector3.zero;
            opacityAreaTransform.localRotation = Quaternion.identity;
            opacityAreaTransform.localScale = Vector3.one;

            BoxCollider2D opacityArea =
                opacityAreaTransform.GetComponent<BoxCollider2D>();

            if (opacityArea == null)
            {
                opacityArea = Undo.AddComponent<BoxCollider2D>(
                    opacityAreaTransform.gameObject);
            }

            TilemapRenderer tilemapRenderer =
                targetTilemap.GetComponent<TilemapRenderer>();
            Bounds visualBounds = tilemapRenderer != null
                ? tilemapRenderer.localBounds
                : targetTilemap.localBounds;

            // A Tilemap can occupy a single cell row while its sprites extend
            // several units vertically. Renderer bounds represent the actual
            // visual area and therefore produce a useful initial trigger.
            if (opacityArea.size.y <= 0.11f)
            {
                opacityArea.offset = visualBounds.center;
                opacityArea.size = new Vector2(
                    visualBounds.size.x,
                    visualBounds.size.y);
            }

            opacityArea.isTrigger = true;

            TilemapAreaOpacityController controller =
                targetTilemap.GetComponent<TilemapAreaOpacityController>();

            if (controller == null)
            {
                controller = Undo.AddComponent<TilemapAreaOpacityController>(
                    targetTilemap.gameObject);
            }

            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("targetTilemap")
                .objectReferenceValue = targetTilemap;
            serializedController.FindProperty("opacityArea")
                .objectReferenceValue = opacityArea;
            serializedController.FindProperty("findLocalPlayerAutomatically")
                .boolValue = true;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(opacityAreaTransform);
            EditorUtility.SetDirty(opacityArea);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = targetTilemap.gameObject;

            Debug.Log(
                "Sala_ABC configured with an editable Tilemap opacity area. " +
                "WorldYSort remains active.",
                targetTilemap);
        }

        private static Tilemap FindTargetTilemap()
        {
            Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap.gameObject.name == TargetTilemapName)
                {
                    return tilemap;
                }
            }

            return null;
        }
    }
}
