using System;
using System.Globalization;
using SocialProyectoCenigraf.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    public sealed class EventPublicationListView : MonoBehaviour
    {
        private const int PageSize = 4;

        private VolatileEventPublicationRepository repository;
        private Action createRequested;
        private Action<EventPublication> editRequested;
        private bool canManagePublications;
        private RectTransform rowsContainer;
        private TMP_Text emptyState;
        private TMP_Text pageLabel;
        private Button previousButton;
        private Button nextButton;
        private GameObject deleteModal;
        private EventPublication pendingDeletion;
        private int currentPage;

        public void Initialize(
            bool allowManagement,
            Action onCreateRequested,
            Action<EventPublication> onEditRequested)
        {
            canManagePublications = allowManagement;
            createRequested = onCreateRequested;
            editRequested = onEditRequested;
            repository = VolatileEventPublicationRepository.Instance;
            repository.PublicationsChanged += Refresh;
            BuildView();
            Refresh();
        }

        public bool TryHandleBack()
        {
            if (deleteModal == null)
            {
                return false;
            }

            CloseDeleteModal();
            return true;
        }

        private void OnDestroy()
        {
            if (repository != null)
            {
                repository.PublicationsChanged -= Refresh;
            }
        }

        private void BuildView()
        {
            RectTransform root = (RectTransform)transform;

            TMP_Text title = CreateText(
                "TXT_AdminTitle",
                root,
                "Menú Administrativo",
                58f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetAnchoredRect(
                title.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 270f),
                new Vector2(1100f, 80f));

            TMP_Text subtitle = CreateText(
                "TXT_PublicationListTitle",
                root,
                "Lista de Publicaciones",
                42f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            SetAnchoredRect(
                subtitle.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 210f),
                new Vector2(900f, 62f));

            Button createButton = CreateButton(
                "BTN_CreateEvent",
                root,
                "Crear Evento",
                new Color(0.79f, 0.40f, 0.41f, 1f),
                new Vector2(-385f, 145f),
                new Vector2(330f, 76f),
                () => createRequested?.Invoke());
            createButton.interactable = canManagePublications;
            AddBorder(
                createButton.GetComponent<RectTransform>(),
                new Color(1f, 1f, 1f, 0.60f),
                4f);

            rowsContainer = CreateRect("PublicationRows", root);
            SetAnchoredRect(
                rowsContainer,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -70f),
                new Vector2(1120f, 370f));

            emptyState = CreateText(
                "TXT_EmptyPublications",
                rowsContainer,
                "Todavía no existen publicaciones.\nCrea el primer evento para comenzar.",
                25f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            emptyState.color = new Color(1f, 1f, 1f, 0.68f);
            Stretch(emptyState.rectTransform);

            previousButton = CreateButton(
                "BTN_PreviousPage",
                root,
                "<",
                new Color(0.23f, 0.40f, 0.62f, 1f),
                new Vector2(-85f, -315f),
                new Vector2(54f, 48f),
                PreviousPage);
            nextButton = CreateButton(
                "BTN_NextPage",
                root,
                ">",
                new Color(0.23f, 0.40f, 0.62f, 1f),
                new Vector2(85f, -315f),
                new Vector2(54f, 48f),
                NextPage);

            pageLabel = CreateText(
                "TXT_Page",
                root,
                string.Empty,
                20f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetAnchoredRect(
                pageLabel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -315f),
                new Vector2(100f, 45f));
        }

        private void Refresh()
        {
            if (rowsContainer == null || repository == null)
            {
                return;
            }

            for (int index = rowsContainer.childCount - 1; index >= 0; index--)
            {
                Transform child = rowsContainer.GetChild(index);
                if (child.gameObject != emptyState.gameObject)
                {
                    Destroy(child.gameObject);
                }
            }

            int count = repository.Publications.Count;
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(count / (float)PageSize));
            currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);
            emptyState.gameObject.SetActive(count == 0);

            int firstNewestIndex = count - 1 - currentPage * PageSize;
            for (int row = 0; row < PageSize; row++)
            {
                int publicationIndex = firstNewestIndex - row;
                if (publicationIndex < 0)
                {
                    break;
                }

                CreatePublicationRow(
                    rowsContainer,
                    repository.Publications[publicationIndex],
                    135f - row * 90f);
            }

            previousButton.interactable = currentPage > 0;
            nextButton.interactable = currentPage < pageCount - 1;
            pageLabel.text = $"{currentPage + 1} / {pageCount}";
        }

        private void CreatePublicationRow(
            RectTransform parent,
            EventPublication publication,
            float y)
        {
            RectTransform row = CreateRoundedPanel(
                $"Publication_{publication.Id}",
                parent,
                new Color(0.10f, 0.19f, 0.31f, 0.98f),
                new Vector2(1100f, 76f),
                new Vector2(0f, y));
            AddBorder(row, new Color(0.20f, 0.43f, 0.76f, 0.95f), 3f);

            TMP_Text title = CreateText(
                "TXT_Title",
                row,
                $"<color=#39D7ED>●</color>  {publication.Title}",
                22f,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            title.color = Color.white;
            SetAnchoredRect(
                title.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(-305f, 0f),
                new Vector2(430f, 52f));

            CreateDateColumn(
                row,
                "Inicio",
                publication.PublicationStartsAt,
                new Vector2(105f, 0f));
            CreateDateColumn(
                row,
                "Fin",
                publication.PublicationEndsAt,
                new Vector2(230f, 0f));

            Button edit = CreateIconButton(
                "BTN_Edit",
                row,
                UIActionIconType.Edit,
                new Color(0.18f, 0.34f, 0.53f, 1f),
                Color.white,
                new Vector2(440f, 0f),
                () => editRequested?.Invoke(publication));
            edit.interactable = canManagePublications;

            Button delete = CreateIconButton(
                "BTN_Delete",
                row,
                UIActionIconType.Delete,
                new Color(0.28f, 0.13f, 0.18f, 1f),
                new Color(1f, 0.34f, 0.38f, 1f),
                new Vector2(505f, 0f),
                () => RequestDelete(publication));
            delete.interactable = canManagePublications;
        }

        private static Button CreateIconButton(
            string objectName,
            RectTransform parent,
            UIActionIconType iconType,
            Color backgroundColor,
            Color iconColor,
            Vector2 position,
            UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIRoundedRectangle),
                typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetAnchoredRect(
                rect,
                new Vector2(0.5f, 0.5f),
                position,
                new Vector2(52f, 48f));

            UIRoundedRectangle background =
                buttonObject.GetComponent<UIRoundedRectangle>();
            background.color = backgroundColor;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(action);

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.90f);
            colors.pressedColor = new Color(0.70f, 0.70f, 0.70f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.40f, 0.42f, 0.47f, 0.45f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            GameObject iconObject = new GameObject(
                "ICO_Action",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIActionIcon));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(rect, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(28f, 28f);

            UIActionIcon icon = iconObject.GetComponent<UIActionIcon>();
            icon.IconType = iconType;
            icon.color = iconColor;
            icon.raycastTarget = false;
            return button;
        }

        private static void CreateDateColumn(
            RectTransform parent,
            string heading,
            DateTime date,
            Vector2 position)
        {
            TMP_Text text = CreateText(
                $"TXT_{heading}",
                parent,
                $"{heading}\n{date.ToString(EventPublicationValidator.DateFormat, CultureInfo.InvariantCulture)}",
                16f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            text.color = new Color(1f, 1f, 1f, 0.76f);
            SetAnchoredRect(
                text.rectTransform,
                new Vector2(0.5f, 0.5f),
                position,
                new Vector2(120f, 58f));
        }

        private void PreviousPage()
        {
            currentPage--;
            Refresh();
        }

        private void NextPage()
        {
            currentPage++;
            Refresh();
        }

        private void RequestDelete(EventPublication publication)
        {
            if (!canManagePublications || deleteModal != null)
            {
                return;
            }

            pendingDeletion = publication;
            deleteModal = new GameObject(
                "DeletePublicationModal",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform overlay = deleteModal.GetComponent<RectTransform>();
            overlay.SetParent(transform, false);
            Stretch(overlay);
            Image dim = deleteModal.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            RectTransform dialog = CreateRoundedPanel(
                "Dialog",
                overlay,
                new Color(0.11f, 0.16f, 0.25f, 1f),
                new Vector2(650f, 300f),
                Vector2.zero);
            AddBorder(dialog, new Color(1f, 0.42f, 0.45f, 0.75f), 3f);

            TMP_Text title = CreateText(
                "TXT_DeleteTitle",
                dialog,
                "¿Eliminar esta publicación?",
                30f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetAnchoredRect(
                title.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 78f),
                new Vector2(570f, 55f));

            TMP_Text message = CreateText(
                "TXT_DeleteMessage",
                dialog,
                publication.Title + "\nEsta acción no se puede deshacer durante la ejecución.",
                20f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            message.color = new Color(1f, 1f, 1f, 0.72f);
            SetAnchoredRect(
                message.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f),
                new Vector2(570f, 65f));

            CreateButton(
                "BTN_KeepPublication",
                dialog,
                "Conservar",
                new Color(0.28f, 0.43f, 0.65f, 1f),
                new Vector2(-145f, -82f),
                new Vector2(260f, 62f),
                CloseDeleteModal);
            CreateButton(
                "BTN_ConfirmDelete",
                dialog,
                "Sí, eliminar",
                new Color(0.76f, 0.29f, 0.32f, 1f),
                new Vector2(145f, -82f),
                new Vector2(260f, 62f),
                ConfirmDelete);
        }

        private void ConfirmDelete()
        {
            if (pendingDeletion != null)
            {
                repository.Remove(pendingDeletion.Id);
            }

            CloseDeleteModal();
        }

        private void CloseDeleteModal()
        {
            pendingDeletion = null;
            if (deleteModal != null)
            {
                Destroy(deleteModal);
                deleteModal = null;
            }
        }

        private static Button CreateButton(
            string objectName,
            RectTransform parent,
            string labelValue,
            Color color,
            Vector2 position,
            Vector2 size,
            UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIRoundedRectangle),
                typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetAnchoredRect(rect, new Vector2(0.5f, 0.5f), position, size);

            UIRoundedRectangle background =
                buttonObject.GetComponent<UIRoundedRectangle>();
            background.color = color;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.45f, 0.47f, 0.52f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TMP_Text label = CreateText(
                "TXT_Label",
                rect,
                labelValue,
                22f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.raycastTarget = false;
            return button;
        }

        private static RectTransform CreateRoundedPanel(
            string objectName,
            RectTransform parent,
            Color color,
            Vector2 size,
            Vector2 position)
        {
            GameObject panelObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIRoundedRectangle));
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetAnchoredRect(rect, new Vector2(0.5f, 0.5f), position, size);
            panelObject.GetComponent<UIRoundedRectangle>().color = color;
            return rect;
        }

        private static void AddBorder(RectTransform target, Color color, float thickness)
        {
            GameObject borderObject = new GameObject(
                "InnerBorder",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIRoundedInnerBorder));
            RectTransform rect = borderObject.GetComponent<RectTransform>();
            rect.SetParent(target, false);
            Stretch(rect);
            UIRoundedInnerBorder border = borderObject.GetComponent<UIRoundedInnerBorder>();
            border.color = color;
            border.Thickness = thickness;
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
