using UnityEngine;

namespace SocialProyectoCenigraf.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UISafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void OnEnable()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.safeArea != lastSafeArea || screenSize != lastScreenSize)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            int width = Mathf.Max(Screen.width, 1);
            int height = Mathf.Max(Screen.height, 1);
            Rect safeArea = Screen.safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= width;
            anchorMin.y /= height;
            anchorMax.x /= width;
            anchorMax.y /= height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(width, height);
        }
    }
}
