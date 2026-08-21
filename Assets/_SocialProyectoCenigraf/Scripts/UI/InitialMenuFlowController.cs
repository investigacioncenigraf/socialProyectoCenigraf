using SocialProyectoCenigraf.Navigation;
using SocialProyectoCenigraf.Roles;
using SocialProyectoCenigraf.Session.State;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    public sealed class InitialMenuFlowController : MonoBehaviour
    {
        private enum MenuView
        {
            RoleSelection,
            Administration,
            Publications,
            CreateEvent
        }

        [Header("Existing initial menu")]
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private GameObject header;
        [SerializeField] private GameObject rolePanel;
        [SerializeField] private RoleCatalog roleCatalog;

        [Header("Button styles")]
        [SerializeField] private RoleDefinition eventsVisualStyle;
        [SerializeField] private RoleDefinition gameVisualStyle;

        private MenuView activeView;
        private GameObject generatedPanel;
        private CreateEventFormView createEventForm;
        private EventPublicationListView publicationList;
        private RoleDefinition activeRole;
        private InputAction backAction;

        private void Awake()
        {
            backAction = new InputAction(
                "BackWithinInitialMenu",
                InputActionType.Button,
                "<Keyboard>/b");
            backAction.performed += HandleBack;
            ShowRoleSelection();
        }

        private void OnEnable()
        {
            backAction?.Enable();
        }

        private void OnDisable()
        {
            backAction?.Disable();
        }

        private void OnDestroy()
        {
            if (backAction != null)
            {
                backAction.performed -= HandleBack;
                backAction.Dispose();
                backAction = null;
            }
        }

        public bool TryHandleRole(RoleDefinition role)
        {
            if (role == null || role.EntryPoint != RoleEntryPoint.Administration)
            {
                return false;
            }

            activeRole = role;
            ShowAdministration();
            return true;
        }

        private void ShowRoleSelection()
        {
            activeView = MenuView.RoleSelection;
            ClearGeneratedPanel();
            SetInitialContentVisible(true);
        }

        private void ShowAdministration()
        {
            activeView = MenuView.Administration;
            SetInitialContentVisible(false);
            RectTransform panel = CreatePanel("AdministrationPanel");

            RoleDefinition selectedRole = GetSelectedRole();
            bool canCreatePublications =
                selectedRole != null && selectedRole.CanCreatePublications;

            CreateTitle(panel, "Menú Administrativo", 250f);

            RectTransform buttons = CreateButtonsContainer(panel, 176f);
            CreateNavigationButton(
                buttons,
                canCreatePublications
                    ? "Eventos"
                    : "Eventos — Solo administrador",
                eventsVisualStyle,
                ShowEvents,
                canCreatePublications);
            CreateNavigationButton(
                buttons,
                "Ir al juego",
                gameVisualStyle,
                () => SceneNavigationService.Instance.NavigateTo("SceneDemo"),
                true);
        }

        private void ShowEvents()
        {
            RoleDefinition selectedRole = GetSelectedRole();
            if (selectedRole == null || !selectedRole.CanCreatePublications)
            {
                return;
            }

            activeView = MenuView.Publications;
            SetInitialContentVisible(false);
            RectTransform panel = CreatePanel("PublicationListPanel");
            publicationList = panel.gameObject.AddComponent<
                EventPublicationListView>();
            publicationList.Initialize(
                selectedRole.CanCreatePublications,
                () => ShowCreateEvent(null),
                ShowCreateEvent);
        }

        private void ShowCreateEvent(
            SocialProyectoCenigraf.Events.EventPublication publication)
        {
            RoleDefinition selectedRole = GetSelectedRole();
            if (selectedRole == null || !selectedRole.CanCreatePublications)
            {
                return;
            }

            activeView = MenuView.CreateEvent;
            SetInitialContentVisible(false);
            RectTransform panel = CreatePanel("CreateEventPanel");
            createEventForm = panel.gameObject.AddComponent<CreateEventFormView>();
            createEventForm.Initialize(
                ShowEvents,
                selectedRole.CanCreatePublications,
                publication,
                ShowEvents);
        }

        private RoleDefinition GetSelectedRole()
        {
            if (activeRole != null)
            {
                return activeRole;
            }

            return roleCatalog == null
                ? null
                : roleCatalog.FindById(
                    GameSessionStore.Instance.State.SelectedRoleId);
        }

        private void HandleBack(InputAction.CallbackContext context)
        {
            GameObject selectedObject = EventSystem.current?.currentSelectedGameObject;
            if (selectedObject != null &&
                selectedObject.GetComponentInParent<TMP_InputField>() != null)
            {
                return;
            }

            if (activeView == MenuView.CreateEvent)
            {
                createEventForm?.RequestCancellation();
            }
            else if (activeView == MenuView.Publications)
            {
                if (publicationList != null && publicationList.TryHandleBack())
                {
                    return;
                }

                ShowAdministration();
            }
            else if (activeView == MenuView.Administration)
            {
                ShowRoleSelection();
            }
        }

        private RectTransform CreatePanel(string panelName)
        {
            ClearGeneratedPanel();

            generatedPanel = new GameObject(panelName, typeof(RectTransform));
            RectTransform rect = generatedPanel.GetComponent<RectTransform>();
            rect.SetParent(safeArea, false);
            Stretch(rect);
            rect.SetAsLastSibling();
            return rect;
        }

        private void CreateTitle(RectTransform parent, string value, float y)
        {
            TMP_Text title = CreateText(
                "TXT_MenuTitle",
                parent,
                value,
                58f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetAnchoredRect(
                title.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, y),
                new Vector2(1100f, 90f));
        }

        private static RectTransform CreateButtonsContainer(
            RectTransform parent,
            float height)
        {
            GameObject containerObject = new GameObject(
                "MenuOptions",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            RectTransform container = containerObject.GetComponent<RectTransform>();
            container.SetParent(parent, false);
            SetAnchoredRect(
                container,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f),
                new Vector2(500f, height));

            VerticalLayoutGroup layout = containerObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return container;
        }

        private static void CreateNavigationButton(
            RectTransform parent,
            string labelValue,
            RoleDefinition visualStyle,
            UnityEngine.Events.UnityAction action,
            bool interactable)
        {
            GameObject buttonObject = new GameObject(
                $"BTN_{labelValue.Replace(' ', '_')}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIRoundedRectangle),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 500f;
            layoutElement.preferredHeight = 80f;

            UIRoundedRectangle background =
                buttonObject.GetComponent<UIRoundedRectangle>();
            background.color = visualStyle != null
                ? visualStyle.ButtonColor
                : new Color(0.37f, 0.69f, 0.76f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(action);
            button.interactable = interactable;

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.48f, 0.50f, 0.56f, 0.62f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            RectTransform content = CreateRect("Content", buttonObject.transform);
            Stretch(content);
            HorizontalLayoutGroup horizontal =
                content.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.padding = new RectOffset(54, 42, 12, 12);
            horizontal.spacing = 24f;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            GameObject iconObject = new GameObject(
                "ICO_Option",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            iconObject.transform.SetParent(content, false);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = visualStyle != null ? visualStyle.Icon : null;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = 50f;
            iconLayout.preferredHeight = 50f;

            TMP_Text label = CreateText(
                "TXT_Label",
                content,
                labelValue,
                interactable ? 27f : 21f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            label.raycastTarget = false;

            GameObject borderObject = new GameObject(
                "InnerBorder",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIRoundedInnerBorder));
            RectTransform borderRect = borderObject.GetComponent<RectTransform>();
            borderRect.SetParent(buttonObject.transform, false);
            Stretch(borderRect);
            UIRoundedInnerBorder border = borderObject.GetComponent<UIRoundedInnerBorder>();
            border.color = new Color(1f, 1f, 1f, 0.6f);
            border.Thickness = 4f;
        }

        private void SetInitialContentVisible(bool visible)
        {
            if (header != null)
            {
                header.SetActive(visible);
            }

            if (rolePanel != null)
            {
                rolePanel.SetActive(visible);
            }
        }

        private void ClearGeneratedPanel()
        {
            if (generatedPanel != null)
            {
                Destroy(generatedPanel);
                generatedPanel = null;
            }

            createEventForm = null;
            publicationList = null;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static TMP_Text CreateText(
            string objectName,
            Transform parent,
            string value,
            float size,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void SetAnchoredRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
