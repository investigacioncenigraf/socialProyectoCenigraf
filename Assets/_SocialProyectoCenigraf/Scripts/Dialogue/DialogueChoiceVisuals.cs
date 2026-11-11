using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.Dialogue
{
    internal static class DialogueChoiceVisuals
    {
        private static Sprite pixelArrow;

        public static Image CreatePixelArrow(Transform parent, float size = 18f)
        {
            GameObject arrowObject = new GameObject(
                "PixelChoiceArrow",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            arrowObject.transform.SetParent(parent, false);

            RectTransform rect = arrowObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(12f, 0f);
            rect.sizeDelta = new Vector2(size, size);

            Image image = arrowObject.GetComponent<Image>();
            image.sprite = GetPixelArrow();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color32(226, 222, 238, 255);
            arrowObject.SetActive(false);
            return image;
        }

        public static void AddPointerEnter(Button button, Action onPointerEnter)
        {
            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            entry.callback.AddListener(_ => onPointerEnter?.Invoke());
            trigger.triggers.Add(entry);
        }

        private static Sprite GetPixelArrow()
        {
            if (pixelArrow != null) return pixelArrow;

            const int size = 9;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "DialoguePixelArrow",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 pixel = new Color32(255, 255, 255, 255);
            for (int y = 0; y < size; y++)
            {
                int rowWidth = y <= size / 2 ? y + 1 : size - y;
                for (int x = 0; x < size; x++)
                    texture.SetPixel(x, y, x < rowWidth ? pixel : clear);
            }
            texture.Apply(false, true);
            pixelArrow = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                9f);
            pixelArrow.name = "DialoguePixelArrow";
            return pixelArrow;
        }
    }
}
