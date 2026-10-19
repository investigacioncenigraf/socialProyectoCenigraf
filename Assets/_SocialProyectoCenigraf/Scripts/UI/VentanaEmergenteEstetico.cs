using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class VentanaEmergenteEstetico : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup windowGroup;

        [Header("Initial state")]
        [SerializeField] private bool visibleAtStart;

        public bool IsVisible { get; private set; }

        public event Action<bool> VisibilityChanged;

        private void Reset()
        {
            windowGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            if (windowGroup == null)
            {
                windowGroup = GetComponent<CanvasGroup>();
            }

            SetVisible(visibleAtStart, false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.mKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            SetVisible(!IsVisible);
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            SetVisible(visible, true);
        }

        private void SetVisible(bool visible, bool notify)
        {
            IsVisible = visible;

            if (windowGroup != null)
            {
                windowGroup.alpha = visible ? 1f : 0f;
                windowGroup.interactable = visible;
                windowGroup.blocksRaycasts = visible;
            }

            if (notify)
            {
                VisibilityChanged?.Invoke(visible);
            }
        }
    }
}
