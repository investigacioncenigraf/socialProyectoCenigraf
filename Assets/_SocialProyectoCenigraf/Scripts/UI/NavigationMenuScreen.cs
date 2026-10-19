using System;
using SocialProyectoCenigraf.Navigation;
using SocialProyectoCenigraf.Roles;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SocialProyectoCenigraf.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class NavigationMenuScreen : MonoBehaviour
    {
        [Serializable]
        private sealed class NavigationOption
        {
            public string label = "Nueva opción";
            public RoleDefinition visualStyle;
            public string destinationSceneName;
        }

        [Header("Content")]
        [SerializeField] private string title = "Menú";
        [SerializeField] private string subtitle;
        [SerializeField] private NavigationOption[] options = Array.Empty<NavigationOption>();

        [Header("Footer")]
        [SerializeField] private string footer =
            "Entorno de desarrollo · Fase de Implementación de Estados";

        [Header("Appearance")]
        [SerializeField] private Color backgroundColor =
            new Color(0.18f, 0.17f, 0.25f, 1f);
        [SerializeField, Range(0f, 1f)] private float dimOpacity = 0.34f;

        private GameObject generatedRoot;

#if UNITY_EDITOR
        private bool rebuildScheduled;
#endif

        private void OnEnable()
        {
            BuildScreen();
        }

        private void OnDisable()
        {
            ClearScreen();
        }

        private void OnDestroy()
        {
            ClearScreen();
        }

        private void BuildScreen()
        {
            ClearScreen();

            generatedRoot = new GameObject(
                Application.isPlaying ? "NavigationMenuUI" : "__NavigationMenuPreview",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            MarkAsPreviewObject(generatedRoot);

            RectTransform rootRect = generatedRoot.GetComponent<RectTransform>();
            rootRect.SetParent(transform, false);

            Canvas canvas = generatedRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = generatedRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject background = CreatePanel("Background", rootRect, backgroundColor);
            background.AddComponent<DecorativeSquaresLayer>();

            GameObject dimLayer = CreatePanel(
                "DimLayer",
                rootRect,
                new Color(0f, 0f, 0f, dimOpacity));
            dimLayer.GetComponent<Image>().raycastTarget = false;

            RectTransform content = CreateRect("SafeArea", rootRect);
            Stretch(content);

            TMP_Text titleText = CreateText(
                "TXT_Title",
                content,
                title,
                58f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetAnchoredRect(
                titleText.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 250f),
                new Vector2(1100f, 90f));

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                TMP_Text subtitleText = CreateText(
                    "TXT_Subtitle",
                    content,
                    subtitle,
                    27f,
                    FontStyles.Normal,
                    TextAlignmentOptions.Center);
                subtitleText.color = new Color(1f, 1f, 1f, 0.82f);
                SetAnchoredRect(
                    subtitleText.rectTransform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 175f),
                    new Vector2(1000f, 60f));
            }

            BuildOptions(content);
            BuildFooter(content);

            if (Application.isPlaying)
            {
                EnsureEventSystem();
            }
        }

        private void BuildOptions(RectTransform parent)
        {
            if (options == null || options.Length == 0)
            {
                return;
            }

            RectTransform container = CreateRect("MenuOptions", parent);
            float height = options.Length * 80f + Mathf.Max(0, options.Length - 1) * 16f;
            SetAnchoredRect(
                container,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f),
                new Vector2(500f, height));

            VerticalLayoutGroup layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < options.Length; i++)
            {
                CreateNavigationButton(container, options[i], i);
            }
        }

        private void CreateNavigationButton(
            RectTransform parent,
            NavigationOption option,
            int index)
        {
            GameObject buttonObject = new GameObject(
                $"BTN_Option_{index + 1:00}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIRoundedRectangle),
                typeof(Button),
                typeof(LayoutElement));
            MarkAsPreviewObject(buttonObject);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(parent, false);

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 500f;
            layoutElement.preferredHeight = 80f;

            UIRoundedRectangle background = buttonObject.GetComponent<UIRoundedRectangle>();
            background.color = option?.visualStyle != null
                ? option.visualStyle.ButtonColor
                : new Color(0.37f, 0.69f, 0.76f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            RectTransform content = CreateRect("Content", buttonRect);
            Stretch(content);
            HorizontalLayoutGroup horizontal = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.padding = new RectOffset(54, 42, 12, 12);
            horizontal.spacing = 24f;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            GameObject iconObject = new GameObject(
                "ICO_Option",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            MarkAsPreviewObject(iconObject);
            iconObject.transform.SetParent(content, false);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = option?.visualStyle != null ? option.visualStyle.Icon : null;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = 50f;
            iconLayout.preferredHeight = 50f;

            TMP_Text label = CreateText(
                "TXT_Label",
                content,
                option?.label ?? "Nueva opción",
                27f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            label.raycastTarget = false;

            GameObject borderObject = new GameObject(
                "InnerBorder",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIRoundedInnerBorder));
            MarkAsPreviewObject(borderObject);
            RectTransform borderRect = borderObject.GetComponent<RectTransform>();
            borderRect.SetParent(buttonRect, false);
            Stretch(borderRect);
            UIRoundedInnerBorder border = borderObject.GetComponent<UIRoundedInnerBorder>();
            border.color = new Color(1f, 1f, 1f, 0.6f);
            border.Thickness = 4f;

            if (Application.isPlaying)
            {
                string destination = option?.destinationSceneName;
                button.onClick.AddListener(() => Navigate(destination));
            }
        }

        private void BuildFooter(RectTransform parent)
        {
            if (string.IsNullOrWhiteSpace(footer))
            {
                return;
            }

            TMP_Text footerText = CreateText(
                "TXT_Footer",
                parent,
                footer,
                19f,
                FontStyles.Normal,
                TextAlignmentOptions.BottomRight);
            footerText.color = new Color(1f, 1f, 1f, 0.48f);
            footerText.rectTransform.anchorMin = new Vector2(1f, 0f);
            footerText.rectTransform.anchorMax = new Vector2(1f, 0f);
            footerText.rectTransform.pivot = new Vector2(1f, 0f);
            footerText.rectTransform.anchoredPosition = new Vector2(-42f, 30f);
            footerText.rectTransform.sizeDelta = new Vector2(820f, 38f);
        }

        private static void Navigate(string destinationSceneName)
        {
            if (string.IsNullOrWhiteSpace(destinationSceneName))
            {
                Debug.LogError("The navigation option has no destination scene.");
                return;
            }

            SceneNavigationService.Instance.NavigateTo(destinationSceneName);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(generatedRoot.transform, false);
        }

        private static GameObject CreatePanel(
            string objectName,
            RectTransform parent,
            Color color)
        {
            GameObject panel = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            MarkAsPreviewObject(panel);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect);
            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return panel;
        }

        private static RectTransform CreateRect(string objectName, RectTransform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            MarkAsPreviewObject(child);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static TMP_Text CreateText(
            string objectName,
            RectTransform parent,
            string value,
            float size,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            MarkAsPreviewObject(textObject);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void SetAnchoredRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ClearScreen()
        {
            if (generatedRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(generatedRoot);
            }
            else
            {
                DestroyImmediate(generatedRoot);
            }

            generatedRoot = null;
        }

        private static void MarkAsPreviewObject(GameObject target)
        {
            if (!Application.isPlaying)
            {
                target.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            dimOpacity = Mathf.Clamp01(dimOpacity);
            if (Application.isPlaying || rebuildScheduled)
            {
                return;
            }

            rebuildScheduled = true;
            EditorApplication.delayCall += RebuildPreviewDelayed;
        }

        private void RebuildPreviewDelayed()
        {
            rebuildScheduled = false;
            if (this != null && isActiveAndEnabled && !Application.isPlaying)
            {
                BuildScreen();
            }
        }
#endif
    }
}
