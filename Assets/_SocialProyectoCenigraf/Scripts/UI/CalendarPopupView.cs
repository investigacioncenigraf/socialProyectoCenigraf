using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    public sealed class CalendarPopupView : MonoBehaviour
    {
        private static readonly CultureInfo SpanishCulture =
            CultureInfo.GetCultureInfo("es-CO");

        private readonly List<Button> dayButtons = new List<Button>(42);
        private readonly List<TMP_Text> dayLabels = new List<TMP_Text>(42);

        private DateTime visibleMonth;
        private DateTime? minimumDate;
        private Action<DateTime> dateSelected;
        private Action closed;
        private TMP_Text monthLabel;

        public void Initialize(
            string title,
            DateTime initialDate,
            DateTime? minimum,
            Action<DateTime> onDateSelected,
            Action onClosed)
        {
            visibleMonth = new DateTime(initialDate.Year, initialDate.Month, 1);
            minimumDate = minimum?.Date;
            dateSelected = onDateSelected;
            closed = onClosed;
            BuildView(title);
            RefreshDays();
        }

        public void Dismiss()
        {
            Close();
        }

        private void BuildView(string titleValue)
        {
            RectTransform overlay = (RectTransform)transform;
            Stretch(overlay);

            Image dim = gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            RectTransform dialog = CreateRoundedPanel(
                "CalendarDialog",
                overlay,
                new Color(0.09f, 0.15f, 0.24f, 1f),
                new Vector2(620f, 610f),
                Vector2.zero);
            AddBorder(dialog, new Color(0.35f, 0.68f, 0.98f, 0.95f), 3f);

            TMP_Text title = CreateText(
                "TXT_CalendarTitle",
                dialog,
                titleValue,
                27f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetAnchoredRect(
                title.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 255f),
                new Vector2(520f, 45f));

            CreateButton(
                "BTN_PreviousMonth",
                dialog,
                "<",
                new Vector2(-235f, 195f),
                new Vector2(58f, 48f),
                () => ChangeMonth(-1));
            CreateButton(
                "BTN_NextMonth",
                dialog,
                ">",
                new Vector2(235f, 195f),
                new Vector2(58f, 48f),
                () => ChangeMonth(1));

            monthLabel = CreateText(
                "TXT_VisibleMonth",
                dialog,
                string.Empty,
                25f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetAnchoredRect(
                monthLabel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 195f),
                new Vector2(380f, 48f));

            string[] weekdayNames = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
            for (int column = 0; column < weekdayNames.Length; column++)
            {
                TMP_Text weekday = CreateText(
                    $"TXT_Weekday_{column}",
                    dialog,
                    weekdayNames[column],
                    18f,
                    FontStyles.Bold,
                    TextAlignmentOptions.Center);
                weekday.color = new Color(0.58f, 0.78f, 1f, 0.9f);
                SetAnchoredRect(
                    weekday.rectTransform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-225f + column * 75f, 140f),
                    new Vector2(66f, 34f));
            }

            for (int index = 0; index < 42; index++)
            {
                int row = index / 7;
                int column = index % 7;
                Button dayButton = CreateButton(
                    $"BTN_Day_{index}",
                    dialog,
                    string.Empty,
                    new Vector2(-225f + column * 75f, 91f - row * 61f),
                    new Vector2(62f, 48f),
                    null);
                dayButtons.Add(dayButton);
                dayLabels.Add(dayButton.GetComponentInChildren<TMP_Text>());
            }

            Button cancel = CreateButton(
                "BTN_CloseCalendar",
                dialog,
                "Cancelar",
                new Vector2(0f, -260f),
                new Vector2(220f, 54f),
                Close);
            cancel.GetComponent<UIRoundedRectangle>().color =
                new Color(0.40f, 0.43f, 0.50f, 1f);
        }

        private void RefreshDays()
        {
            monthLabel.text = SpanishCulture.TextInfo.ToTitleCase(
                visibleMonth.ToString("MMMM yyyy", SpanishCulture));

            int mondayBasedOffset = ((int)visibleMonth.DayOfWeek + 6) % 7;
            int daysInMonth = DateTime.DaysInMonth(
                visibleMonth.Year,
                visibleMonth.Month);

            for (int index = 0; index < dayButtons.Count; index++)
            {
                Button button = dayButtons[index];
                TMP_Text label = dayLabels[index];
                button.onClick.RemoveAllListeners();

                int dayNumber = index - mondayBasedOffset + 1;
                bool belongsToMonth = dayNumber >= 1 && dayNumber <= daysInMonth;
                button.gameObject.SetActive(belongsToMonth);
                if (!belongsToMonth)
                {
                    continue;
                }

                DateTime date = new DateTime(
                    visibleMonth.Year,
                    visibleMonth.Month,
                    dayNumber);
                bool available = !minimumDate.HasValue || date >= minimumDate.Value;
                bool today = date == DateTime.Today;

                label.text = dayNumber.ToString(CultureInfo.InvariantCulture);
                button.interactable = available;
                UIRoundedRectangle background =
                    button.GetComponent<UIRoundedRectangle>();
                background.color = today
                    ? new Color(0.23f, 0.53f, 0.82f, 1f)
                    : new Color(0.13f, 0.22f, 0.34f, 1f);

                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.62f, 0.82f, 1f, 1f);
                colors.pressedColor = new Color(0.42f, 0.68f, 0.95f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.35f, 0.37f, 0.42f, 0.45f);
                button.colors = colors;

                DateTime selectedDate = date;
                button.onClick.AddListener(() => SelectDate(selectedDate));
            }
        }

        private void ChangeMonth(int offset)
        {
            visibleMonth = visibleMonth.AddMonths(offset);
            RefreshDays();
        }

        private void SelectDate(DateTime date)
        {
            dateSelected?.Invoke(date.Date);
            Close();
        }

        private void Close()
        {
            closed?.Invoke();
            Destroy(gameObject);
        }

        private static Button CreateButton(
            string objectName,
            RectTransform parent,
            string textValue,
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
            background.color = new Color(0.16f, 0.29f, 0.44f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.68f, 0.84f, 1f, 1f);
            colors.pressedColor = new Color(0.45f, 0.67f, 0.90f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TMP_Text label = CreateText(
                "TXT_Label",
                rect,
                textValue,
                20f,
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
