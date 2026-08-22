using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_InputField))]
    public sealed class InputFieldFocusVisual : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private static readonly Color NormalBackground =
            new Color(0.055f, 0.10f, 0.17f, 0.92f);
        private static readonly Color HoverBackground =
            new Color(0.075f, 0.14f, 0.23f, 0.98f);
        private static readonly Color FocusBackground =
            new Color(0.075f, 0.17f, 0.28f, 1f);
        private static readonly Color HoverBorder =
            new Color(0.40f, 0.68f, 0.95f, 0.48f);
        private static readonly Color FocusBorder =
            new Color(0.40f, 0.76f, 1f, 1f);

        private const float TransitionSpeed = 13f;

        private UIRoundedRectangle background;
        private UIRoundedInnerBorder border;
        private TMP_Text label;
        private bool pointerInside;
        private bool focused;

        public void Initialize(
            UIRoundedRectangle targetBackground,
            UIRoundedInnerBorder targetBorder,
            TMP_Text targetLabel)
        {
            background = targetBackground;
            border = targetBorder;
            label = targetLabel;
            ApplyImmediate();
        }

        private void Update()
        {
            if (background == null || border == null)
            {
                return;
            }

            Color targetBackground = focused
                ? FocusBackground
                : pointerInside ? HoverBackground : NormalBackground;
            Color targetBorder = focused
                ? FocusBorder
                : pointerInside ? HoverBorder : Color.clear;
            Color targetLabel = focused
                ? Color.white
                : new Color(1f, 1f, 1f, pointerInside ? 0.90f : 0.76f);

            float amount = 1f - Mathf.Exp(-TransitionSpeed * Time.unscaledDeltaTime);
            background.color = Color.Lerp(background.color, targetBackground, amount);
            border.color = Color.Lerp(border.color, targetBorder, amount);
            if (label != null)
            {
                label.color = Color.Lerp(label.color, targetLabel, amount);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            focused = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            focused = false;
        }

        private void ApplyImmediate()
        {
            if (background != null)
            {
                background.color = NormalBackground;
            }

            if (border != null)
            {
                border.color = Color.clear;
            }
        }
    }
}
