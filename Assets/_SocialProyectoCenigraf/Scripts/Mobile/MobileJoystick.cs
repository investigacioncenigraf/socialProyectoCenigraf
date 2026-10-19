using UnityEngine;
using UnityEngine.EventSystems;

namespace SocialProyectoCenigraf.Mobile
{
    public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        public RectTransform Handle { get; set; }
        public Vector2 Value { get; private set; }
        private int? activePointer;
        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;
        public void OnPointerDown(PointerEventData data)
        {
            if (activePointer.HasValue) return;
            activePointer = data.pointerId;
            OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (activePointer != data.pointerId) return;
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out var point)) return;
            float radius = (rect.rect.width - Handle.rect.width) * 0.5f;
            Vector2 offset = Vector2.ClampMagnitude(point - rect.rect.center, radius);
            Handle.anchoredPosition = offset;
            Value = offset.magnitude < radius * 0.12f ? Vector2.zero : offset / radius;
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (activePointer == data.pointerId) ResetInput();
        }
        public void ResetInput()
        {
            activePointer = null; Value = Vector2.zero;
            if (Handle != null) Handle.anchoredPosition = Vector2.zero;
        }
        private void OnDisable() => ResetInput();
        private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
        private void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
    }
}
