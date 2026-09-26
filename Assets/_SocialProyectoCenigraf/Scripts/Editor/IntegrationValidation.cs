using System;
using System.Linq;
using SocialProyectoCenigraf.Player.State;
using SocialProyectoCenigraf.Player.Replication;
using SocialProyectoCenigraf.CameraSystem.Follow;
using SocialProyectoCenigraf.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace SocialProyectoCenigraf.Editor
{
    public static class IntegrationValidation
    {
        public static void Run()
        {
            try
            {
                foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes.Where(s => s.enabled))
                {
                    Scene scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                    foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    {
                        Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                            "Missing script: " + entry.path + " / " + child.name);
                        foreach (MonoBehaviour behaviour in child.GetComponents<MonoBehaviour>())
                        {
                            if (behaviour == null) continue;
                            SerializedProperty property = new SerializedObject(behaviour).GetIterator();
                            while (property.Next(true))
                            {
                                if (property.propertyType == SerializedPropertyType.ObjectReference)
                                    Require(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                                        "Broken reference: " + child.name + "." + property.propertyPath);
                            }
                        }
                    }
                    Debug.Log("INTEGRATION: Scene loaded, no missing scripts/references: " + entry.path);
                    if (scene.name == "SceneDemo") ValidateGameplayScene(scene);
                }

                Require(TMP_Settings.defaultFontAsset != null, "Missing TMP default font");
                ValidatePlayerReplicationBoundary();
                Debug.Log("INTEGRATION VALIDATION PASSED");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void ValidateGameplayScene(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            PlayerStateStore[] players = roots.SelectMany(r => r.GetComponentsInChildren<PlayerStateStore>(true)).ToArray();
            Require(players.Length == 1, "SceneDemo must contain exactly one PlayerStateStore");
            PlayerStateStore player = players[0];
            Require(player.CompareTag("Player"), "Player tag required by Carlos interactions");
            Require(PrefabUtility.IsPartOfPrefabInstance(player), "Player must remain Daniel's prefab instance");
            PlayerReplicationContext replicationContext =
                player.GetComponent<PlayerReplicationContext>();
            Require(replicationContext != null,
                "Player must expose a replication context");
            Require(replicationContext.Authority == PlayerControlAuthority.OfflineLocal,
                "Current Player must remain in offline-local mode");
            Require(player.GetComponent<PlayerReplicationBridge>() != null,
                "Player must expose the transport-neutral replication bridge");
            Require(roots.SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).Count() == 1,
                "SceneDemo must have exactly one EventSystem");
            foreach (Component component in roots.SelectMany(r => r.GetComponentsInChildren<Component>(true)))
            {
                if (component is CameraFollowController || component is PlayerAppearancePreview ||
                    component is PlayerAppearanceCustomizationController)
                {
                    SerializedProperty reference = new SerializedObject(component).FindProperty("playerStateStore");
                    Require(reference != null && reference.objectReferenceValue == player,
                        "Player reference lost on " + component.GetType().Name);
                }
            }
            ObjetoRecogible pickup = roots.SelectMany(r => r.GetComponentsInChildren<ObjetoRecogible>(true)).Single();
            Require(pickup.player == player.transform, "Carnet must target the prefab player");
            Require(pickup.objetosBloqueo.Length == 2 && pickup.objetosBloqueo.All(g => g != null),
                "Carnet must retain both restricted-area references");
            CameraBounds bounds = roots.SelectMany(r => r.GetComponentsInChildren<CameraBounds>(true)).Single();
            Vector2 constrained = bounds.ConstrainPosition(new Vector2(10000f, -10000f), 0f, 1f);
            Require(constrained.x <= bounds.maxX && constrained.y >= bounds.minY, "Camera limits failed");
            Vector2 wide = bounds.ConstrainPosition(Vector2.zero, 10000f, 2f);
            Require(Mathf.Approximately(wide.x, (bounds.minX + bounds.maxX) * 0.5f) &&
                    Mathf.Approximately(wide.y, (bounds.minY + bounds.maxY) * 0.5f), "Large zoom bounds failed");
            Debug.Log("INTEGRATION: prefab, UI/camera references, event system, carnet and bounds verified");
        }

        private static void ValidatePlayerReplicationBoundary()
        {
            PlayerStateData localState = new PlayerStateData(
                new Vector2(1f, 2f),
                true,
                new Vector2(0.5f, 0.25f),
                new Vector2(0f, -0.1f),
                "visitor",
                "Demo",
                PlayerFacingDirection.DownRight,
                170,
                4,
                true,
                Color.white,
                Color.white,
                Color.white,
                "local-player",
                false);
            PlayerReplicatedStateData remoteSnapshot =
                new PlayerReplicatedStateData(
                    "remote-player",
                    new Vector2(8f, 9f),
                    "student",
                    "Demo",
                    PlayerFacingDirection.UpLeft,
                    true,
                    Color.red,
                    Color.green,
                    Color.blue);

            PlayerStateData result = PlayerStateReducer.Reduce(
                localState,
                PlayerAction.ApplyReplicatedState(remoteSnapshot));

            Require(result.PlayerId == "remote-player" &&
                    result.Position == new Vector2(8f, 9f) &&
                    result.IsMoving &&
                    result.FacingDirection == PlayerFacingDirection.UpLeft,
                "Replicated player fields were not applied atomically");
            Require(result.ColliderSize == localState.ColliderSize &&
                    result.ColliderOffset == localState.ColliderOffset &&
                    result.YSortEnabled == localState.YSortEnabled &&
                    result.AnimationFrameDurationMilliseconds ==
                        localState.AnimationFrameDurationMilliseconds,
                "A replicated snapshot changed local player configuration");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
