using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    [AddComponentMenu("UI/Graphics/Rounded Rectangle")]
    [DisallowMultipleComponent]
    public sealed class UIRoundedRectangle : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float cornerRadius = 8f;
        [SerializeField, Range(1, 12)] private int cornerSegments = 5;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float radius = Mathf.Clamp(
                cornerRadius,
                0f,
                Mathf.Min(rect.width, rect.height) * 0.5f);
            List<Vector2> perimeter = BuildRoundedPerimeter(
                rect,
                radius,
                cornerSegments);

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = rect.center;
            vertexHelper.AddVert(vertex);

            for (int i = 0; i < perimeter.Count; i++)
            {
                vertex.position = perimeter[i];
                vertexHelper.AddVert(vertex);
            }

            for (int i = 0; i < perimeter.Count; i++)
            {
                int next = (i + 1) % perimeter.Count;
                vertexHelper.AddTriangle(0, i + 1, next + 1);
            }
        }

        private static List<Vector2> BuildRoundedPerimeter(
            Rect rect,
            float radius,
            int segments)
        {
            int safeSegments = Mathf.Max(1, segments);
            List<Vector2> points = new List<Vector2>((safeSegments + 1) * 4);
            Vector2[] centers =
            {
                new Vector2(rect.xMax - radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMin + radius),
                new Vector2(rect.xMax - radius, rect.yMin + radius)
            };
            float[] startAngles = { 0f, 90f, 180f, 270f };

            for (int corner = 0; corner < centers.Length; corner++)
            {
                for (int segment = 0; segment <= safeSegments; segment++)
                {
                    float radians = (
                        startAngles[corner] +
                        90f * segment / safeSegments) * Mathf.Deg2Rad;
                    points.Add(centers[corner] + new Vector2(
                        Mathf.Cos(radians),
                        Mathf.Sin(radians)) * radius);
                }
            }

            return points;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Clamp(cornerSegments, 1, 12);
            SetVerticesDirty();
        }
#endif
    }
}
