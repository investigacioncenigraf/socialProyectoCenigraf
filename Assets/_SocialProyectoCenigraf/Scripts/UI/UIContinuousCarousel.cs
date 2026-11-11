using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    public sealed class UIContinuousCarousel : MonoBehaviour
    {
        public enum HorizontalDirection
        {
            Left,
            Right
        }

        public enum ResponsiveLayout
        {
            PreserveCurrent,
            CoverParent,
            BottomCoverParent
        }

        [Header("Images")]
        [SerializeField] private RawImage primaryImage;
        [SerializeField] private RawImage continuationImage;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float speed = 20f;
        [SerializeField] private HorizontalDirection direction =
            HorizontalDirection.Left;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Responsive Layout")]
        [SerializeField] private ResponsiveLayout responsiveLayout =
            ResponsiveLayout.PreserveCurrent;
        [SerializeField, Range(0.1f, 1f)] private float minimumHeightRatio =
            0.42f;

        [Header("Seam")]
        [SerializeField, Range(0f, 4f)] private float seamOverlap = 1f;

        private Vector2 basePosition;
        private float distance;
        private float cachedStep;
        private Vector2 cachedParentSize;
        private bool isRefreshingLayout;

        public RawImage PrimaryImage => primaryImage;
        public RawImage ContinuationImage => continuationImage;

        public float Speed
        {
            get => speed;
            set => speed = Mathf.Max(0f, value);
        }

        public void Configure(
            RawImage primary,
            RawImage continuation,
            float pixelsPerSecond,
            ResponsiveLayout layout,
            float heightRatio = 0.42f)
        {
            primaryImage = primary;
            continuationImage = continuation;
            speed = Mathf.Max(0f, pixelsPerSecond);
            responsiveLayout = layout;
            minimumHeightRatio = Mathf.Clamp(heightRatio, 0.1f, 1f);
            RefreshLayout(true);
        }

        private void OnEnable()
        {
            RefreshLayout(true);
        }

        private void Update()
        {
            if (!Application.isPlaying || speed <= 0f || !IsReady())
            {
                return;
            }

            RefreshIfParentChanged();

            float deltaTime = useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            float step = GetStep();
            if (step <= Mathf.Epsilon)
            {
                return;
            }

            distance = Mathf.Repeat(distance + speed * deltaTime, step);
            ApplyPositions();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!IsReady() || isRefreshingLayout)
            {
                return;
            }

            RefreshLayout(false);
        }

        private void RefreshIfParentChanged()
        {
            if (!IsReady())
            {
                return;
            }

            RectTransform parent = primaryImage.rectTransform.parent
                as RectTransform;
            if (parent != null &&
                (parent.rect.size - cachedParentSize).sqrMagnitude > 0.01f)
            {
                RefreshLayout(false);
            }
        }

        private void RefreshLayout(bool resetDistance)
        {
            if (!IsReady() || isRefreshingLayout)
            {
                return;
            }

            isRefreshingLayout = true;

            RectTransform primaryRect = primaryImage.rectTransform;
            RectTransform continuationRect = continuationImage.rectTransform;
            RectTransform parent = primaryRect.parent as RectTransform;

            if (parent != null)
            {
                cachedParentSize = parent.rect.size;
                ApplyResponsiveLayout(
                    primaryRect,
                    continuationRect,
                    parent.rect.size);
            }

            basePosition = primaryRect.anchoredPosition;
            cachedStep = GetStep();
            if (resetDistance)
            {
                distance = 0f;
            }
            else
            {
                distance = Mathf.Repeat(
                    distance,
                    Mathf.Max(cachedStep, 0.001f));
            }

            CopyVisualSettings();
            ApplyPositions();
            isRefreshingLayout = false;
        }

        private void ApplyResponsiveLayout(
            RectTransform primaryRect,
            RectTransform continuationRect,
            Vector2 parentSize)
        {
            if (responsiveLayout == ResponsiveLayout.PreserveCurrent ||
                primaryImage.texture == null ||
                parentSize.x <= 0f ||
                parentSize.y <= 0f)
            {
                return;
            }

            float textureWidth = primaryImage.texture.width;
            float textureHeight = primaryImage.texture.height;
            float aspect = textureHeight > 0f
                ? textureWidth / textureHeight
                : 1f;

            Vector2 tileSize;
            Vector2 anchor;
            Vector2 pivot;

            if (responsiveLayout == ResponsiveLayout.CoverParent)
            {
                tileSize = GetCoverSize(parentSize, aspect);
                anchor = new Vector2(0.5f, 0.5f);
                pivot = new Vector2(0.5f, 0.5f);
            }
            else
            {
                float targetHeight = parentSize.y * minimumHeightRatio;
                float widthFromHeight = targetHeight * aspect;
                float width = Mathf.Max(parentSize.x, widthFromHeight);
                float height = width / Mathf.Max(aspect, 0.001f);
                tileSize = new Vector2(width, height);
                anchor = new Vector2(0.5f, 0f);
                pivot = new Vector2(0.5f, 0f);
            }

            ConfigureRect(primaryRect, anchor, pivot, tileSize);
            ConfigureRect(continuationRect, anchor, pivot, tileSize);
        }

        private static Vector2 GetCoverSize(Vector2 area, float aspect)
        {
            float areaAspect = area.x / Mathf.Max(area.y, 0.001f);
            if (aspect >= areaAspect)
            {
                return new Vector2(area.y * aspect, area.y);
            }

            return new Vector2(area.x, area.x / Mathf.Max(aspect, 0.001f));
        }

        private static void ConfigureRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private void CopyVisualSettings()
        {
            continuationImage.texture = primaryImage.texture;
            continuationImage.material = primaryImage.material;
            continuationImage.color = primaryImage.color;
            continuationImage.uvRect = primaryImage.uvRect;
            continuationImage.raycastTarget = false;
        }

        private void ApplyPositions()
        {
            if (!IsReady())
            {
                return;
            }

            float step = GetStep();
            if (step <= Mathf.Epsilon)
            {
                return;
            }

            float signedDistance = direction == HorizontalDirection.Left
                ? -distance
                : distance;
            float continuationOffset = direction == HorizontalDirection.Left
                ? step
                : -step;

            primaryImage.rectTransform.anchoredPosition =
                basePosition + Vector2.right * signedDistance;
            continuationImage.rectTransform.anchoredPosition =
                basePosition + Vector2.right *
                (signedDistance + continuationOffset);
        }

        private float GetStep()
        {
            if (primaryImage == null)
            {
                return 0f;
            }

            return Mathf.Max(
                0f,
                primaryImage.rectTransform.rect.width - seamOverlap);
        }

        private bool IsReady()
        {
            return primaryImage != null && continuationImage != null;
        }
    }
}
