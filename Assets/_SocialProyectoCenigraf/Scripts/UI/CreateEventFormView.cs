using System;
using System.Globalization;
using System.IO;
using SocialProyectoCenigraf.Events;
using SocialProyectoCenigraf.Session.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    public sealed class CreateEventFormView : MonoBehaviour
    {
        private static readonly Color CardColor =
            new Color(0.10f, 0.19f, 0.31f, 0.97f);
        private static readonly Color InputColor =
            new Color(0.055f, 0.10f, 0.17f, 0.92f);
        private static readonly Color PrimaryColor =
            new Color(0.40f, 0.56f, 0.82f, 1f);
        private static readonly Color DangerColor =
            new Color(0.79f, 0.40f, 0.41f, 1f);

        private Action cancelConfirmed;
        private TMP_InputField titleInput;
        private TMP_InputField descriptionInput;
        private TMP_InputField linkInput;
        private TMP_InputField startDateInput;
        private TMP_InputField endDateInput;
        private TMP_Text imageFileText;
        private TMP_Text statusText;
        private GameObject confirmationModal;
        private string selectedImagePath = string.Empty;
        private Texture2D previewTexture;
        private Sprite previewSprite;
        private Image previewImage;
        private bool canCreatePublications;
        private CalendarPopupView calendarPopup;
        private EventPublication editingPublication;
        private Action saveConfirmed;

        public void Initialize(
            Action onCancelConfirmed,
            bool allowPublicationCreation,
            EventPublication publicationToEdit = null,
            Action onSaveConfirmed = null)
        {
            cancelConfirmed = onCancelConfirmed;
            canCreatePublications = allowPublicationCreation;
            editingPublication = publicationToEdit;
            saveConfirmed = onSaveConfirmed;
            BuildView();
            PopulateFormForEditing();
        }

        public void RequestCancellation()
        {
            if (calendarPopup != null)
            {
                calendarPopup.Dismiss();
                return;
            }

            ShowCancelConfirmation();
        }

        private void OnDestroy()
        {
            DestroyPreviewResources();
        }

        private void BuildView()
        {
            RectTransform root = (RectTransform)transform;

            TMP_Text mainTitle = CreateText(
                "TXT_AdminTitle",
                root,
                "Menú Administrativo",
                58f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetAnchoredRect(
                mainTitle.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 255f),
                new Vector2(1100f, 80f));

            TMP_Text subtitle = CreateText(
                "TXT_CreateEvent",
                root,
                editingPublication == null ? "Crear Evento" : "Modificar Evento",
                42f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            SetAnchoredRect(
                subtitle.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 195f),
                new Vector2(800f, 65f));

            RectTransform card = CreateRoundedPanel(
                "EventFormCard",
                root,
                CardColor,
                new Vector2(1100f, 410f),
                new Vector2(0f, -35f));
            AddBorder(card, new Color(0.18f, 0.39f, 0.68f, 0.95f), 3f);

            titleInput = CreateField(card, "Title", "Título del evento", 150f, false);
            descriptionInput = CreateField(
                card,
                "Description",
                "Descripción del evento",
                90f,
                false);
            linkInput = CreateField(
                card,
                "Link",
                "Link (opcional)",
                30f,
                false);
            startDateInput = CreateField(
                card,
                "StartDate",
                "Inicio de publicación",
                -30f,
                false,
                "dd/MM/yyyy");
            endDateInput = CreateField(
                card,
                "EndDate",
                "Final de publicación",
                -90f,
                false,
                "dd/MM/yyyy");

            ConfigureDateInput(startDateInput, true);
            ConfigureDateInput(endDateInput, false);

            BuildImagePicker(card, -150f);

            CreateActionButton(
                root,
                "BTN_SaveEvent",
                editingPublication == null
                    ? "Guardar Evento"
                    : "Guardar Cambios",
                PrimaryColor,
                new Vector2(-175f, -280f),
                HandleSave);
            CreateActionButton(
                root,
                "BTN_CancelEvent",
                "Cancelar",
                DangerColor,
                new Vector2(175f, -280f),
                ShowCancelConfirmation);

            statusText = CreateText(
                "TXT_FormStatus",
                root,
                string.Empty,
                20f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            SetAnchoredRect(
                statusText.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -340f),
                new Vector2(1050f, 38f));
        }

        private TMP_InputField CreateField(
            RectTransform card,
            string fieldName,
            string labelValue,
            float y,
            bool multiline,
            string placeholderValue = "Escribe aquí")
        {
            TMP_Text label = CreateText(
                $"LBL_{fieldName}",
                card,
                labelValue,
                24f,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            label.color = new Color(1f, 1f, 1f, 0.76f);
            SetAnchoredRect(
                label.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(-350f, y),
                new Vector2(315f, 48f));

            TMP_InputField input = CreateInputField(
                $"INP_{fieldName}",
                card,
                placeholderValue,
                multiline,
                label);
            SetAnchoredRect(
                input.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(150f, y),
                new Vector2(650f, 50f));
            return input;
        }

        private void BuildImagePicker(RectTransform card, float y)
        {
            TMP_Text label = CreateText(
                "LBL_Image",
                card,
                "Imagen",
                24f,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            label.color = new Color(1f, 1f, 1f, 0.76f);
            SetAnchoredRect(
                label.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(-350f, y),
                new Vector2(315f, 48f));

            GameObject pickerObject = CreateSmallButton(
                card,
                "BTN_SelectImage",
                "Cargar imagen",
                new Vector2(-10f, y),
                new Vector2(260f, 48f),
                HandleSelectImage);

            previewImage = pickerObject.transform.Find("Preview")?.GetComponent<Image>();

            imageFileText = CreateText(
                "TXT_ImageFile",
                card,
                "Ningún archivo seleccionado",
                18f,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            imageFileText.color = new Color(1f, 1f, 1f, 0.55f);
            SetAnchoredRect(
                imageFileText.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(330f, y),
                new Vector2(390f, 44f));
        }

        private void ConfigureDateInput(TMP_InputField input, bool isStartDate)
        {
            input.readOnly = true;
            input.shouldHideMobileInput = true;
            input.onSelect.AddListener(_ => OpenCalendar(input, isStartDate));
        }

        private void OpenCalendar(TMP_InputField targetInput, bool isStartDate)
        {
            if (calendarPopup != null)
            {
                return;
            }

            DateTime initialDate = TryParseDate(targetInput.text, out DateTime parsed)
                ? parsed
                : DateTime.Today;
            DateTime? minimumDate = null;
            if (!isStartDate && TryParseDate(startDateInput.text, out DateTime start))
            {
                minimumDate = start;
                if (initialDate < start)
                {
                    initialDate = start;
                }
            }

            GameObject popupObject = new GameObject(
                isStartDate ? "StartDateCalendar" : "EndDateCalendar",
                typeof(RectTransform),
                typeof(CalendarPopupView));
            RectTransform popupRect = popupObject.GetComponent<RectTransform>();
            popupRect.SetParent(transform, false);
            Stretch(popupRect);
            popupRect.SetAsLastSibling();

            calendarPopup = popupObject.GetComponent<CalendarPopupView>();
            calendarPopup.Initialize(
                isStartDate
                    ? "Seleccionar fecha de inicio"
                    : "Seleccionar fecha de finalización",
                initialDate,
                minimumDate,
                selectedDate => ApplySelectedDate(
                    targetInput,
                    isStartDate,
                    selectedDate),
                () => calendarPopup = null);
        }

        private void ApplySelectedDate(
            TMP_InputField targetInput,
            bool isStartDate,
            DateTime selectedDate)
        {
            targetInput.text = selectedDate.ToString(
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture);

            if (isStartDate &&
                TryParseDate(endDateInput.text, out DateTime currentEnd) &&
                currentEnd < selectedDate)
            {
                endDateInput.text = string.Empty;
            }

            SetStatus(string.Empty, true);
        }

        private static bool TryParseDate(string value, out DateTime date)
        {
            return DateTime.TryParseExact(
                value,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }

        private GameObject CreateSmallButton(
            RectTransform parent,
            string objectName,
            string labelValue,
            Vector2 position,
            Vector2 size,
            UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = CreateRoundedButton(
                objectName,
                parent,
                labelValue,
                new Color(0.20f, 0.35f, 0.55f, 1f),
                position,
                size,
                action);

            GameObject previewObject = new GameObject(
                "Preview",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform previewRect = previewObject.GetComponent<RectTransform>();
            previewRect.SetParent(buttonObject.transform, false);
            previewRect.anchorMin = new Vector2(0f, 0.5f);
            previewRect.anchorMax = new Vector2(0f, 0.5f);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.anchoredPosition = new Vector2(28f, 0f);
            previewRect.sizeDelta = new Vector2(34f, 34f);
            Image image = previewObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return buttonObject;
        }

        private void CreateActionButton(
            RectTransform parent,
            string objectName,
            string label,
            Color color,
            Vector2 position,
            UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = CreateRoundedButton(
                objectName,
                parent,
                label,
                color,
                position,
                new Vector2(325f, 80f),
                action);
            AddBorder(
                buttonObject.GetComponent<RectTransform>(),
                new Color(1f, 1f, 1f, 0.6f),
                4f);
        }

        private GameObject CreateRoundedButton(
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
            button.onClick.AddListener(action);

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f);
            colors.pressedColor = new Color(0.76f, 0.76f, 0.76f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            TMP_Text text = CreateText(
                "TXT_Label",
                rect,
                labelValue,
                size: 22f,
                style: FontStyles.Bold,
                alignment: TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.raycastTarget = false;
            return buttonObject;
        }

        private static TMP_InputField CreateInputField(
            string objectName,
            RectTransform parent,
            string placeholderValue,
            bool multiline,
            TMP_Text label)
        {
            GameObject inputObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIRoundedRectangle),
                typeof(TMP_InputField));
            RectTransform inputRect = inputObject.GetComponent<RectTransform>();
            inputRect.SetParent(parent, false);

            UIRoundedRectangle background =
                inputObject.GetComponent<UIRoundedRectangle>();
            background.color = InputColor;

            RectTransform viewport = CreateRect("Text Area", inputRect);
            Stretch(viewport);
            viewport.offsetMin = new Vector2(14f, 7f);
            viewport.offsetMax = new Vector2(-14f, -7f);
            viewport.gameObject.AddComponent<RectMask2D>();

            TMP_Text placeholder = CreateText(
                "Placeholder",
                viewport,
                placeholderValue,
                21f,
                FontStyles.Italic,
                TextAlignmentOptions.MidlineLeft);
            Stretch(placeholder.rectTransform);
            placeholder.color = new Color(1f, 1f, 1f, 0.32f);

            TMP_Text valueText = CreateText(
                "Text",
                viewport,
                string.Empty,
                21f,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            Stretch(valueText.rectTransform);

            TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.transition = Selectable.Transition.None;
            input.textViewport = viewport;
            input.textComponent = valueText;
            input.placeholder = placeholder;
            input.lineType = multiline
                ? TMP_InputField.LineType.MultiLineNewline
                : TMP_InputField.LineType.SingleLine;
            input.pointSize = 21f;
            input.customCaretColor = true;
            input.caretColor = Color.white;
            input.caretWidth = 3;
            input.caretBlinkRate = 0.65f;
            input.selectionColor = new Color(0.35f, 0.55f, 0.85f, 0.65f);

            UIRoundedInnerBorder focusBorder = AddBorder(
                inputRect,
                Color.clear,
                3f);
            InputFieldFocusVisual focusVisual =
                inputObject.AddComponent<InputFieldFocusVisual>();
            focusVisual.Initialize(background, focusBorder, label);
            return input;
        }

        private void HandleSelectImage()
        {
#if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel(
                "Seleccionar imagen del evento",
                string.Empty,
                "png,jpg,jpeg");
            if (!string.IsNullOrWhiteSpace(path))
            {
                SetSelectedImage(path);
            }
#else
            SetStatus(
                "El selector de archivos para la compilación se conectará mediante un adaptador de plataforma.",
                false);
#endif
        }

        private void SetSelectedImage(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                DestroyPreviewResources();

                previewTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!previewTexture.LoadImage(bytes))
                {
                    throw new InvalidOperationException("El archivo no contiene una imagen válida.");
                }

                previewSprite = Sprite.Create(
                    previewTexture,
                    new Rect(0f, 0f, previewTexture.width, previewTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                previewImage.sprite = previewSprite;
                previewImage.color = Color.white;
                selectedImagePath = path;
                imageFileText.text = Path.GetFileName(path);
                SetStatus(string.Empty, true);
            }
            catch (Exception exception)
            {
                SetStatus($"No fue posible cargar la imagen: {exception.Message}", false);
            }
        }

        private void HandleSave()
        {
            if (!canCreatePublications)
            {
                SetStatus(
                    "Tu rol no tiene permiso para crear publicaciones.",
                    false);
                return;
            }

            string roleId = GameSessionStore.Instance.State.SelectedRoleId;
            if (!EventPublicationValidator.TryCreate(
                    titleInput.text,
                    descriptionInput.text,
                    linkInput.text,
                    startDateInput.text,
                    endDateInput.text,
                    selectedImagePath,
                    roleId,
                    out EventPublication publication,
                    out string error))
            {
                SetStatus(error, false);
                return;
            }

            VolatileEventPublicationRepository repository =
                VolatileEventPublicationRepository.Instance;
            if (editingPublication == null)
            {
                repository.Add(publication);
                SetStatus("Evento creado correctamente.", true);
            }
            else
            {
                EventPublication updated = editingPublication.WithUpdatedContent(
                    publication.Title,
                    publication.Description,
                    publication.Link,
                    publication.PublicationStartsAt,
                    publication.PublicationEndsAt,
                    publication.LocalImagePath);
                if (!repository.UpdatePublication(updated))
                {
                    SetStatus(
                        "La publicación ya no existe y no pudo actualizarse.",
                        false);
                    return;
                }

                SetStatus("Evento actualizado correctamente.", true);
            }

            if (saveConfirmed != null)
            {
                saveConfirmed.Invoke();
            }
            else
            {
                ClearForm();
            }
        }

        private void PopulateFormForEditing()
        {
            if (editingPublication == null)
            {
                return;
            }

            titleInput.text = editingPublication.Title;
            descriptionInput.text = editingPublication.Description;
            linkInput.text = editingPublication.Link;
            startDateInput.text = editingPublication.PublicationStartsAt.ToString(
                EventPublicationValidator.DateFormat,
                CultureInfo.InvariantCulture);
            endDateInput.text = editingPublication.PublicationEndsAt.ToString(
                EventPublicationValidator.DateFormat,
                CultureInfo.InvariantCulture);

            selectedImagePath = editingPublication.LocalImagePath;
            if (string.IsNullOrWhiteSpace(selectedImagePath))
            {
                return;
            }

            imageFileText.text = Path.GetFileName(selectedImagePath);
            if (File.Exists(selectedImagePath))
            {
                SetSelectedImage(selectedImagePath);
            }
        }

        private void ShowCancelConfirmation()
        {
            if (confirmationModal != null)
            {
                return;
            }

            confirmationModal = new GameObject(
                "CancelConfirmationModal",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform overlay = confirmationModal.GetComponent<RectTransform>();
            overlay.SetParent(transform, false);
            Stretch(overlay);
            Image dim = confirmationModal.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            RectTransform dialog = CreateRoundedPanel(
                "Dialog",
                overlay,
                new Color(0.11f, 0.16f, 0.25f, 1f),
                new Vector2(650f, 285f),
                Vector2.zero);
            AddBorder(dialog, new Color(1f, 1f, 1f, 0.35f), 3f);

            TMP_Text title = CreateText(
                "TXT_ConfirmTitle",
                dialog,
                "¿Cancelar la publicación?",
                31f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetAnchoredRect(
                title.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 75f),
                new Vector2(570f, 55f));

            TMP_Text message = CreateText(
                "TXT_ConfirmMessage",
                dialog,
                "Los datos ingresados se perderán.",
                21f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            message.color = new Color(1f, 1f, 1f, 0.72f);
            SetAnchoredRect(
                message.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 22f),
                new Vector2(570f, 42f));

            CreateRoundedButton(
                "BTN_KeepEditing",
                dialog,
                "Continuar editando",
                new Color(0.28f, 0.43f, 0.65f, 1f),
                new Vector2(-145f, -72f),
                new Vector2(260f, 62f),
                CloseConfirmation);
            CreateRoundedButton(
                "BTN_Discard",
                dialog,
                "Sí, descartar",
                DangerColor,
                new Vector2(145f, -72f),
                new Vector2(260f, 62f),
                ConfirmCancellation);

        }

        private void CloseConfirmation()
        {
            if (confirmationModal != null)
            {
                Destroy(confirmationModal);
                confirmationModal = null;
            }
        }

        private void ConfirmCancellation()
        {
            CloseConfirmation();
            cancelConfirmed?.Invoke();
        }

        private void ClearForm()
        {
            titleInput.text = string.Empty;
            descriptionInput.text = string.Empty;
            linkInput.text = string.Empty;
            startDateInput.text = string.Empty;
            endDateInput.text = string.Empty;
            selectedImagePath = string.Empty;
            imageFileText.text = "Ningún archivo seleccionado";
            DestroyPreviewResources();
            if (previewImage != null)
            {
                previewImage.sprite = null;
                previewImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        private void SetStatus(string message, bool success)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message;
            statusText.color = success
                ? new Color(0.48f, 0.90f, 0.62f, 1f)
                : new Color(1f, 0.58f, 0.58f, 1f);
        }

        private void DestroyPreviewResources()
        {
            if (previewSprite != null)
            {
                Destroy(previewSprite);
                previewSprite = null;
            }

            if (previewTexture != null)
            {
                Destroy(previewTexture);
                previewTexture = null;
            }
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

        private static UIRoundedInnerBorder AddBorder(
            RectTransform target,
            Color color,
            float thickness)
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
            return border;
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
            string labelValue,
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
            text.text = labelValue;
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
