using UnityEngine;

namespace SocialProyectoCenigraf.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class GameplayHUDResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform playerSummary;
        [SerializeField] private RectTransform menuButton;
        [SerializeField] private RectTransform chatGroup;

        [Header("Responsive limits")]
        [SerializeField, Range(0.45f, 1f)] private float minimumScale = 0.62f;
        [SerializeField, Min(0f)] private float horizontalGap = 18f;
        [SerializeField, Min(0f)] private float verticalGap = 24f;

        private RectTransform layoutRoot;
        private Vector2 lastSize;

        public void Configure(
            RectTransform summary,
            RectTransform targetMenuButton,
            RectTransform targetChatGroup)
        {
            playerSummary = summary;
            menuButton = targetMenuButton;
            chatGroup = targetChatGroup;
            ApplyLayout();
        }

        private void OnEnable()
        {
            layoutRoot = GetComponent<RectTransform>();
            ApplyLayout();
        }

        private void Update()
        {
            if (layoutRoot == null)
            {
                layoutRoot = GetComponent<RectTransform>();
            }

            Vector2 currentSize = layoutRoot.rect.size;
            if ((currentSize - lastSize).sqrMagnitude > 0.01f)
            {
                ApplyLayout();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            if (playerSummary == null || menuButton == null || chatGroup == null)
            {
                return;
            }

            if (layoutRoot == null)
            {
                layoutRoot = GetComponent<RectTransform>();
            }

            Vector2 available = layoutRoot.rect.size;
            if (available.x <= 0f || available.y <= 0f)
            {
                return;
            }

            float topRequiredWidth = playerSummary.rect.width +
                menuButton.rect.width + horizontalGap;
            float topScale = Mathf.Clamp(
                available.x / Mathf.Max(topRequiredWidth, 1f),
                minimumScale,
                1f);

            float widthScale = available.x /
                Mathf.Max(chatGroup.rect.width, 1f);
            float remainingHeight = available.y -
                playerSummary.rect.height * topScale - verticalGap;
            float heightScale = remainingHeight /
                Mathf.Max(chatGroup.rect.height, 1f);
            float chatScale = Mathf.Clamp(
                Mathf.Min(widthScale, heightScale),
                minimumScale,
                1f);

            playerSummary.localScale = Vector3.one * topScale;
            menuButton.localScale = Vector3.one * topScale;
            chatGroup.localScale = Vector3.one * chatScale;
            lastSize = available;
        }
    }
}
