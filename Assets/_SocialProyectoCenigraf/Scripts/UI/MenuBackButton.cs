using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    /// <summary>Shared upper-left return control for generated menus.</summary>
    internal static class MenuBackButton
    {
        public static Button Create(RectTransform parent, Sprite sprite, UnityAction onClick)
        {
            GameObject root = CreateObject("BTN_Back", parent,
                typeof(UIRoundedRectangle), typeof(Button));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(32f, -32f);
            rect.sizeDelta = new Vector2(80f, 80f);

            UIRoundedRectangle background = root.GetComponent<UIRoundedRectangle>();
            background.color = new Color(0.28f, 0.32f, 0.4f, 1f);
            Button button = root.GetComponent<Button>();
            button.targetGraphic = background;
            if (Application.isPlaying) button.onClick.AddListener(onClick);

            GameObject iconObject = CreateObject("ICO_Back", rect, typeof(Image));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(48f, 48f);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = sprite != null;

            if (sprite == null)
            {
                GameObject labelObject = CreateObject("TXT_BackPlaceholder", rect,
                    typeof(TextMeshProUGUI));
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
                label.text = "<";
                label.font = TMP_Settings.defaultFontAsset;
                label.fontSize = 36f;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }
            return button;
        }

        private static GameObject CreateObject(string name, Transform parent, params System.Type[] components)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            if (!Application.isPlaying)
                obj.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            obj.transform.SetParent(parent, false);
            foreach (System.Type component in components) obj.AddComponent(component);
            return obj;
        }
    }
}
