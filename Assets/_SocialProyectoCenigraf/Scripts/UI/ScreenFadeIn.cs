using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    // Black curtain that dissolves over a scene. It is built in Awake so the first
    // rendered frame is already black: the splash screen then melts into the scene
    // instead of cutting to it. The curtain is cosmetic, it never eats input, and it
    // destroys itself as soon as it is transparent.
    [DisallowMultipleComponent]
    public sealed class ScreenFadeIn : MonoBehaviour
    {
        // One below the loading screen of the teleports, above every scene canvas.
        private const int CurtainSortingOrder = short.MaxValue - 1;

        private CanvasGroup group;
        private float fadeSeconds;
        private float holdSeconds;
        private float elapsed;

        public static ScreenFadeIn FromBlack(float seconds, float holdSeconds = 0f)
        {
            if (seconds <= 0f && holdSeconds <= 0f)
            {
                return null;
            }

            GameObject host = new GameObject(
                nameof(ScreenFadeIn),
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup));

            ScreenFadeIn curtain = host.AddComponent<ScreenFadeIn>();
            curtain.Build(Mathf.Max(0f, seconds), Mathf.Max(0f, holdSeconds));
            return curtain;
        }

        private void Build(float seconds, float hold)
        {
            fadeSeconds = seconds;
            holdSeconds = hold;

            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CurtainSortingOrder;

            group = GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;

            GameObject black = new GameObject(
                "Black",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            black.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)black.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = black.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
        }

        private void Update()
        {
            // Unscaled: the first frames of a scene are long and a paused game must not
            // leave the curtain stuck on screen.
            elapsed += Time.unscaledDeltaTime;

            float progress = elapsed - holdSeconds;
            if (progress <= 0f)
            {
                return;
            }

            group.alpha = fadeSeconds <= 0f
                ? 0f
                : 1f - Mathf.Clamp01(progress / fadeSeconds);

            if (group.alpha <= 0.001f)
            {
                Destroy(gameObject);
            }
        }
    }
}
