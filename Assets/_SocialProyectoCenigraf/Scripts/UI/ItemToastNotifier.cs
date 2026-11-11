using System.Collections.Generic;
using SocialProyectoCenigraf.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    /// <summary>
    /// Small notices on the right side of the screen, one per item picked up. They slide in, hold
    /// for a few seconds and leave; the ones below move up to take the free place.
    /// Builds itself at runtime, so no scene needs to carry it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemToastNotifier : MonoBehaviour
    {
        /// <summary>Lo que espera el primero de la fila antes de empezar a irse.</summary>
        private const float HoldSeconds = 4f;
        /// <summary>Lo que espera el siguiente una vez que la fila se reacomoda.</summary>
        private const float CascadeSeconds = 0.5f;
        private const float RiseSeconds = 0.5f;
        private const float RiseDistance = 48f;
        private const float FadeOut = 0.3f;
        private const float Width = 300f;
        private const float Height = 56f;
        private const float Spacing = 8f;
        private const int MaxOnScreen = 6;

        private static ItemToastNotifier instance;
        private static readonly Dictionary<string, int> lastKnown = new Dictionary<string, int>();

        private readonly List<Toast> toasts = new List<Toast>();
        private RectTransform stack;

        private float headTimer;

        private sealed class Toast
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public float Age;
            public float StartY;
            public float TargetY;
            public bool Leaving;
            public float LeavingAge;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            lastKnown.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            GameObject host = new GameObject(nameof(ItemToastNotifier));
            instance = host.AddComponent<ItemToastNotifier>();
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            BuildCanvas();
            PlayerItemInventory.ItemAdded += HandleItemAdded;
        }

        private void OnDestroy()
        {
            PlayerItemInventory.ItemAdded -= HandleItemAdded;

            if (instance == this)
            {
                instance = null;
            }
        }

        private void BuildCanvas()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject stackObject = new GameObject("Stack", typeof(RectTransform));
            stack = stackObject.GetComponent<RectTransform>();
            stack.SetParent(transform, false);

            // Right side, just under the label with the name of the scene.
            stack.anchorMin = new Vector2(1f, 1f);
            stack.anchorMax = new Vector2(1f, 1f);
            stack.pivot = new Vector2(1f, 1f);
            stack.anchoredPosition = new Vector2(-16f, -200f);
            stack.sizeDelta = new Vector2(Width, 0f);
        }

        /// <summary>The inventory reports totals, so the amount picked up is the difference.</summary>
        private void HandleItemAdded(ItemDefinition item, int total)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id))
            {
                return;
            }

            string key = item.id.Trim();
            lastKnown.TryGetValue(key, out int previous);
            lastKnown[key] = total;

            int amount = total - previous;

            if (amount > 0)
            {
                Show(item, amount);
            }
        }

        public static void Notify(ItemDefinition item, int amount)
        {
            if (instance != null)
            {
                instance.Show(item, amount);
            }
        }

        private void Show(ItemDefinition item, int amount)
        {
            if (stack == null)
            {
                return;
            }

            if (toasts.Count >= MaxOnScreen && toasts[0].Rect != null)
            {
                Destroy(toasts[0].Rect.gameObject);
                toasts.RemoveAt(0);
            }

            toasts.Add(BuildToast(item, amount));
            Reposition();

            // Whatever is on screen gets the full time again: the newest one has to be readable.
            headTimer = HoldSeconds;
        }

        private Toast BuildToast(ItemDefinition item, int amount)
        {
            GameObject toastObject = new GameObject($"Toast_{item.id}", typeof(RectTransform));
            RectTransform rect = toastObject.GetComponent<RectTransform>();
            rect.SetParent(stack, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(Width, Height);

            CanvasGroup group = toastObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            UIRoundedRectangle background = toastObject.AddComponent<UIRoundedRectangle>();
            background.color = new Color(0.05f, 0.07f, 0.11f, 1f);
            background.raycastTarget = false;

            bool hasIcon = item.icon != null;

            if (hasIcon)
            {
                GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.SetParent(rect, false);
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(10f, 0f);
                iconRect.sizeDelta = new Vector2(36f, 36f);

                Image icon = iconObject.AddComponent<Image>();
                icon.sprite = item.icon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(hasIcon ? 56f : 14f, 6f);
            labelRect.offsetMax = new Vector2(-12f, -6f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.Left;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.text = $"x{amount}  {item.displayName}";

            // Enters from below.
            float targetY = -toasts.Count * (Height + Spacing);
            rect.anchoredPosition = new Vector2(0f, targetY - RiseDistance);

            return new Toast
            {
                Rect = rect,
                Group = group,
                StartY = targetY - RiseDistance,
                TargetY = targetY
            };
        }

        private void Reposition()
        {
            for (int i = 0; i < toasts.Count; i++)
            {
                toasts[i].TargetY = -i * (Height + Spacing);
            }
        }

        private void Update()
        {
            if (toasts.Count == 0)
            {
                return;
            }

            float delta = Time.unscaledDeltaTime;
            Toast head = toasts[0];

            // Only the first of the queue leaves; the rest wait their turn.
            if (!head.Leaving)
            {
                headTimer -= delta;

                if (headTimer <= 0f)
                {
                    head.Leaving = true;
                    head.LeavingAge = 0f;
                }
            }

            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                Toast toast = toasts[i];

                if (toast.Rect == null)
                {
                    toasts.RemoveAt(i);
                    continue;
                }

                toast.Age += delta;

                if (toast.Leaving)
                {
                    toast.LeavingAge += delta;
                    toast.Group.alpha = Mathf.Clamp01(1f - toast.LeavingAge / FadeOut);

                    if (toast.LeavingAge >= FadeOut)
                    {
                        Destroy(toast.Rect.gameObject);
                        toasts.RemoveAt(i);
                        Reposition();

                        // The queue settles, then the next one leaves.
                        headTimer = CascadeSeconds;
                    }

                    continue;
                }

                if (toast.Age < RiseSeconds)
                {
                    // Rises into place while it appears.
                    float progress = toast.Age / RiseSeconds;
                    float eased = progress * progress * (3f - 2f * progress);
                    toast.Group.alpha = eased;
                    toast.Rect.anchoredPosition = new Vector2(
                        0f, Mathf.Lerp(toast.StartY, toast.TargetY, eased));
                    continue;
                }

                toast.Group.alpha = 1f;
                toast.Rect.anchoredPosition = Vector2.Lerp(
                    toast.Rect.anchoredPosition,
                    new Vector2(0f, toast.TargetY),
                    1f - Mathf.Exp(-14f * delta));
            }
        }
    }
}
