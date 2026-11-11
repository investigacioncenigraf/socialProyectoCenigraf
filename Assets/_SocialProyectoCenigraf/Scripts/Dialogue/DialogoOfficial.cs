using System;
using System.Collections.Generic;
using SocialProyectoCenigraf.Audio;
using SocialProyectoCenigraf.Controls;
using SocialProyectoCenigraf.Player.Movement;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.Dialogue
{
    /// <summary>
    /// Alternative popup presentation for the shared dialogue catalog.
    /// Kept independent from DialoguePresenter so the existing cinematic
    /// presentation remains available.
    /// </summary>
    public sealed class DialogoOfficial : MonoBehaviour
    {
        private static DialogoOfficial instance;
        private static Sprite roundedSprite;
        private static Texture2D roundedTexture;

        public readonly struct ChoiceContext
        {
            public readonly string dialogueId;
            public readonly string lineId;
            public readonly string optionKey;

            public ChoiceContext(string dialogueId, string lineId, string optionKey)
            {
                this.dialogueId = dialogueId;
                this.lineId = lineId;
                this.optionKey = optionKey;
            }
        }

        /// <summary>Permite que sistemas externos reaccionen a opciones por su key antes de navegar a la línea destino.</summary>
        public static event Action<ChoiceContext> ChoiceSelected;

        [Header("Appearance")]
        [SerializeField] private Sprite npcPortrait;
        [SerializeField] private Sprite playerPortrait;

        private DialogueSequence sequence;
        private DialogueCatalog catalog;
        private DialogueNode currentNode;
        private int lineIndex;
        private int inputFrame;
        private Canvas canvas;
        private RectTransform popup;
        private Image portrait;
        private Color currentSpeakerColor = Color.white;
        private TMP_Text speakerText;
        private TMP_Text bodyText;
        private Button continueButton;
        private RectTransform choicesContent;
        private ScrollRect choicesScroll;
        private CanvasGroup choicesCanvasGroup;
        private readonly List<Button> choiceButtons = new List<Button>();
        private readonly List<Image> choicePointers = new List<Image>();
        private int selectedChoiceIndex;
        private bool choicesUseVerticalLayout;
        private bool choicesReady;
        private float choicesRevealAlpha;
        private PlayerMovementController player;
        private bool restoreMovement;

        public static bool IsOpen => instance != null && instance.sequence != null;
        public static int LastClosedFrame { get; private set; } = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            LastClosedFrame = -1;
            ChoiceSelected = null;
        }

        public static bool TryOpen(
            DialogueCatalog catalog,
            string id,
            PlayerMovementController sourcePlayer,
            Sprite speakerPortrait = null,
            Color speakerColor = default,
            Sprite playerPortrait = null)
        {
            if (IsOpen || DialoguePresenter.IsOpen ||
                SocialProyectoCenigraf.Rewards.RewardWindow.IsOpen || sourcePlayer == null)
            {
                return false;
            }

            if (catalog == null || !catalog.TryGet(id, out DialogueSequence dialogue))
            {
                Debug.LogWarning($"Diálogo '{id}' vacío, inexistente o duplicado en el catálogo.");
                return false;
            }

            if (instance == null)
            {
                instance = new GameObject(nameof(DialogoOfficial)).AddComponent<DialogoOfficial>();
            }

            instance.npcPortrait = speakerPortrait;
            instance.playerPortrait = playerPortrait;
            instance.currentSpeakerColor = speakerColor == default ? Color.white : speakerColor;
            instance.Open(catalog, dialogue, sourcePlayer);
            return true;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            SceneManager.activeSceneChanged += OnSceneChanged;
            BuildInterface();
        }

        private void OnSceneChanged(Scene previous, Scene next) => Close();

        private void Open(DialogueCatalog sourceCatalog, DialogueSequence dialogue, PlayerMovementController sourcePlayer)
        {
            catalog = sourceCatalog;
            sequence = dialogue;
            lineIndex = 0;
            currentNode = null;
            if (sequence.UsesNodes && !catalog.ResolveStartNode(sequence, out currentNode))
            {
                Debug.LogError($"El diálogo '{sequence.id}' no tiene un nodo inicial válido.", this);
                Close();
                return;
            }
            player = sourcePlayer;
            restoreMovement = player.enabled;
            player.enabled = false;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
            canvas.gameObject.SetActive(true);
            ShowLine();
        }

        private void ShowLine()
        {
            if (sequence.UsesNodes && currentNode != null)
                DialogueProgress.RecordNodeReached(sequence.id, currentNode.id);

            string speaker;
            string dialogueText;
            DialogueChoice[] choices = null;
            if (sequence.UsesNodes)
            {
                speaker = currentNode?.speaker;
                dialogueText = currentNode?.text;
                choices = currentNode?.VisibleChoices;
            }
            else
            {
                DialogueLine line = sequence.lines[lineIndex];
                speaker = line?.speaker;
                dialogueText = line?.text;
            }

            speakerText.text = DialoguePlayerNameTokens.Replace(speaker ?? string.Empty);
            bodyText.text = DialoguePlayerNameTokens.Replace(dialogueText ?? string.Empty);
            Sprite currentPortrait = speaker != null &&
                                     speaker.ToLowerInvariant().Contains("player")
                ? playerPortrait
                : npcPortrait;
            portrait.sprite = currentPortrait;
            portrait.gameObject.SetActive(currentPortrait != null);
            portrait.color = currentPortrait == playerPortrait ? Color.white : currentSpeakerColor;
            inputFrame = Time.frameCount;
            bool hasChoices = choices != null && choices.Length > 0;
            continueButton.gameObject.SetActive(!hasChoices);
            popup.sizeDelta = hasChoices
                ? new Vector2(980f, 520f)
                : new Vector2(980f, 390f);
            bodyText.rectTransform.offsetMin = new Vector2(54f, hasChoices ? 218f : 110f);
            BuildChoices(hasChoices ? choices : null);
            choicesReady = !hasChoices;
            choicesRevealAlpha = 0f;
            choicesCanvasGroup.alpha = hasChoices ? 0f : 1f;
            choicesCanvasGroup.interactable = !hasChoices;
            choicesCanvasGroup.blocksRaycasts = !hasChoices;
            continueButton.interactable = true;
            TMP_Text label = continueButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = sequence.UsesNodes
                    ? !currentNode.HasConditionalRoutes && IsEndTarget(currentNode.DefaultTargetLineId)
                        ? "Cerrar"
                        : "Continuar"
                    : lineIndex + 1 < sequence.lines.Length ? "Continuar" : "Cerrar";
            }
        }

        private void BuildChoices(DialogueChoice[] choices)
        {
            ClearChoiceButtons();

            bool showChoices = choices != null && choices.Length > 0;
            choicesScroll.gameObject.SetActive(showChoices);
            if (!showChoices) return;

            const float gap = 16f;
            const float horizontalHeight = 50f;
            const float sidePadding = 24f;
            float availableWidth = choicesScroll.viewport.rect.width - sidePadding * 2f;
            float[] buttonWidths = new float[choices.Length];
            float horizontalTotalWidth = gap * Mathf.Max(0, choices.Length - 1);
            choicesUseVerticalLayout = true;
            for (int i = 0; i < choices.Length; i++)
            {
                string label = DialoguePlayerNameTokens.Replace(choices[i]?.DisplayText ?? string.Empty);
                if (label.Length > 24 || label.Contains("\n"))
                {
                    choicesUseVerticalLayout = true;
                }

                buttonWidths[i] = Mathf.Clamp(64f + label.Length * 13f, 130f, 320f);
                horizontalTotalWidth += buttonWidths[i];
            }
            if (horizontalTotalWidth > availableWidth)
            {
                choicesUseVerticalLayout = true;
            }

            const float bodyHeight = 154f;
            float verticalGap = 8f;
            float choicePadding = sidePadding;
            float[] buttonHeights = new float[choices.Length];
            float contentHeight = 142f;
            if (choicesUseVerticalLayout)
            {
                float desiredHeight = choicePadding * 2f;
                for (int i = 0; i < choices.Length; i++)
                {
                    string label = DialoguePlayerNameTokens.Replace(choices[i]?.DisplayText ?? string.Empty);
                    int estimatedLines = Mathf.Max(1, Mathf.CeilToInt(label.Length / 58f));
                    buttonHeights[i] = Mathf.Clamp(34f + estimatedLines * 27f, 52f, 100f);
                    desiredHeight += buttonHeights[i] + (i < choices.Length - 1 ? verticalGap : 0f);
                }

                RectTransform canvasRect = canvas.transform as RectTransform;
                float canvasHeight = canvasRect != null && canvasRect.rect.height > 0f
                    ? canvasRect.rect.height
                    : 1080f;
                float maximumPanelHeight = Mathf.Max(420f, canvasHeight - 64f);
                float maximumChoicesHeight = Mathf.Max(64f, maximumPanelHeight - 224f - bodyHeight);
                contentHeight = desiredHeight;
                if (contentHeight > maximumChoicesHeight)
                {
                    choicePadding = 12f;
                    verticalGap = 4f;
                    float availableButtonHeight = maximumChoicesHeight - choicePadding * 2f -
                                                  verticalGap * Mathf.Max(0, choices.Length - 1);
                    float maxButtonHeight = Mathf.Max(1f, availableButtonHeight / choices.Length);
                    contentHeight = choicePadding * 2f + verticalGap * Mathf.Max(0, choices.Length - 1);
                    for (int i = 0; i < buttonHeights.Length; i++)
                    {
                        buttonHeights[i] = Mathf.Min(buttonHeights[i], maxButtonHeight);
                        contentHeight += buttonHeights[i];
                    }
                }
            }
            choicesScroll.viewport.sizeDelta = new Vector2(860f, contentHeight);
            choicesContent.sizeDelta = new Vector2(0f, contentHeight);
            popup.sizeDelta = new Vector2(980f, 224f + bodyHeight + contentHeight);
            bodyText.rectTransform.offsetMin = new Vector2(54f, 76f + contentHeight);
            bodyText.rectTransform.offsetMax = new Vector2(-54f, -148f);
            selectedChoiceIndex = 0;
            for (int index = 0; index < choices.Length; index++)
            {
                int choiceIndex = index;
                DialogueChoice choice = choices[index];
                Button button = CreateButton(
                    $"Choice_{index + 1}",
                    choicesContent,
                    DialoguePlayerNameTokens.Replace(choice?.DisplayText ?? "(Respuesta sin texto)"));
                Image choiceImage = button.GetComponent<Image>();
                choiceImage.color = new Color(0.96f, 0.95f, 0.92f, 1f);
                Outline choiceOutline = button.gameObject.AddComponent<Outline>();
                choiceOutline.effectColor = new Color(0.28f, 0.27f, 0.25f, 0.72f);
                choiceOutline.effectDistance = new Vector2(1.5f, -1.5f);
                ColorBlock choiceColors = button.colors;
                choiceColors.normalColor = choiceImage.color;
                choiceColors.highlightedColor = new Color(0.84f, 0.78f, 0.92f, 1f);
                choiceColors.selectedColor = choiceColors.highlightedColor;
                choiceColors.pressedColor = new Color(0.72f, 0.64f, 0.82f, 1f);
                button.colors = choiceColors;
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                if (choicesUseVerticalLayout)
                {
                    float precedingHeight = choicePadding;
                    for (int previous = 0; previous < index; previous++)
                    {
                        precedingHeight += buttonHeights[previous] + verticalGap;
                    }
                    buttonRect.anchorMin = new Vector2(0f, 1f);
                    buttonRect.anchorMax = new Vector2(1f, 1f);
                    buttonRect.pivot = new Vector2(0.5f, 1f);
                    buttonRect.anchoredPosition = new Vector2(0f, -precedingHeight);
                    buttonRect.sizeDelta = new Vector2(0f, buttonHeights[index]);
                }
                else
                {
                    float x = -horizontalTotalWidth * 0.5f + buttonWidths[index] * 0.5f;
                    for (int previous = 0; previous < index; previous++)
                    {
                        x += buttonWidths[previous] + gap;
                    }
                    buttonRect.anchorMin = new Vector2(0.5f, 1f);
                    buttonRect.anchorMax = new Vector2(0.5f, 1f);
                    buttonRect.pivot = new Vector2(0.5f, 1f);
                    buttonRect.anchoredPosition = new Vector2(
                        x,
                        -(contentHeight - horizontalHeight) * 0.5f);
                    buttonRect.sizeDelta = new Vector2(buttonWidths[index], horizontalHeight);
                }
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                label.alignment = choicesUseVerticalLayout
                    ? TextAlignmentOptions.MidlineLeft
                    : TextAlignmentOptions.Center;
                if (choicesUseVerticalLayout)
                {
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 12f;
                    label.fontSizeMax = 24f;
                }
                label.color = new Color(0.12f, 0.12f, 0.12f);
                label.richText = true;
                label.margin = new Vector4(48f, 4f, 20f, 4f);
                Image pointer = DialogueChoiceVisuals.CreatePixelArrow(buttonRect, 24f);
                choicePointers.Add(pointer);
                DialogueChoiceVisuals.AddPointerEnter(button, () =>
                {
                    selectedChoiceIndex = choiceIndex;
                    UpdateChoiceSelection();
                });
                button.onClick.AddListener(() => SelectChoice(choiceIndex));
                choiceButtons.Add(button);
            }

            choicesScroll.verticalNormalizedPosition = 1f;
            UpdateChoiceSelection();
        }

        private void ClearChoiceButtons()
        {
            foreach (Button button in choiceButtons)
            {
                if (button != null) Destroy(button.gameObject);
            }
            choiceButtons.Clear();
            choicePointers.Clear();
        }

        private void UpdateChoiceSelection()
        {
            if (choiceButtons.Count == 0) return;
            selectedChoiceIndex = Mathf.Clamp(selectedChoiceIndex, 0, choiceButtons.Count - 1);
            EventSystem.current?.SetSelectedGameObject(choiceButtons[selectedChoiceIndex].gameObject);
            for (int index = 0; index < choicePointers.Count; index++)
                choicePointers[index].gameObject.SetActive(index == selectedChoiceIndex);
            choicesScroll.verticalNormalizedPosition =
                1f - (float)selectedChoiceIndex / Mathf.Max(1, choiceButtons.Count - 1);
        }

        private void UpdateChoiceReveal()
        {
            if (choiceButtons.Count == 0 || choicesReady) return;

            choicesRevealAlpha = Mathf.MoveTowards(choicesRevealAlpha, 1f, Time.unscaledDeltaTime * 5f);
            choicesCanvasGroup.alpha = choicesRevealAlpha;
            if (choicesRevealAlpha < 1f) return;

            choicesReady = true;
            choicesCanvasGroup.interactable = true;
            choicesCanvasGroup.blocksRaycasts = true;
            UpdateChoiceSelection();
        }

        private void SelectChoice(int index)
        {
            DialogueChoice[] visibleChoices = currentNode?.VisibleChoices;
            if (sequence == null || visibleChoices == null ||
                index < 0 || index >= visibleChoices.Length || Time.frameCount == inputFrame || !choicesReady)
                return;

            inputFrame = Time.frameCount;
            RetroFeedbackAudio.PlayInteraction();
            DialogueChoice selectedChoice = visibleChoices[index];
            string optionKey = string.IsNullOrWhiteSpace(selectedChoice.key)
                ? selectedChoice.text
                : selectedChoice.key.Trim();
            ChoiceSelected?.Invoke(new ChoiceContext(sequence.id, currentNode.id, optionKey));
            string targetId = selectedChoice.TargetLineId;
            if (IsEndTarget(targetId))
            {
                CompleteConversation();
                return;
            }

            GoToNode(targetId);
        }

        private void GoToNode(string nodeId)
        {
            if (IsEndTarget(nodeId))
            {
                CompleteConversation();
                return;
            }
            if (!catalog.TryGetNode(sequence, nodeId, out DialogueNode nextNode))
            {
                Debug.LogError($"El nodo '{nodeId}' no existe o está duplicado en el diálogo '{sequence.id}'.", this);
                Close();
                return;
            }

            currentNode = nextNode;
            ShowLine();
        }

        private void CompleteConversation()
        {
            DialogueSequence completedSequence = sequence;
            DialogueNode terminalNode = currentNode;
            PlayerMovementController recipient = player;
            Close();
            DialogueCompletion.Apply(completedSequence, recipient, terminalNode);
        }

        private static bool IsEndTarget(string targetId) =>
            string.IsNullOrWhiteSpace(targetId) ||
            string.Equals(targetId.Trim(), DialogueSequence.EndToken, System.StringComparison.OrdinalIgnoreCase);

        private void Update()
        {
            if (sequence == null) return;
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                Close();
                return;
            }

            UpdateChoiceReveal();

            if (GameplayInputLock.IsLocked || Time.frameCount == inputFrame)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                (pad != null && pad.buttonEast.wasPressedThisFrame))
            {
                Close();
            }
            else if (choiceButtons.Count > 0)
            {
                if (!choicesReady) return;

                bool up = keyboard != null &&
                          (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame);
                bool down = keyboard != null &&
                            (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame);
                up |= pad != null && pad.dpad.up.wasPressedThisFrame;
                down |= pad != null && pad.dpad.down.wasPressedThisFrame;
                if (up || down)
                {
                    selectedChoiceIndex = (selectedChoiceIndex + (down ? 1 : -1) + choiceButtons.Count) % choiceButtons.Count;
                    UpdateChoiceSelection();
                }
                else if ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)) ||
                         (pad != null && pad.buttonSouth.wasPressedThisFrame))
                {
                    SelectChoice(selectedChoiceIndex);
                }
                return;
            }
            else if ((keyboard != null &&
                      (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) ||
                     (pad != null && pad.buttonSouth.wasPressedThisFrame))
            {
                Advance();
            }
        }

        public static void AdvanceCurrent()
        {
            if (IsOpen) instance.Advance();
        }

        public void Advance()
        {
            if (sequence == null || Time.frameCount == inputFrame) return;
            if (sequence.UsesNodes && currentNode.VisibleChoices.Length > 0)
            {
                return;
            }

            inputFrame = Time.frameCount;
            RetroFeedbackAudio.PlayInteraction();
            if (sequence.UsesNodes)
            {
                string nextNodeId = catalog.ResolveNextNode(sequence, currentNode);
                if (IsEndTarget(nextNodeId))
                {
                    CompleteConversation();
                }
                else
                {
                    GoToNode(nextNodeId);
                }
                return;
            }
            if (++lineIndex < sequence.lines.Length)
            {
                ShowLine();
                return;
            }

            DialogueSequence completedSequence = sequence;
            PlayerMovementController recipient = player;
            Close();
            DialogueCompletion.Apply(completedSequence, recipient);
        }

        public void Close()
        {
            if (player != null && restoreMovement) player.enabled = true;
            player = null;
            restoreMovement = false;
            sequence = null;
            currentNode = null;
            catalog = null;
            ClearChoiceButtons();
            if (canvas != null) canvas.gameObject.SetActive(false);
            LastClosedFrame = Time.frameCount;
        }

        private void OnDisable()
        {
            if (sequence != null) Close();
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (instance != this) return;
            Close();
            instance = null;
        }

        private void BuildInterface()
        {
            GameObject root = new GameObject("DialogoOfficialCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2100;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = new GameObject("DialoguePopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            popup = panel.GetComponent<RectTransform>();
            popup.anchorMin = new Vector2(0.5f, 0.5f);
            popup.anchorMax = new Vector2(0.5f, 0.5f);
            popup.pivot = new Vector2(0.5f, 0.5f);
            popup.sizeDelta = new Vector2(980f, 390f);
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.91f, 0.90f, 0.88f, 1f);
            panelImage.sprite = GetRoundedSprite();
            panelImage.type = Image.Type.Sliced;
            panelImage.raycastTarget = true;
            Shadow panelShadow = panel.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(0f, 0f, 0f, 0.24f);
            panelShadow.effectDistance = new Vector2(0f, -10f);

            portrait = CreateImage("NPCHeadPortrait", popup, Color.white);
            portrait.rectTransform.anchorMin = new Vector2(0f, 1f);
            portrait.rectTransform.anchorMax = new Vector2(0f, 1f);
            portrait.rectTransform.pivot = new Vector2(0f, 1f);
            portrait.rectTransform.anchoredPosition = new Vector2(30f, -24f);
            portrait.rectTransform.sizeDelta = new Vector2(104f, 104f);
            portrait.preserveAspect = true;

            speakerText = CreateText("SpeakerName", popup, 34f, TextAlignmentOptions.MidlineLeft);
            SetAnchors(speakerText.rectTransform, new Vector2(0f, 1f), Vector2.one);
            speakerText.rectTransform.pivot = new Vector2(0f, 1f);
            speakerText.rectTransform.offsetMin = new Vector2(154f, -96f);
            speakerText.rectTransform.offsetMax = new Vector2(-64f, -28f);
            speakerText.fontStyle = FontStyles.Bold;
            speakerText.color = new Color(0.12f, 0.12f, 0.12f);

            bodyText = CreateText("DialogueText", popup, 30f, TextAlignmentOptions.Top);
            bodyText.enableAutoSizing = true;
            bodyText.fontSizeMin = 20f;
            bodyText.fontSizeMax = 30f;
            SetAnchors(bodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            bodyText.rectTransform.offsetMin = new Vector2(54f, 110f);
            bodyText.rectTransform.offsetMax = new Vector2(-54f, -148f);

            GameObject viewportObject = new GameObject("ChoicesViewport",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(ScrollRect));
            viewportObject.transform.SetParent(popup, false);
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0.5f, 0f);
            viewportRect.anchorMax = new Vector2(0.5f, 0f);
            viewportRect.pivot = new Vector2(0.5f, 0f);
            viewportRect.anchoredPosition = new Vector2(0f, 96f);
            viewportRect.sizeDelta = new Vector2(860f, 142f);
            Image viewportImage = viewportObject.GetComponent<Image>();
            // The Image must have opaque alpha for Mask to write its stencil.
            // Mask.showMaskGraphic keeps this viewport graphic itself invisible.
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = true;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;
            choicesScroll = viewportObject.GetComponent<ScrollRect>();
            choicesCanvasGroup = viewportObject.AddComponent<CanvasGroup>();
            choicesScroll.horizontal = false;
            choicesScroll.vertical = true;
            choicesScroll.movementType = ScrollRect.MovementType.Clamped;
            choicesScroll.scrollSensitivity = 48f;

            GameObject contentObject = new GameObject("ChoicesContent", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);
            choicesContent = contentObject.GetComponent<RectTransform>();
            choicesContent.anchorMin = new Vector2(0f, 1f);
            choicesContent.anchorMax = Vector2.one;
            choicesContent.pivot = new Vector2(0.5f, 1f);
            choicesContent.anchoredPosition = Vector2.zero;
            choicesContent.sizeDelta = Vector2.zero;
            choicesScroll.viewport = viewportRect;
            choicesScroll.content = choicesContent;
            viewportObject.SetActive(false);

            continueButton = CreateButton("ContinueButton", popup, "Continuar");
            RectTransform buttonRect = continueButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.anchoredPosition = new Vector2(-38f, 28f);
            buttonRect.sizeDelta = new Vector2(210f, 62f);
            continueButton.onClick.AddListener(Advance);

            Button closeButton = CreateButton("CloseButton", popup, "×");
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-20f, -18f);
            closeRect.sizeDelta = new Vector2(48f, 48f);
            closeButton.onClick.AddListener(Close);

            root.AddComponent<CanvasGroup>();
            root.SetActive(false);
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, float size, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.font = GetDialogFont();
            text.fontSize = size;
            text.color = new Color(0.12f, 0.12f, 0.12f);
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static TMP_FontAsset GetDialogFont()
        {
            // The project's global TMP font is PressStart2P. This presentation
            // intentionally uses the standard non-pixel Liberation Sans asset.
            TMP_FontAsset standardFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/LiberationSans SDF");
            return standardFont != null ? standardFont : TMP_Settings.defaultFontAsset;
        }

        private static Sprite GetRoundedSprite()
        {
            if (roundedSprite != null) return roundedSprite;

            const int size = 64;
            const float radius = 18f;
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "DialogoOfficialRoundedTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[size * size];
            float half = size * 0.5f;
            Vector2 innerBounds = Vector2.one * (half - radius);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(Mathf.Abs(x + 0.5f - half), Mathf.Abs(y + 0.5f - half));
                    Vector2 distance = point - innerBounds;
                    Vector2 outside = new Vector2(Mathf.Max(distance.x, 0f), Mathf.Max(distance.y, 0f));
                    float signedDistance = outside.magnitude +
                        Mathf.Min(Mathf.Max(distance.x, distance.y), 0f) - radius;
                    float alpha = Mathf.Clamp01(0.5f - signedDistance / 1.25f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            roundedTexture.SetPixels(pixels);
            roundedTexture.Apply(false, true);
            roundedSprite = Sprite.Create(
                roundedTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            return roundedSprite;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.96f, 0.95f, 0.92f, 1f);
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.84f, 0.88f, 0.91f, 1f);
            colors.pressedColor = new Color(0.72f, 0.78f, 0.82f, 1f);
            button.colors = colors;
            TMP_Text text = CreateText("Label", go.transform, 24f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.text = label;
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
        }
    }
}
