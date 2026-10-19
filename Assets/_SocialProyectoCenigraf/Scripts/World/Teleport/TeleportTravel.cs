using System.Collections;
using System.Collections.Generic;
using SocialProyectoCenigraf.CameraSystem.Follow;
using SocialProyectoCenigraf.Player.Movement;
using SocialProyectoCenigraf.Player.State;
using SocialProyectoCenigraf.World.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.World.Teleport
{
    public sealed class TeleportTravel : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float minimumLoadingSeconds = 1f;
        [SerializeField, Min(0f)] private float fadeSeconds = 0.35f;
        public static bool IsTraveling { get; private set; }

        private CanvasGroup cover;
        private Camera loadingCamera;
        private Scene arrivalScene;
        private readonly List<PlayerGate> players = new List<PlayerGate>();
        private readonly List<CameraGate> cameras = new List<CameraGate>();
        private readonly List<FollowGate> followers = new List<FollowGate>();
        private readonly Dictionary<Renderer, bool> renderers = new Dictionary<Renderer, bool>();
        private bool finished;

        private sealed class PlayerGate
        {
            public PlayerMovementController Player;
            public bool Enabled;
            public Rigidbody2D Body;
            public bool Simulated;
        }
        private sealed class CameraGate { public Camera Camera; public bool Enabled; }
        private sealed class FollowGate { public CameraFollowController Follow; public bool Enabled; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsTraveling = false;

        public static void Begin(string scenePath, string targetId)
        {
            if (IsTraveling) return;
            if (!Application.CanStreamedLevelBeLoaded(scenePath))
            {
                Debug.LogWarning($"Teleport: la escena '{scenePath}' no está habilitada en Build Profiles. No se cambió de escena.");
                return;
            }
            IsTraveling = true;
            var host = new GameObject("TeleportTravel").AddComponent<TeleportTravel>();
            DontDestroyOnLoad(host.gameObject);
            host.BuildCover(); // Opaque before any scene activation or arrival frame.
            host.StartCoroutine(host.Travel(scenePath, targetId));
        }
        private void BuildCover()
        {
            // A real camera keeps Game View rendering while arrival cameras are gated.
            var cameraObject = new GameObject("LoadingCamera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            loadingCamera = cameraObject.GetComponent<Camera>();
            loadingCamera.clearFlags = CameraClearFlags.SolidColor;
            loadingCamera.backgroundColor = Color.black;
            loadingCamera.cullingMask = 0;
            loadingCamera.depth = -1000;
            loadingCamera.orthographic = true;
            loadingCamera.allowHDR = false;
            loadingCamera.allowMSAA = false;

            var prefab = Resources.Load<GameObject>("UI/LoadingScreen");
            var root = prefab != null ? Instantiate(prefab, transform, false) : BuildDefaultLoadingScreen();
            root.name = "LoadingScreen";
            cover = root.GetComponent<CanvasGroup>();
            cover.alpha = 1f;cover.blocksRaycasts = true;
            var icon = root.transform.Find("LoadingIndicator/IconSlot/LoadingIcon");
            if (icon != null)
            {
                var image = icon.GetComponent<Image>();
                image.enabled = image.sprite != null;
            }
        }
        private GameObject BuildDefaultLoadingScreen()
        {
            var root = new GameObject("LoadingScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var black = new GameObject("Black", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            black.transform.SetParent(root.transform, false);
            var rect = (RectTransform)black.transform;
            rect.anchorMin = Vector2.zero;rect.anchorMax = Vector2.one;rect.offsetMin = rect.offsetMax = Vector2.zero;
            black.GetComponent<Image>().color = Color.black;

            var indicator = new GameObject("LoadingIndicator", typeof(RectTransform));
            indicator.transform.SetParent(root.transform, false);
            var row = (RectTransform)indicator.transform;
            row.anchorMin = row.anchorMax = new Vector2(1, 0);
            row.pivot = new Vector2(1, 0);
            row.anchoredPosition = new Vector2(-48, 40);
            row.sizeDelta = new Vector2(430, 72);
            var label = new GameObject("LoadingLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(indicator.transform, false);
            var textRect = (RectTransform)label.transform;
            textRect.anchorMin = Vector2.zero;textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;textRect.offsetMax = new Vector2(-88, 0);
            var text = label.GetComponent<TextMeshProUGUI>();text.font = TMP_Settings.defaultFontAsset;
            text.text = "Cargando…";text.fontSize = 28;text.alignment = TextAlignmentOptions.MidlineRight;
            text.enableAutoSizing = true;text.fontSizeMin = 18;text.fontSizeMax = 28;
            text.color = Color.white;text.raycastTarget = false;

            var slot = new GameObject("IconSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            slot.transform.SetParent(indicator.transform, false);
            var slotRect = (RectTransform)slot.transform;
            slotRect.anchorMin = slotRect.anchorMax = new Vector2(1, 0.5f);
            slotRect.pivot = new Vector2(1, 0.5f);slotRect.anchoredPosition = Vector2.zero;
            slotRect.sizeDelta = new Vector2(64, 64);
            var frame = slot.GetComponent<Image>();frame.color = new Color(0.45f, 0.76f, 0.78f, 0.4f);frame.raycastTarget = false;
            var inset = new GameObject("Inset", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            inset.transform.SetParent(slot.transform, false);
            var insetRect = (RectTransform)inset.transform;insetRect.sizeDelta = new Vector2(60, 60);
            inset.GetComponent<Image>().color = Color.black;inset.GetComponent<Image>().raycastTarget = false;
            var icon = new GameObject("LoadingIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icon.transform.SetParent(slot.transform, false);
            ((RectTransform)icon.transform).sizeDelta = new Vector2(52, 52);
            var image = icon.GetComponent<Image>();image.preserveAspect = true;image.raycastTarget = false;image.enabled = false;
            return root;
        }
        private IEnumerator Travel(string scenePath, string targetId)
        {
            float coveredAt = Time.unscaledTime;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            try
            {
                yield return null; // Present black before starting the scene load.
                AsyncOperation load = null;
                try { load = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single); }
                catch (System.Exception error) { Debug.LogException(error); }
                if (load == null) yield break;
                yield return load;
                yield return null; // Let Start initialize scene objects while still hidden and frozen.
                PlacePlayer(targetId);
                HideNewPlayerRenderers();
                SnapCameras();
                float remaining = minimumLoadingSeconds - (Time.unscaledTime - coveredAt);
                if (remaining > 0f) yield return new WaitForSecondsRealtime(remaining);
                SnapCameras();
                RestoreVisuals();
                if (fadeSeconds > 0f)
                {
                    float elapsed = 0f;
                    while (elapsed < fadeSeconds)
                    {
                        cover.alpha = 1f - Mathf.Clamp01(elapsed / fadeSeconds);
                        yield return null;
                        elapsed += Time.unscaledDeltaTime;
                    }
                }
                cover.alpha = 0f;
            }
            finally { Finish(); }
        }
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            arrivalScene = scene;
            foreach (var player in FindObjectsByType<PlayerMovementController>(FindObjectsSortMode.None))
            {
                if (player.gameObject.scene != scene || !player.isActiveAndEnabled) continue;
                var body = player.GetComponent<Rigidbody2D>();
                players.Add(new PlayerGate { Player = player, Enabled = player.enabled, Body = body, Simulated = body != null && body.simulated });
                player.enabled = false;
                if (body != null) { body.linearVelocity = Vector2.zero;body.simulated = false; }
            }
            foreach (var follow in FindObjectsByType<CameraFollowController>(FindObjectsSortMode.None))
            {
                if (follow.gameObject.scene != scene) continue;
                followers.Add(new FollowGate { Follow = follow, Enabled = follow.enabled });
                follow.enabled = false;
            }
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera.gameObject.scene != scene) continue;
                cameras.Add(new CameraGate { Camera = camera, Enabled = camera.enabled });
                camera.enabled = false;
            }
            HideNewPlayerRenderers();
        }
        private void HideNewPlayerRenderers()
        {
            foreach (var gate in players)
            {
                if (gate.Player == null) continue;
                foreach (var renderer in gate.Player.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderers.ContainsKey(renderer)) renderers.Add(renderer, renderer.forceRenderingOff);
                    renderer.forceRenderingOff = true;
                }
            }
        }
        private void PlacePlayer(string targetId)
        {
            SceneTeleport destination = null;
            bool duplicate = false;
            foreach (var point in FindObjectsByType<SceneTeleport>(FindObjectsSortMode.None))
            {
                if (point.gameObject.scene != arrivalScene || string.IsNullOrEmpty(targetId) || point.TeleportId != targetId) continue;
                if (destination != null) { duplicate = true;break; }
                destination = point;
            }
            if (destination == null || duplicate)
            {
                Debug.LogWarning(duplicate
                    ? $"Teleport: ID '{targetId}' duplicado en '{arrivalScene.name}'. Se conserva el spawn por defecto."
                    : $"Teleport: no se encontró el ID '{targetId}' en '{arrivalScene.name}'. Se conserva el spawn por defecto.");
                return;
            }
            if (players.Count == 0 || players[0].Player == null)
            {
                Debug.LogWarning($"Teleport: '{arrivalScene.name}' no contiene un jugador local activo.");
                return;
            }
            var gate = players[0];
            Vector3 position = destination.SpawnPosition;
            if (gate.Body != null) { gate.Body.position = position;gate.Body.linearVelocity = Vector2.zero; }
            gate.Player.transform.position = position;
            gate.Player.GetComponent<PlayerStateStore>().Dispatch(PlayerAction.ReconcilePosition(position));
            Physics2D.SyncTransforms();
            gate.Player.GetComponent<WorldYSort>()?.RefreshSortingOrder();
        }
        private void SnapCameras()
        {
            foreach (var gate in followers) if (gate.Enabled && gate.Follow != null) gate.Follow.SnapToTarget();
        }
        private void RestoreVisuals()
        {
            foreach (var pair in renderers) if (pair.Key != null) pair.Key.forceRenderingOff = pair.Value;
            foreach (var gate in followers) if (gate.Follow != null) gate.Follow.enabled = gate.Enabled;
            bool sceneCameraReady = false;
            foreach (var gate in cameras)
            {
                if (gate.Camera == null) continue;
                gate.Camera.enabled = gate.Enabled;
                sceneCameraReady |= gate.Camera.isActiveAndEnabled;
            }
            // Disable the black camera only after a scene camera can take over, never leaving a camera-less frame.
            if (loadingCamera != null) loadingCamera.enabled = !sceneCameraReady;
        }
        private void Finish(bool destroyHost = true)
        {
            if (finished) return;
            finished = true;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            RestoreVisuals();
            foreach (var gate in players)
            {
                if (gate.Body != null) gate.Body.simulated = gate.Simulated;
                if (gate.Player != null) gate.Player.enabled = gate.Enabled;
            }
            Physics2D.SyncTransforms();
            IsTraveling = false;
            if (destroyHost) Destroy(gameObject);
        }
        private void OnDestroy() { if (!finished) Finish(false); }
    }
}
