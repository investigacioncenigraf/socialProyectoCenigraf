using System.Collections;
using SocialProyectoCenigraf.Dialogue;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SocialProyectoCenigraf.World.Teleport
{
    /// <summary>
    /// Reveals the induction path and enables its portal after the temple welcome dialogue.
    /// The progress is session-scoped through DialogueProgress.
    /// </summary>
    public sealed class TemplePathDialogueUnlock : MonoBehaviour
    {
        private const string TempleSceneName = "Templo_De_Los_4_Caminos";
        private const string WelcomeDialogueId = "Sabio_de_los_cuatro_caminos_bienvenida";
        private const string PathOverlayName = "Camino_de_Induccion_color";
        private const string PortalName = "Teleport_Prueba de la Induccion";

        [SerializeField, Min(0f)] private float fadeDelaySeconds = 0.3f;
        [SerializeField, Min(0f)] private float fadeDurationSeconds = 0.5f;

        private Coroutine fadeRoutine;
        private bool pathRevealed;

        private void Awake()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            DialogueProgress.DialogueCompleted += OnDialogueCompleted;
            ApplyCurrentScene();
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            DialogueProgress.DialogueCompleted -= OnDialogueCompleted;
        }

        private void OnActiveSceneChanged(Scene previous, Scene current)
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
            ApplyCurrentScene();
        }

        private void OnDialogueCompleted(string dialogueId)
        {
            if (!string.Equals(dialogueId, WelcomeDialogueId, System.StringComparison.Ordinal)) return;
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != TempleSceneName) return;

            SceneTeleport portal = FindTargetPortal();
            if (portal != null) portal.SetPortalActive(true);

            if (!pathRevealed) return;
            pathRevealed = false;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            GameObject pathOverlay = GameObject.Find(PathOverlayName);
            if (pathOverlay != null) fadeRoutine = StartCoroutine(FadeOutPath(pathOverlay));
        }

        private void ApplyCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != TempleSceneName) return;

            bool unlocked = DialogueProgress.HasCompletedDialogue(WelcomeDialogueId);
            GameObject pathOverlay = GameObject.Find(PathOverlayName);
            if (pathOverlay != null)
                SetPathVisibility(pathOverlay, !unlocked);
            pathRevealed = !unlocked;

            SceneTeleport portal = FindTargetPortal();
            if (portal != null)
                portal.SetPortalActive(unlocked);
            else
                Debug.LogWarning($"No se encontró el portal de inducción en la escena {TempleSceneName}.");
        }

        private IEnumerator FadeOutPath(GameObject pathOverlay)
        {
            Tilemap[] tilemaps = pathOverlay.GetComponentsInChildren<Tilemap>(true);
            SpriteRenderer[] spriteRenderers = pathOverlay.GetComponentsInChildren<SpriteRenderer>(true);
            Color[] tilemapColors = new Color[tilemaps.Length];
            Color[] spriteColors = new Color[spriteRenderers.Length];

            for (int i = 0; i < tilemaps.Length; i++)
            {
                tilemapColors[i] = tilemaps[i].color;
                TilemapRenderer renderer = tilemaps[i].GetComponent<TilemapRenderer>();
                if (renderer != null) renderer.enabled = true;
            }
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                spriteColors[i] = spriteRenderers[i].color;
                spriteRenderers[i].enabled = true;
            }

            if (fadeDelaySeconds > 0f)
                yield return new WaitForSecondsRealtime(fadeDelaySeconds);

            SocialProyectoCenigraf.Audio.RetroFeedbackAudio.PlayMissionAssigned();

            float duration = Mathf.Max(0f, fadeDurationSeconds);
            float elapsed = 0f;
            while (duration > 0f && elapsed < duration)
            {
                float opacity = 1f - Mathf.Clamp01(elapsed / duration);
                ApplyPathOpacity(tilemaps, tilemapColors, spriteRenderers, spriteColors, opacity);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            ApplyPathOpacity(tilemaps, tilemapColors, spriteRenderers, spriteColors, 0f);
            SetPathVisibility(pathOverlay, false);
            fadeRoutine = null;
        }

        private static void ApplyPathOpacity(
            Tilemap[] tilemaps,
            Color[] tilemapColors,
            SpriteRenderer[] spriteRenderers,
            Color[] spriteColors,
            float opacity)
        {
            for (int i = 0; i < tilemaps.Length; i++)
            {
                if (tilemaps[i] == null) continue;
                Color color = tilemapColors[i];
                color.a *= opacity;
                tilemaps[i].color = color;
            }
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null) continue;
                Color color = spriteColors[i];
                color.a *= opacity;
                spriteRenderers[i].color = color;
            }
        }

        private static void SetPathVisibility(GameObject pathOverlay, bool visible)
        {
            foreach (TilemapRenderer renderer in pathOverlay.GetComponentsInChildren<TilemapRenderer>(true))
                renderer.enabled = visible;
            foreach (SpriteRenderer renderer in pathOverlay.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.enabled = visible;
        }

        private static SceneTeleport FindTargetPortal()
        {
            SceneTeleport[] portals = FindObjectsByType<SceneTeleport>(FindObjectsSortMode.None);
            foreach (SceneTeleport portal in portals)
                if (portal.gameObject.name == PortalName) return portal;

            // Existing scene instance is linked to this dialogue, even if its hierarchy name differs.
            foreach (SceneTeleport portal in portals)
                if (portal.LockedDialogueId == WelcomeDialogueId) return portal;

            return null;
        }
    }
}
