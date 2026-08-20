using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public sealed class RoleSelectionButton : MonoBehaviour
    {
        [Header("Internal references")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;

        [Header("Presentation")]
        [SerializeField] private Sprite icon;
        [SerializeField] private string title = "Seleccionar rol";
        [SerializeField] private Color backgroundColor =
            new Color(0.37f, 0.69f, 0.76f, 1f);

        private void Reset()
        {
            backgroundImage = GetComponent<Image>();

            Transform content = transform.Find("Content");
            if (content != null)
            {
                iconImage = content.Find("ICO_Role")?.GetComponent<Image>();
                titleText = content.Find("TXT_Title")?.GetComponent<TMP_Text>();
            }

            ApplyPresentation();
        }

        private void Awake()
        {
            ApplyPresentation();
        }

        public void ApplyPresentation()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                // A null Sprite already renders nothing. Keeping the component
                // enabled makes icon changes visible immediately in Prefab Mode.
                iconImage.enabled = true;
                iconImage.preserveAspect = true;
            }

            if (titleText != null)
            {
                titleText.text = string.IsNullOrWhiteSpace(title)
                    ? "Seleccionar rol"
                    : title;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyPresentation();
        }
#endif
    }
}
