using UnityEngine;

namespace SocialProyectoCenigraf.World.NPC
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cenigraf/NPC/Warning Zone Visual Fade")]
    public sealed class NPCWarningZoneVisualFade : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private bool proximityFadeEnabled;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer[] renderers;
        [SerializeField] private NPCLayerColorController layerColorController;

        [Header("Fade durations")]
        [SerializeField, Min(0f)] private float fadeInSeconds = 0.3f;
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.5f;

        private Color[] baseColors;
        private float visibility;
        private float targetVisibility;
        private int playersInWarningZone;

        public float FadeInSeconds => fadeInSeconds;
        public float FadeOutSeconds => fadeOutSeconds;
        public bool ProximityFadeEnabled => proximityFadeEnabled;

        private void Reset() => FindVisuals();

        private void Awake()
        {
            FindVisuals();
            CaptureBaseColors();
            if (layerColorController == null)
            {
                layerColorController = GetComponent<NPCLayerColorController>();
            }
            visibility = proximityFadeEnabled ? 0f : 1f;
            targetVisibility = visibility;
            ApplyVisibility();
            layerColorController?.SetVisualOpacity(visibility);
        }

        private void Update()
        {
            if (!proximityFadeEnabled)
            {
                visibility = 1f;
                targetVisibility = 1f;
                ApplyVisibility();
                layerColorController?.SetVisualOpacity(1f);
                return;
            }

            float duration = targetVisibility > visibility
                ? fadeInSeconds
                : fadeOutSeconds;

            if (duration <= Mathf.Epsilon)
            {
                visibility = targetVisibility;
            }
            else
            {
                visibility = Mathf.MoveTowards(
                    visibility,
                    targetVisibility,
                    Time.deltaTime / duration);
            }

            ApplyVisibility();
            layerColorController?.SetVisualOpacity(visibility);
        }

        public void PlayerEnteredWarningZone()
        {
            if (!proximityFadeEnabled)
            {
                return;
            }

            playersInWarningZone++;
            SetVisible(true);
        }

        public void PlayerExitedWarningZone()
        {
            if (!proximityFadeEnabled)
            {
                return;
            }

            playersInWarningZone = Mathf.Max(0, playersInWarningZone - 1);
            if (playersInWarningZone == 0)
            {
                SetVisible(false);
            }
        }

        public void SetVisible(bool visible)
        {
            if (!proximityFadeEnabled)
            {
                targetVisibility = 1f;
                return;
            }

            targetVisibility = visible ? 1f : 0f;
        }

        private void ApplyVisibility()
        {
            if (renderers == null || baseColors == null)
            {
                return;
            }

            int count = Mathf.Min(renderers.Length, baseColors.Length);
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer spriteRenderer = renderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                Color color = spriteRenderer.color;
                color.a = baseColors[i].a * visibility;
                spriteRenderer.color = color;
            }
        }

        private void FindVisuals()
        {
            if (visualRoot == null)
            {
                Transform[] children = GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child.name == "Visual")
                    {
                        visualRoot = child;
                        break;
                    }
                }
            }

            if (visualRoot != null && (renderers == null || renderers.Length == 0))
            {
                renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        private void CaptureBaseColors()
        {
            if (renderers == null)
            {
                return;
            }

            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    baseColors[i] = renderers[i].color;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            fadeInSeconds = Mathf.Max(0f, fadeInSeconds);
            fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
            FindVisuals();
            if (layerColorController == null)
            {
                layerColorController = GetComponent<NPCLayerColorController>();
            }

            if (!Application.isPlaying)
            {
                visibility = proximityFadeEnabled ? 0f : 1f;
                targetVisibility = visibility;
            }
        }
#endif
    }
}
