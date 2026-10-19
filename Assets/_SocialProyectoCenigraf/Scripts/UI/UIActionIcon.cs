using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    public enum UIActionIconType
    {
        Edit = 0,
        Delete = 1
    }

    [AddComponentMenu("UI/Graphics/Action Icon")]
    [DisallowMultipleComponent]
    public sealed class UIActionIcon : MaskableGraphic
    {
        [SerializeField] private UIActionIconType iconType;

        public UIActionIconType IconType
        {
            get => iconType;
            set
            {
                iconType = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            float scale = Mathf.Min(rect.width, rect.height) / 32f;
            Vector2 center = rect.center;

            if (iconType == UIActionIconType.Edit)
            {
                DrawEdit(vertexHelper, center, scale);
            }
            else
            {
                DrawDelete(vertexHelper, center, scale);
            }
        }

        private void DrawEdit(VertexHelper helper, Vector2 center, float scale)
        {
            Vector2 direction = new Vector2(0.7071f, 0.7071f);
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Vector2 start = center - direction * 9f * scale;
            Vector2 end = center + direction * 8f * scale;
            float halfWidth = 3.2f * scale;

            AddQuad(
                helper,
                start + normal * halfWidth,
                end + normal * halfWidth,
                end - normal * halfWidth,
                start - normal * halfWidth);
            AddTriangle(
                helper,
                end + normal * halfWidth,
                end + direction * 5f * scale,
                end - normal * halfWidth);

            Vector2 eraserStart = start - direction * 3f * scale;
            AddQuad(
                helper,
                eraserStart + normal * halfWidth,
                start + normal * halfWidth,
                start - normal * halfWidth,
                eraserStart - normal * halfWidth);
        }

        private void DrawDelete(VertexHelper helper, Vector2 center, float scale)
        {
            float thickness = 2.2f * scale;
            float left = center.x - 8f * scale;
            float right = center.x + 8f * scale;
            float top = center.y + 7f * scale;
            float bottom = center.y - 10f * scale;

            AddRect(helper, left, top, right, top + thickness);
            AddRect(
                helper,
                center.x - 4f * scale,
                top + thickness,
                center.x + 4f * scale,
                top + thickness * 2f);
            AddRect(helper, left + scale, bottom, left + scale + thickness, top);
            AddRect(helper, right - scale - thickness, bottom, right - scale, top);
            AddRect(helper, left + scale, bottom, right - scale, bottom + thickness);
            AddRect(
                helper,
                center.x - 3.2f * scale,
                bottom + 4f * scale,
                center.x - 1.3f * scale,
                top - 3f * scale);
            AddRect(
                helper,
                center.x + 1.3f * scale,
                bottom + 4f * scale,
                center.x + 3.2f * scale,
                top - 3f * scale);
        }

        private void AddRect(
            VertexHelper helper,
            float left,
            float bottom,
            float right,
            float top)
        {
            AddQuad(
                helper,
                new Vector2(left, top),
                new Vector2(right, top),
                new Vector2(right, bottom),
                new Vector2(left, bottom));
        }

        private void AddQuad(
            VertexHelper helper,
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d)
        {
            int first = helper.currentVertCount;
            AddVertex(helper, a);
            AddVertex(helper, b);
            AddVertex(helper, c);
            AddVertex(helper, d);
            helper.AddTriangle(first, first + 1, first + 2);
            helper.AddTriangle(first, first + 2, first + 3);
        }

        private void AddTriangle(
            VertexHelper helper,
            Vector2 a,
            Vector2 b,
            Vector2 c)
        {
            int first = helper.currentVertCount;
            AddVertex(helper, a);
            AddVertex(helper, b);
            AddVertex(helper, c);
            helper.AddTriangle(first, first + 1, first + 2);
        }

        private void AddVertex(VertexHelper helper, Vector2 position)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = position;
            helper.AddVert(vertex);
        }
    }
}
