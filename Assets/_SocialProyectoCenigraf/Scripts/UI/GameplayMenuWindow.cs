using System;
using System.Collections;
using System.Collections.Generic;
using SocialProyectoCenigraf.Audio;
using SocialProyectoCenigraf.Controls;
using SocialProyectoCenigraf.Dialogue;
using SocialProyectoCenigraf.Rewards;
using SocialProyectoCenigraf.World.NPC;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    /// <summary>
    /// Floating menu that drops from the HUD menu button. Opens with ESC or with that
    /// button, closes with ESC, with the button again or by clicking outside. Carries an
    /// audio page with the player volume for music and effects. It builds itself at
    /// runtime and persists between scenes, so no scene needs to carry it, and it stays
    /// out of the login screen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayMenuWindow : MonoBehaviour
    {
        private const float FadeSeconds = 0.15f;
        private const float PanelWidth = 420f;
        private const float Padding = 18f;
        private const float TitleHeight = 30f;
        private const float Gap = 12f;
        private const float ButtonHeight = 64f;
        private const float RowHeight = 54f;
        private const float RowGap = 10f;
        private const float LabelHeight = 24f;
        private const float DropdownHeight = 48f;
        private const float OptionHeight = 44f;
        private const float TightGap = 4f;
        private const float MessageHeight = 60f;

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color PanelColor = new Color32(22, 34, 48, 240);
        private static readonly Color ButtonColor = new Color32(30, 45, 61, 255);
        private static readonly Color FieldColor = new Color32(12, 23, 35, 255);
        private static readonly Color BorderColor = new Color32(207, 225, 237, 88);
        private static readonly Color AccentColor = new Color32(65, 210, 225, 255);
        private static readonly Color TextColor = new Color32(244, 249, 255, 255);
        private static readonly Color MutedTextColor = new Color32(180, 197, 211, 255);
        private static readonly Color DangerColor = new Color32(166, 52, 52, 255);

        private enum Page
        {
            Main,
            Audio,
            Interface,
            Logout
        }

        private static GameplayMenuWindow instance;

        private CanvasGroup group;
        private RectTransform panel;
        private RectTransform mainPage;
        private RectTransform audioPage;
        private RectTransform interfacePage;
        private RectTransform logoutPage;
        private RectTransform dropdownList;
        private TMP_Text dropdownValue;
        private TMP_Text dropdownCaret;
        private readonly List<KeyValuePair<TextAnimationStyle, TMP_Text>> dropdownOptions =
            new List<KeyValuePair<TextAnimationStyle, TMP_Text>>();
        private TMP_Text titleText;
        private Slider musicSlider;
        private Slider effectsSlider;
        private TMP_Text musicValue;
        private TMP_Text effectsValue;
        private Button hookedMenuButton;
        private GameObject ownedEventSystem;
        private GameObject previousSelection;
        private Coroutine fadeRoutine;
        private float mainHeight;
        private float audioHeight;
        private float interfaceHeight;
        private float interfaceExpandedHeight;
        private float logoutHeight;
        private Page currentPage;
        private bool dropdownExpanded;
        private bool isOpen;
        private int openedFrame = -1;

        public static bool IsOpen => instance != null && instance.isOpen;
        public static int LastClosedFrame { get; private set; } = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            LastClosedFrame = -1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart() => GetInstance();

        public static void Toggle()
        {
            GameplayMenuWindow menu = GetInstance();
            if (menu.isOpen)
            {
                menu.Close();
                return;
            }

            menu.Open();
        }

        private static GameplayMenuWindow GetInstance()
        {
            if (instance == null)
            {
                GameObject host = new GameObject(nameof(GameplayMenuWindow));
                instance = host.AddComponent<GameplayMenuWindow>();
            }

            return instance;
        }

        // Every other window owns ESC while it is up, and the frame it closes on belongs
        // to it as well: the same press must not open this menu behind it.
        private static bool AnotherWindowOwnsInput()
        {
            int frame = Time.frameCount;
            return GameplayInputLock.IsLocked ||
                DialoguePresenter.IsOpen || DialoguePresenter.LastClosedFrame == frame ||
                DialogoOfficial.IsOpen || DialogoOfficial.LastClosedFrame == frame ||
                RewardWindow.IsOpen || RewardWindow.LastClosedFrame == frame ||
                NPCFullscreenOverlay.IsOpen ||
                NPCFullscreenOverlay.LastClosedFrame == frame;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            AudioVolumeSettings.Changed += RefreshVolumeLabels;
            TextAnimationSettings.Changed += RefreshDropdown;
            HookMenuButton();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            AudioVolumeSettings.Changed -= RefreshVolumeLabels;
            TextAnimationSettings.Changed -= RefreshDropdown;
            GameplayInputLock.Release(this);
            UnhookMenuButton();

            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (isOpen)
            {
                // The press that opened it must not close it on the same frame.
                if (Time.frameCount == openedFrame)
                {
                    return;
                }

                // ESC steps back one level at a time before leaving the menu.
                if (dropdownExpanded)
                {
                    SetDropdownExpanded(false);
                    return;
                }

                if (currentPage != Page.Main)
                {
                    ShowPage(Page.Main);
                    return;
                }

                Close();
                return;
            }

            if (LoginController.ExistsInScene() || AnotherWindowOwnsInput())
            {
                return;
            }

            Open();
        }

        public void Open()
        {
            if (isOpen || LoginController.ExistsInScene() || AnotherWindowOwnsInput())
            {
                return;
            }

            isOpen = true;
            openedFrame = Time.frameCount;
            GameplayInputLock.Acquire(this);
            group.blocksRaycasts = true;
            group.interactable = true;
            SetMenuButtonVisible(false);
            ShowPage(Page.Main);

            if (EventSystem.current == null)
            {
                ownedEventSystem = new GameObject(
                    "MenuEventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
            }

            previousSelection = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            RetroFeedbackAudio.PlayInteraction();
            StartFade(1f);
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            LastClosedFrame = Time.frameCount;
            GameplayInputLock.Release(this);
            group.blocksRaycasts = false;
            group.interactable = false;
            SetMenuButtonVisible(true);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(previousSelection);
            }

            previousSelection = null;

            if (ownedEventSystem != null)
            {
                Destroy(ownedEventSystem);
                ownedEventSystem = null;
            }

            StartFade(0f);
        }

        // TODO: cerrar la sesión y volver a la pantalla de login.
        private void HandleCloseSession()
        {
        }

        private void ShowPage(Page page)
        {
            bool leavingAudio = currentPage == Page.Audio && page != Page.Audio;
            bool leavingInterface = currentPage == Page.Interface && page != Page.Interface;

            currentPage = page;
            SetDropdownExpanded(false);

            mainPage.gameObject.SetActive(page == Page.Main);
            audioPage.gameObject.SetActive(page == Page.Audio);
            interfacePage.gameObject.SetActive(page == Page.Interface);
            logoutPage.gameObject.SetActive(page == Page.Logout);

            float height;
            switch (page)
            {
                case Page.Audio:
                    titleText.text = "AUDIO";
                    height = audioHeight;
                    SyncSlidersFromSettings();
                    break;
                case Page.Interface:
                    titleText.text = "INTERFAZ";
                    height = interfaceHeight;
                    RefreshDropdown();
                    break;
                case Page.Logout:
                    titleText.text = "CERRAR SESIÓN";
                    height = logoutHeight;
                    break;
                default:
                    titleText.text = "MENU";
                    height = mainHeight;
                    break;
            }

            panel.sizeDelta = new Vector2(PanelWidth, height);

            // Leaving a page is the quiet moment to write its setting to disk.
            if (leavingAudio)
            {
                AudioVolumeSettings.Save();
            }

            if (leavingInterface)
            {
                TextAnimationSettings.Save();
            }
        }

        private void SetDropdownExpanded(bool expanded)
        {
            dropdownExpanded = expanded && currentPage == Page.Interface;

            if (currentPage == Page.Interface && panel != null)
            {
                panel.sizeDelta = new Vector2(
                    PanelWidth,
                    dropdownExpanded ? interfaceExpandedHeight : interfaceHeight);
            }

            if (dropdownList != null)
            {
                dropdownList.gameObject.SetActive(dropdownExpanded);
            }

            if (dropdownCaret != null)
            {
                dropdownCaret.text = dropdownExpanded ? "^" : "v";
            }
        }

        private void RefreshDropdown()
        {
            if (dropdownValue == null)
            {
                return;
            }

            TextAnimationStyle current = TextAnimationSettings.Style;
            dropdownValue.text = TextAnimationSettings.LabelOf(current);

            // The chosen option stays marked with the accent while the list is open.
            for (int i = 0; i < dropdownOptions.Count; i++)
            {
                dropdownOptions[i].Value.color =
                    dropdownOptions[i].Key == current ? AccentColor : TextColor;
            }
        }

        private void SelectTextAnimation(TextAnimationStyle style)
        {
            TextAnimationSettings.Style = style;
            SetDropdownExpanded(false);
            RefreshDropdown();
        }

        private void SyncSlidersFromSettings()
        {
            // Without notify: writing the stored value back must not look like a change.
            musicSlider.SetValueWithoutNotify(AudioVolumeSettings.Music);
            effectsSlider.SetValueWithoutNotify(AudioVolumeSettings.Effects);
            RefreshVolumeLabels();
        }

        private void RefreshVolumeLabels()
        {
            musicValue.text = Percentage(AudioVolumeSettings.Music);
            effectsValue.text = Percentage(AudioVolumeSettings.Effects);
        }

        private static string Percentage(float value) =>
            Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";

        private void HandleMusicChanged(float value)
        {
            AudioVolumeSettings.Music = value;
        }

        private void HandleEffectsChanged(float value)
        {
            AudioVolumeSettings.Effects = value;
            // A tone at the new level, so the slider can be heard while it moves.
            RetroFeedbackAudio.PlayButtonHover();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive)
            {
                return;
            }

            if (isOpen)
            {
                Close();
            }

            HookMenuButton();
            // The HUD may be assembled by its own Start, one frame after the scene loads.
            StartCoroutine(HookNextFrame());
        }

        private IEnumerator HookNextFrame()
        {
            yield return null;
            HookMenuButton();
        }

        private void HookMenuButton()
        {
            GameplayHUDView hud = FindFirstObjectByType<GameplayHUDView>();
            Button menuButton = hud != null ? hud.MenuButton : null;
            if (menuButton == hookedMenuButton)
            {
                return;
            }

            UnhookMenuButton();

            if (menuButton == null)
            {
                return;
            }

            hookedMenuButton = menuButton;
            hookedMenuButton.onClick.AddListener(Toggle);
        }

        private void UnhookMenuButton()
        {
            if (hookedMenuButton != null)
            {
                hookedMenuButton.onClick.RemoveListener(Toggle);
                // Never leave a HUD with its button hidden behind a closed menu.
                hookedMenuButton.gameObject.SetActive(true);
            }

            hookedMenuButton = null;
        }

        // The button is what opened the menu, so it has nothing left to offer while the
        // menu is up, and the panel reads cleaner without it behind the veil.
        private void SetMenuButtonVisible(bool visible)
        {
            if (hookedMenuButton != null)
            {
                hookedMenuButton.gameObject.SetActive(visible);
            }
        }

        private void StartFade(float target)
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(Fade(target));
        }

        private IEnumerator Fade(float target)
        {
            float from = group.alpha;
            float elapsed = 0f;

            while (elapsed < FadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(elapsed / FadeSeconds));
                yield return null;
            }

            group.alpha = target;
            fadeRoutine = null;
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Over the HUD at 100, under the stage banner and the loading screen.
            canvas.sortingOrder = 400;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            RectTransform safeArea = CreateRect("SafeArea", transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<UISafeAreaFitter>();

            // Clicking anywhere outside the panel closes the menu. A Button here would
            // join the automatic hover and click tones of the HUD, so it stays an Image.
            RectTransform backdropRect = CreateRect("Backdrop", safeArea);
            Stretch(backdropRect);
            Image backdrop = backdropRect.gameObject.AddComponent<Image>();
            backdrop.color = BackdropColor;
            EventTrigger backdropTrigger = backdropRect.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry dismiss = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };
            dismiss.callback.AddListener(_ => Close());
            backdropTrigger.triggers.Add(dismiss);

            // mainHeight is settled by BuildMainPage, from how many entries it lays out.
            audioHeight = Padding + TitleHeight + Gap +
                RowHeight + RowGap + RowHeight + Gap + ButtonHeight + Padding;

            // Centred on both axes: the panel grows from its middle, so changing page
            // keeps it in place instead of pushing it down.
            panel = CreateRounded("Panel", safeArea, PanelColor, 18f);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(PanelWidth, mainHeight);
            panel.anchoredPosition = Vector2.zero;
            CreateBorder(panel, 1.5f, 18f);

            titleText = CreateLabel(panel, "TXT_Title", "MENU", 14f, MutedTextColor);
            titleText.alignment = TextAlignmentOptions.Left;
            PlaceRow(titleText.rectTransform, TitleHeight, -Padding);

            mainPage = CreateRect("MainPage", panel);
            Stretch(mainPage);
            audioPage = CreateRect("AudioPage", panel);
            Stretch(audioPage);
            interfacePage = CreateRect("InterfacePage", panel);
            Stretch(interfacePage);
            logoutPage = CreateRect("LogoutPage", panel);
            Stretch(logoutPage);

            BuildMainPage();
            BuildAudioPage();
            BuildInterfacePage();
            BuildLogoutPage();
            ShowPage(Page.Main);
        }

        private void BuildMainPage()
        {
            // "Cerrar sesión" is the last entry by convention: a new option goes above
            // it, never below. The panel height follows the count on its own.
            (string Name, string Label, Action Action)[] entries =
            {
                ("BTN_Continuar", "Continuar", Close),
                ("BTN_Audio", "Audio", () => ShowPage(Page.Audio)),
                ("BTN_Interfaz", "Interfaz", () => ShowPage(Page.Interface)),
                ("BTN_CerrarSesion", "Cerrar sesión", () => ShowPage(Page.Logout))
            };

            float top = Padding + TitleHeight + Gap;
            for (int i = 0; i < entries.Length; i++)
            {
                CreateMenuButton(
                    mainPage,
                    entries[i].Name,
                    entries[i].Label,
                    -(top + i * (ButtonHeight + Gap)),
                    entries[i].Action);
            }

            mainHeight = top + entries.Length * ButtonHeight +
                (entries.Length - 1) * Gap + Padding;
        }

        private void BuildAudioPage()
        {
            float top = Padding + TitleHeight + Gap;

            musicSlider = CreateVolumeRow(
                "ROW_Musica",
                "MÚSICA",
                -top,
                HandleMusicChanged,
                out musicValue);
            effectsSlider = CreateVolumeRow(
                "ROW_Efectos",
                "EFECTOS",
                -(top + RowHeight + RowGap),
                HandleEffectsChanged,
                out effectsValue);

            CreateMenuButton(
                audioPage,
                "BTN_Volver",
                "Volver",
                -(top + RowHeight + RowGap + RowHeight + Gap),
                () => ShowPage(Page.Main));
        }

        private void BuildInterfacePage()
        {
            float top = Padding + TitleHeight + Gap;

            TMP_Text caption = CreateLabel(
                interfacePage,
                "TXT_AnimacionTexto",
                "ANIMACIÓN DE TEXTO",
                12f,
                MutedTextColor);
            caption.alignment = TextAlignmentOptions.Left;
            PlaceRow(caption.rectTransform, LabelHeight, -top);

            float fieldTop = top + LabelHeight + TightGap;
            RectTransform field = CreateRounded(
                "BTN_AnimacionTexto",
                interfacePage,
                FieldColor,
                10f);
            PlaceRow(field, DropdownHeight, -fieldTop);
            CreateBorder(field, 1.5f, 10f);

            UIRoundedRectangle fieldGraphic = field.GetComponent<UIRoundedRectangle>();
            fieldGraphic.raycastTarget = true;
            Button fieldButton = field.gameObject.AddComponent<Button>();
            fieldButton.targetGraphic = fieldGraphic;
            fieldButton.onClick.AddListener(() => SetDropdownExpanded(!dropdownExpanded));

            dropdownValue = CreateLabel(field, "TXT_Value", "Default", 16f, TextColor);
            dropdownValue.alignment = TextAlignmentOptions.Left;
            Stretch(dropdownValue.rectTransform, 14f, 40f, 6f, 6f);

            // Press Start 2P has no chevron glyph, so the caret is ASCII.
            dropdownCaret = CreateLabel(field, "TXT_Caret", "v", 14f, MutedTextColor);
            dropdownCaret.rectTransform.anchorMin = new Vector2(1f, 0f);
            dropdownCaret.rectTransform.anchorMax = new Vector2(1f, 1f);
            dropdownCaret.rectTransform.pivot = new Vector2(1f, 0.5f);
            dropdownCaret.rectTransform.sizeDelta = new Vector2(34f, 0f);
            dropdownCaret.rectTransform.anchoredPosition = Vector2.zero;

            float afterField = fieldTop + DropdownHeight;
            CreateMenuButton(
                interfacePage,
                "BTN_Volver",
                "Volver",
                -(afterField + Gap),
                () => ShowPage(Page.Main));

            interfaceHeight = afterField + Gap + ButtonHeight + Padding;

            BuildDropdownList(afterField + TightGap);
        }

        // The open list floats over the content below it, like any dropdown, so the page
        // keeps its height and the button under it does not move.
        private void BuildDropdownList(float offsetY)
        {
            TextAnimationStyle[] styles =
            {
                TextAnimationStyle.Default,
                TextAnimationStyle.Retro
            };

            float height = TightGap + styles.Length * (OptionHeight + TightGap);
            dropdownList = CreateRounded("DropdownList", interfacePage, ButtonColor, 10f);
            PlaceRow(dropdownList, height, -offsetY);
            CreateBorder(dropdownList, 1.5f, 10f);
            dropdownList.GetComponent<UIRoundedRectangle>().raycastTarget = true;
            interfaceExpandedHeight = offsetY + height + Padding;

            dropdownOptions.Clear();
            for (int i = 0; i < styles.Length; i++)
            {
                TextAnimationStyle style = styles[i];
                RectTransform option = CreateRect("OPT_" + style, dropdownList);
                option.anchorMin = new Vector2(0f, 1f);
                option.anchorMax = new Vector2(1f, 1f);
                option.pivot = new Vector2(0.5f, 1f);
                option.sizeDelta = new Vector2(-TightGap * 2f, OptionHeight);
                option.anchoredPosition =
                    new Vector2(0f, -(TightGap + i * (OptionHeight + TightGap)));

                Image hit = option.gameObject.AddComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0.1f);

                Button button = option.gameObject.AddComponent<Button>();
                button.targetGraphic = hit;
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 0f);
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
                button.colors = colors;
                button.onClick.AddListener(() => SelectTextAnimation(style));

                TMP_Text label = CreateLabel(
                    option,
                    "TXT_Label",
                    TextAnimationSettings.LabelOf(style),
                    16f,
                    TextColor);
                label.alignment = TextAlignmentOptions.Left;
                Stretch(label.rectTransform, 14f, 14f, 6f, 6f);
                dropdownOptions.Add(
                    new KeyValuePair<TextAnimationStyle, TMP_Text>(style, label));
            }

            dropdownList.gameObject.SetActive(false);
        }

        private void BuildLogoutPage()
        {
            float top = Padding + TitleHeight + Gap;

            TMP_Text message = CreateLabel(
                logoutPage,
                "TXT_Confirmar",
                "¿Seguro que quieres cerrar sesión?",
                14f,
                TextColor);
            message.textWrappingMode = TextWrappingModes.Normal;
            PlaceRow(message.rectTransform, MessageHeight, -top);

            // Side by side, the way a yes or no question reads.
            RectTransform row = CreateRect("ROW_Confirmar", logoutPage);
            PlaceRow(row, ButtonHeight, -(top + MessageHeight + Gap));

            RectTransform yes = CreateMenuButton(
                row, "BTN_Si", "Sí", 0f, HandleCloseSession, DangerColor);
            PlaceInRow(yes, 0f, 0.5f, 0f, Gap * 0.5f);

            RectTransform no = CreateMenuButton(row, "BTN_No", "No", 0f, Close);
            PlaceInRow(no, 0.5f, 1f, Gap * 0.5f, 0f);

            logoutHeight = top + MessageHeight + Gap + ButtonHeight + Padding;
        }

        private Slider CreateVolumeRow(
            string name,
            string label,
            float offsetY,
            UnityEngine.Events.UnityAction<float> onChanged,
            out TMP_Text valueText)
        {
            RectTransform row = CreateRect(name, audioPage);
            PlaceRow(row, RowHeight, offsetY);

            TMP_Text caption = CreateLabel(row, "TXT_Label", label, 12f, MutedTextColor);
            caption.alignment = TextAlignmentOptions.Left;
            caption.rectTransform.anchorMin = new Vector2(0f, 0f);
            caption.rectTransform.anchorMax = new Vector2(0f, 1f);
            caption.rectTransform.pivot = new Vector2(0f, 0.5f);
            caption.rectTransform.sizeDelta = new Vector2(110f, 0f);
            caption.rectTransform.anchoredPosition = Vector2.zero;

            valueText = CreateLabel(row, "TXT_Value", "100%", 12f, TextColor);
            valueText.alignment = TextAlignmentOptions.Right;
            valueText.rectTransform.anchorMin = new Vector2(1f, 0f);
            valueText.rectTransform.anchorMax = new Vector2(1f, 1f);
            valueText.rectTransform.pivot = new Vector2(1f, 0.5f);
            valueText.rectTransform.sizeDelta = new Vector2(56f, 0f);
            valueText.rectTransform.anchoredPosition = Vector2.zero;

            RectTransform sliderRect = CreateRect("Slider", row);
            sliderRect.anchorMin = new Vector2(0f, 0f);
            sliderRect.anchorMax = new Vector2(1f, 1f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.offsetMin = new Vector2(118f, 12f);
            sliderRect.offsetMax = new Vector2(-64f, -12f);

            Slider slider = sliderRect.gameObject.AddComponent<Slider>();

            RectTransform background = CreateRounded("Background", sliderRect, FieldColor, 5f);
            background.anchorMin = new Vector2(0f, 0.25f);
            background.anchorMax = new Vector2(1f, 0.75f);
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;
            // The slider needs something to be hit on to receive the drag.
            background.GetComponent<UIRoundedRectangle>().raycastTarget = true;

            RectTransform fillArea = CreateRect("FillArea", sliderRect);
            fillArea.anchorMin = new Vector2(0f, 0.25f);
            fillArea.anchorMax = new Vector2(1f, 0.75f);
            fillArea.pivot = new Vector2(0.5f, 0.5f);
            fillArea.sizeDelta = new Vector2(-20f, 0f);
            fillArea.anchoredPosition = new Vector2(-5f, 0f);

            RectTransform fill = CreateRounded("Fill", fillArea, AccentColor, 5f);
            fill.sizeDelta = new Vector2(10f, 0f);

            RectTransform handleArea = CreateRect("HandleSlideArea", sliderRect);
            Stretch(handleArea);
            handleArea.sizeDelta = new Vector2(-20f, 0f);

            RectTransform handle = CreateRounded("Handle", handleArea, TextColor, 9f);
            handle.sizeDelta = new Vector2(20f, 0f);
            UIRoundedRectangle handleGraphic = handle.GetComponent<UIRoundedRectangle>();
            handleGraphic.raycastTarget = true;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleGraphic;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        private RectTransform CreateMenuButton(
            RectTransform parent,
            string name,
            string label,
            float offsetY,
            Action action,
            Color? background = null)
        {
            RectTransform rect = CreateRounded(
                name,
                parent,
                background ?? ButtonColor,
                12f);
            PlaceRow(rect, ButtonHeight, offsetY);
            CreateBorder(rect, 1.5f, 12f);

            UIRoundedRectangle graphic = rect.GetComponent<UIRoundedRectangle>();
            graphic.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => action());

            TMP_Text text = CreateLabel(rect, "TXT_Label", label, 18f, TextColor);
            Stretch(text.rectTransform, 14f, 14f, 8f, 8f);
            return rect;
        }

        // A horizontal slice of a row, for buttons that sit next to each other.
        private static void PlaceInRow(
            RectTransform rect,
            float anchorMinX,
            float anchorMaxX,
            float leftInset,
            float rightInset)
        {
            rect.anchorMin = new Vector2(anchorMinX, 0f);
            rect.anchorMax = new Vector2(anchorMaxX, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(leftInset, 0f);
            rect.offsetMax = new Vector2(-rightInset, 0f);
        }

        // A full width row inside the panel, hanging from its top edge.
        private static void PlaceRow(RectTransform rect, float height, float offsetY)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-Padding * 2f, height);
            rect.anchoredPosition = new Vector2(0f, offsetY);
        }

        private static TMP_Text CreateLabel(
            RectTransform parent,
            string name,
            string content,
            float size,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = content;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 8f;
            text.fontSizeMax = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRounded(
            string name,
            Transform parent,
            Color color,
            float radius)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();
            UIRoundedRectangle graphic = rect.gameObject.AddComponent<UIRoundedRectangle>();
            graphic.color = color;
            graphic.raycastTarget = false;
            graphic.CornerRadius = radius;
            return rect;
        }

        private static void CreateBorder(RectTransform parent, float thickness, float radius)
        {
            RectTransform rect = CreateRect("Border", parent);
            Stretch(rect);
            rect.gameObject.AddComponent<CanvasRenderer>();
            UIRoundedInnerBorder border = rect.gameObject.AddComponent<UIRoundedInnerBorder>();
            border.color = BorderColor;
            border.Thickness = thickness;
            border.CornerRadius = radius;
            border.raycastTarget = false;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            RectTransform rect = host.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(
            RectTransform rect,
            float left = 0f,
            float right = 0f,
            float top = 0f,
            float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
