using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class LoginBackgroundBlur : MonoBehaviour
    {
        [SerializeField] private RawImage sourceSky;
        [SerializeField] private RawImage sourceSkyContinuation;
        [SerializeField] private RawImage sourceLandscape;
        [SerializeField] private RawImage sourceLandscapeContinuation;
        [SerializeField] private RawImage blurredSky;
        [SerializeField] private RawImage blurredSkyContinuation;
        [SerializeField] private RawImage blurredLandscape;
        [SerializeField] private RawImage blurredLandscapeContinuation;

        private readonly Vector3[] worldCorners = new Vector3[4];

        public void Configure(
            RawImage skySource,
            RawImage skySourceContinuation,
            RawImage landscapeSource,
            RawImage landscapeSourceContinuation,
            RawImage skyBlur,
            RawImage skyBlurContinuation,
            RawImage landscapeBlur,
            RawImage landscapeBlurContinuation)
        {
            sourceSky = skySource;
            sourceSkyContinuation = skySourceContinuation;
            sourceLandscape = landscapeSource;
            sourceLandscapeContinuation = landscapeSourceContinuation;
            blurredSky = skyBlur;
            blurredSkyContinuation = skyBlurContinuation;
            blurredLandscape = landscapeBlur;
            blurredLandscapeContinuation = landscapeBlurContinuation;
            SynchronizeLayers();
        }

        private void LateUpdate()
        {
            SynchronizeLayers();
        }

        private void OnValidate()
        {
            SynchronizeLayers();
        }

        private void SynchronizeLayers()
        {
            SynchronizeLayer(sourceSky, blurredSky);
            SynchronizeLayer(
                sourceSkyContinuation,
                blurredSkyContinuation);
            SynchronizeLayer(sourceLandscape, blurredLandscape);
            SynchronizeLayer(
                sourceLandscapeContinuation,
                blurredLandscapeContinuation);
        }

        private void SynchronizeLayer(RawImage source, RawImage target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.texture = source.texture;
            target.uvRect = source.uvRect;
            target.color = source.color;

            RectTransform sourceRect = source.rectTransform;
            RectTransform targetRect = target.rectTransform;
            RectTransform blurRoot = (RectTransform)transform;

            sourceRect.GetWorldCorners(worldCorners);
            Vector3 bottomLeft = blurRoot.InverseTransformPoint(worldCorners[0]);
            Vector3 topRight = blurRoot.InverseTransformPoint(worldCorners[2]);
            Vector3 center = (bottomLeft + topRight) * 0.5f;

            targetRect.anchorMin = new Vector2(0.5f, 0.5f);
            targetRect.anchorMax = new Vector2(0.5f, 0.5f);
            targetRect.pivot = new Vector2(0.5f, 0.5f);
            targetRect.localPosition = new Vector3(center.x, center.y, 0f);
            targetRect.sizeDelta = new Vector2(
                Mathf.Abs(topRight.x - bottomLeft.x),
                Mathf.Abs(topRight.y - bottomLeft.y));
        }
    }
}
