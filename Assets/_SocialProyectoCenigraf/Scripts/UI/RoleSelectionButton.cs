using TMPro;
using SocialProyectoCenigraf.Navigation;
using SocialProyectoCenigraf.Roles;
using SocialProyectoCenigraf.Session.State;
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
        [SerializeField] private Button button;

        [Header("Role")]
        [SerializeField] private RoleDefinition roleDefinition;

        [Header("Presentation")]
        [SerializeField] private Sprite icon;
        [SerializeField] private string title = "Seleccionar rol";
        [SerializeField] private Color backgroundColor =
            new Color(0.37f, 0.69f, 0.76f, 1f);

        private bool isLoadingScene;

        public RoleDefinition Role => roleDefinition;

        private void Reset()
        {
            backgroundImage = GetComponent<Image>();
            button = GetComponent<Button>();

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
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            button.onClick.AddListener(HandleClick);
            ApplyPresentation();
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void Configure(RoleDefinition role)
        {
            roleDefinition = role;
            ApplyPresentation();
        }

        public void ApplyPresentation()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = roleDefinition == null
                    ? backgroundColor
                    : roleDefinition.ButtonColor;
            }

            if (iconImage != null)
            {
                iconImage.sprite = roleDefinition == null
                    ? icon
                    : roleDefinition.Icon;
                // A null Sprite already renders nothing. Keeping the component
                // enabled makes icon changes visible immediately in Prefab Mode.
                iconImage.enabled = true;
                iconImage.preserveAspect = true;
            }

            if (titleText != null)
            {
                string visibleTitle = roleDefinition == null
                    ? title
                    : roleDefinition.DisplayName;
                titleText.text = string.IsNullOrWhiteSpace(visibleTitle)
                    ? "Seleccionar rol"
                    : visibleTitle;
            }
        }

        private void HandleClick()
        {
            if (isLoadingScene || roleDefinition == null)
            {
                return;
            }

            GameSessionStore.Instance.SelectRole(roleDefinition);

            InitialMenuFlowController menuFlow =
                FindFirstObjectByType<InitialMenuFlowController>();
            if (menuFlow != null && menuFlow.TryHandleRole(roleDefinition))
            {
                return;
            }

            string destination = roleDefinition.DestinationSceneName;
            if (string.IsNullOrWhiteSpace(destination) ||
                !Application.CanStreamedLevelBeLoaded(destination))
            {
                Debug.LogError(
                    $"Role '{roleDefinition.Id}' has an invalid destination scene.",
                    this);
                return;
            }

            isLoadingScene = true;
            button.interactable = false;
            SceneNavigationService.Instance.NavigateTo(destination);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyPresentation();
        }
#endif
    }
}
