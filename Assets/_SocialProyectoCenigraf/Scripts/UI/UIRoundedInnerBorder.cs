using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    [ExecuteAlways]
    [AddComponentMenu("UI/Effects/Rounded Inner Border")]
    [DisallowMultipleComponent]
    public sealed class UIRoundedInnerBorder : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float thickness = 4f;
        [SerializeField, Min(0f)] private float cornerRadius = 8f;
        [SerializeField, Range(1, 12)] private int cornerSegments = 5;

        public float Thickness
        {
            get => thickness;
            set
            {
                thickness = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect outerRect = GetPixelAdjustedRect();
            float safeThickness = Mathf.Min(
                thickness,
                Mathf.Min(outerRect.width, outerRect.height) * 0.5f);
            if (safeThickness <= 0f || outerRect.width <= 0f || outerRect.height <= 0f)
            {
                return;
            }

            Rect innerRect = new Rect(
                outerRect.xMin + safeThickness,
                outerRect.yMin + safeThickness,
                outerRect.width - safeThickness * 2f,
                outerRect.height - safeThickness * 2f);

            float outerRadius = Mathf.Clamp(
                cornerRadius,
                0f,
                Mathf.Min(outerRect.width, outerRect.height) * 0.5f);
            float innerRadius = Mathf.Max(0f, outerRadius - safeThickness);

            List<Vector2> outerPoints = BuildRoundedPerimeter(
                outerRect,
                outerRadius,
                cornerSegments);
            List<Vector2> innerPoints = BuildRoundedPerimeter(
                innerRect,
                innerRadius,
                cornerSegments);

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            for (int i = 0; i < outerPoints.Count; i++)
            {
                int next = (i + 1) % outerPoints.Count;
                int firstVertex = vertexHelper.currentVertCount;

                vertex.position = outerPoints[i];
                vertexHelper.AddVert(vertex);
                vertex.position = outerPoints[next];
                vertexHelper.AddVert(vertex);
                vertex.position = innerPoints[next];
                vertexHelper.AddVert(vertex);
                vertex.position = innerPoints[i];
                vertexHelper.AddVert(vertex);

                vertexHelper.AddTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
                vertexHelper.AddTriangle(firstVertex, firstVertex + 2, firstVertex + 3);
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
                    float angle = startAngles[corner] +
                                  90f * segment / safeSegments;
                    float radians = angle * Mathf.Deg2Rad;
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
            thickness = Mathf.Max(0f, thickness);
            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Clamp(cornerSegments, 1, 12);
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
