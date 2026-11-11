using System;
using System.Collections;
using SocialProyectoCenigraf.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    /// <summary>
    /// A band across the middle third of the screen that announces what comes next. It holds the
    /// player for a moment so the change of stage is understood, and calls back when it is gone.
    /// Builds itself at runtime, so no scene needs to carry it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageBanner : MonoBehaviour
    {
        private const float FadeIn = 0.35f;
        private const float Hold = 1.6f;
        private const float FadeOut = 0.35f;

        private static StageBanner instance;

        private CanvasGroup group;
        private TMP_Text titleText;
        private TMP_Text subtitleText;
        private Coroutine routine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        /// <summary>Shows the band and runs <paramref name="onFinished"/> once it has faded out.</summary>
        public static void Show(string title, string subtitle, Action onFinished = null)
        {
            StageBanner banner = GetInstance();
            banner.Play(title, subtitle, onFinished);
        }

        private static StageBanner GetInstance()
        {
            if (instance == null)
            {
                GameObject host = new GameObject(nameof(StageBanner));
                instance = host.AddComponent<StageBanner>();
                DontDestroyOnLoad(host);
            }

            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Build();
        }

        private void OnDestroy()
        {
            GameplayInputLock.Release(this);

            if (instance == this)
            {
                instance = null;
            }
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // The whole width, the middle third of the height.
            GameObject bandObject = new GameObject("Band", typeof(RectTransform));
            RectTransform band = bandObject.GetComponent<RectTransform>();
            band.SetParent(transform, false);
            band.anchorMin = new Vector2(0f, 1f / 3f);
            band.anchorMax = new Vector2(1f, 2f / 3f);
            band.offsetMin = Vector2.zero;
            band.offsetMax = Vector2.zero;

            Image background = bandObject.AddComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.08f, 1f);
            background.raycastTarget = false;

            titleText = CreateText(band, "Title", 64f, new Vector2(0f, 0.52f), new Vector2(1f, 1f));
            subtitleText = CreateText(band, "Subtitle", 28f, new Vector2(0f, 0f), new Vector2(1f, 0.48f));
            subtitleText.color = new Color(0.72f, 0.78f, 0.86f);
        }

        private static TMP_Text CreateText(
            RectTransform parent, string name, float size, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(40f, 10f);
            rect.offsetMax = new Vector2(-40f, -10f);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private void Play(string title, string subtitle, Action onFinished)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                GameplayInputLock.Release(this);
            }

            titleText.text = title;
            subtitleText.text = subtitle;
            routine = StartCoroutine(Sequence(onFinished));
        }

        private IEnumerator Sequence(Action onFinished)
        {
            GameplayInputLock.Acquire(this);

            yield return Fade(0f, 1f, FadeIn);

            float elapsed = 0f;

            while (elapsed < Hold)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return Fade(1f, 0f, FadeOut);

            group.alpha = 0f;
            GameplayInputLock.Release(this);
            routine = null;
            onFinished?.Invoke();
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / seconds);
                group.alpha = Mathf.Lerp(from, to, progress * progress * (3f - 2f * progress));
                yield return null;
            }

            group.alpha = to;
        }
    }
}
