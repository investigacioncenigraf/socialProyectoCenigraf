using System.Collections.Generic;
using SocialProyectoCenigraf.Debugging.State;
using SocialProyectoCenigraf.Player.Movement;
using TMPro;
using SocialProyectoCenigraf.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.Dialogue
{
    public sealed class DialoguePresenter : MonoBehaviour
    {
        private static DialoguePresenter instance;
        public static bool IsOpen => instance != null && instance.sequence != null;
        [SerializeField, Min(1f)] private float charactersPerSecond = 35f;
        private DialogueCatalog catalog;
        private DialogueSequence sequence;
        private DialogueNode currentNode;
        private int lineIndex;
        private float revealed;
        private int inputFrame;
        private Canvas canvas;
        private RectTransform lowerBar;
        private TMP_Text speakerText, bodyText, hintText;
        private RectTransform choicesRoot;
        private CanvasGroup choicesCanvasGroup;
        private readonly List<Button> choiceButtons = new List<Button>();
        private readonly List<Image> choiceBackgrounds = new List<Image>();
        private readonly List<Image> choicePointers = new List<Image>();
        private int selectedChoiceIndex;
        private bool choicesReady;
        private float choicesRevealAlpha;
        private PlayerMovementController player;
        private bool restoreMovement;
        private bool mobileLayout;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; LastClosedFrame = -1; }

        public static bool TryOpen(
            DialogueCatalog catalog,
            string id,
            PlayerMovementController sourcePlayer,
            Sprite speakerPortrait = null,
            Color speakerColor = default,
            Sprite playerPortrait = null)
        {
            if (IsOpen || DialogoOfficial.IsOpen ||
                SocialProyectoCenigraf.Rewards.RewardWindow.IsOpen || sourcePlayer == null) return false;
            if (catalog == null || !catalog.TryGet(id, out var dialogue))
            {
                Debug.LogWarning($"Diálogo '{id}' vacío, inexistente o duplicado en el catálogo.");
                return false;
            }
            // The player's text animation decides the presenter, for every conversation:
            // both render nodes and plain lines, so the format of the entry is not part
            // of the choice any more.
            bool useLegacyPresentation = DebugStore.Instance.State.DialogueMode ==
                                         DialoguePresentationMode.LegacyCinematic;
            if (!useLegacyPresentation)
            {
                return DialogoOfficial.TryOpen(
                    catalog,
                    id,
                    sourcePlayer,
                    speakerPortrait,
                    speakerColor,
                    playerPortrait);
            }
            if (instance == null)
                instance = new GameObject("DialogueSystem").AddComponent<DialoguePresenter>();
            return instance.Open(catalog, dialogue, sourcePlayer);
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            SceneManager.activeSceneChanged += OnSceneChanged;
            BuildInterface();
        }
        private void OnSceneChanged(Scene previous, Scene next) => Close();
        private bool Open(
            DialogueCatalog sourceCatalog,
            DialogueSequence dialogue,
            PlayerMovementController sourcePlayer)
        {
            catalog = sourceCatalog;
            sequence = dialogue;
            lineIndex = 0;
            currentNode = null;
            selectedChoiceIndex = 0;
            if (sequence.UsesNodes && !catalog.ResolveStartNode(sequence, out currentNode))
            {
                sequence = null;
                catalog = null;
                return false;
            }
            player = sourcePlayer; restoreMovement = player.enabled;
            player.enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
            canvas.gameObject.SetActive(true);
            ShowLine();
            return true;
        }
        private void ShowLine()
        {
            if (sequence.UsesNodes && currentNode != null)
                DialogueProgress.RecordNodeReached(sequence.id, currentNode.id);

            DialogueLine line = null;
            if (sequence.UsesNodes)
            {
                line = new DialogueLine
                {
                    speaker = currentNode?.speaker,
                    text = currentNode?.text
                };
            }
            else
            {
                line = sequence.lines[lineIndex];
            }
            speakerText.text = DialoguePlayerNameTokens.Replace(line?.speaker ?? "");
            bodyText.text = DialoguePlayerNameTokens.Replace(line?.text ?? "");
            bodyText.maxVisibleCharacters = 0;
            bodyText.ForceMeshUpdate();
            revealed = 0; inputFrame = Time.frameCount;
            ClearChoiceButtons();
            DialogueChoice[] choices = sequence.UsesNodes ? currentNode.VisibleChoices : null;
            bool hasChoices = choices != null && choices.Length > 0;
            lowerBar.anchorMax = new Vector2(1f, hasChoices ? (mobileLayout ? 0.72f : 0.52f) : (mobileLayout ? 0.38f : 0.27f));
            choicesRoot.gameObject.SetActive(hasChoices);
            choicesReady = !hasChoices;
            choicesRevealAlpha = 0f;
            choicesCanvasGroup.alpha = 0f;
            choicesCanvasGroup.interactable = false;
            choicesCanvasGroup.blocksRaycasts = false;
            if (hasChoices && mobileLayout)
            {
                speakerText.rectTransform.anchorMin = new Vector2(0.06f, 0.91f);
                speakerText.rectTransform.anchorMax = new Vector2(0.94f, 0.99f);
                bodyText.rectTransform.anchorMin = new Vector2(0.06f, 0.77f);
                bodyText.rectTransform.anchorMax = new Vector2(0.94f, 0.89f);
                hintText.rectTransform.anchorMin = new Vector2(0.06f, 0.08f);
                hintText.rectTransform.anchorMax = new Vector2(0.94f, 0.23f);
            }
            else if (hasChoices)
            {
                speakerText.rectTransform.anchorMin = new Vector2(0.06f, 0.87f);
                speakerText.rectTransform.anchorMax = new Vector2(0.94f, 0.98f);
                bodyText.rectTransform.anchorMin = new Vector2(0.06f, 0.68f);
                bodyText.rectTransform.anchorMax = new Vector2(0.94f, 0.85f);
                hintText.rectTransform.anchorMin = new Vector2(0.06f, 0.02f);
                hintText.rectTransform.anchorMax = new Vector2(0.94f, 0.10f);
            }
            else
            {
                speakerText.rectTransform.anchorMin = new Vector2(0.06f, 0.73f);
                speakerText.rectTransform.anchorMax = new Vector2(mobileLayout ? 0.78f : 0.94f, 0.95f);
                bodyText.rectTransform.anchorMin = new Vector2(0.06f, 0.23f);
                bodyText.rectTransform.anchorMax = new Vector2(mobileLayout ? 0.78f : 0.94f, 0.72f);
                hintText.rectTransform.anchorMin = new Vector2(0.06f, 0.04f);
                hintText.rectTransform.anchorMax = new Vector2(mobileLayout ? 0.78f : 0.94f, 0.19f);
            }
            if (hasChoices) BuildChoices(choices);
            SetHint("Mostrar texto completo");
        }
        private void Update()
        {
            if (sequence == null) return;
            if (player == null || !player.gameObject.activeInHierarchy) { Close(); return; }
            int total = bodyText.textInfo.characterCount;
            int previousVisible = bodyText.maxVisibleCharacters;
            revealed = Mathf.Min(total, revealed + Time.unscaledDeltaTime * charactersPerSecond);
            bodyText.maxVisibleCharacters = Mathf.FloorToInt(revealed);
            for (int i = previousVisible; i < bodyText.maxVisibleCharacters; i++)
                if (char.IsLetterOrDigit(bodyText.textInfo.characterInfo[i].character))
                { RetroFeedbackAudio.PlayLetter(); break; }
            bool textComplete = bodyText.maxVisibleCharacters >= total;
            UpdateChoiceReveal(textComplete);
            if (textComplete)
            {
                if (choiceButtons.Count > 0)
                    SetHint(choicesReady
                        ? "↑ / ↓ Elegir respuesta     Enter / E Confirmar     Esc Cerrar"
                        : string.Empty);
                else
                    SetHint(sequence.UsesNodes
                        ? "E · Continuar     Esc · Cerrar"
                        : lineIndex + 1 < sequence.lines.Length
                            ? "E · Continuar     Esc · Cerrar"
                            : "E · Terminar     Esc · Cerrar");
            }
            if (SocialProyectoCenigraf.Controls.GameplayInputLock.IsLocked || Time.frameCount == inputFrame) return;

            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                (gamepad != null && gamepad.buttonEast.wasPressedThisFrame))
            {
                Close();
                return;
            }

            if (choiceButtons.Count > 0)
            {
                if (!textComplete)
                {
                    if ((keyboard != null && keyboard.eKey.wasPressedThisFrame) ||
                        (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame))
                        RevealAllText();
                    return;
                }
                if (!choicesReady) return;

                bool up = keyboard != null &&
                          (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame);
                bool down = keyboard != null &&
                            (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame);
                up |= gamepad != null && gamepad.dpad.up.wasPressedThisFrame;
                down |= gamepad != null && gamepad.dpad.down.wasPressedThisFrame;
                if (up || down)
                {
                    selectedChoiceIndex = (selectedChoiceIndex + (down ? 1 : -1) + choiceButtons.Count) % choiceButtons.Count;
                    UpdateChoiceSelection();
                }
                else if ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)) ||
                         (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame))
                {
                    SelectChoice(selectedChoiceIndex);
                }
                return;
            }

            bool advancePressed =
                (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) ||
                (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
            if (advancePressed) Advance();
        }
        public static void AdvanceCurrent()
        {
            if (!IsOpen) return;
            if (instance.choiceButtons.Count > 0)
            {
                if (instance.bodyText.maxVisibleCharacters < instance.bodyText.textInfo.characterCount)
                    instance.RevealAllText();
                else
                    instance.SelectChoice(instance.selectedChoiceIndex);
                return;
            }
            instance.Advance();
        }
        public void Advance()
        {
            if (sequence == null || Time.frameCount == inputFrame) return;
            int total = bodyText.textInfo.characterCount;
            if (bodyText.maxVisibleCharacters < total)
            {
                RetroFeedbackAudio.PlayInteraction();
                RevealAllText();
                return;
            }
            if (sequence.UsesNodes)
            {
                if (choiceButtons.Count > 0)
                {
                    SelectChoice(selectedChoiceIndex);
                    return;
                }
                string targetId = catalog.ResolveNextNode(sequence, currentNode);
                if (IsEndTarget(targetId)) CompleteConversation();
                else GoToNode(targetId);
                return;
            }
            inputFrame = Time.frameCount;
            RetroFeedbackAudio.PlayInteraction();
            if (++lineIndex >= sequence.lines.Length)
            {
                DialogueSequence completedSequence = sequence;
                var recipient = player;
                Close();
                DialogueCompletion.Apply(completedSequence, recipient);
            }
            else ShowLine();
        }
        public void Close()
        {
            if (player != null && restoreMovement) player.enabled = true;
            player = null; restoreMovement = false; sequence = null; currentNode = null; catalog = null;
            ClearChoiceButtons();
            if (canvas != null) canvas.gameObject.SetActive(false);
            // Keep E from reopening the same dialogue during this frame.
            LastClosedFrame = Time.frameCount;
        }

        private void RevealAllText()
        {
            revealed = bodyText.textInfo.characterCount;
            bodyText.maxVisibleCharacters = bodyText.textInfo.characterCount;
            inputFrame = Time.frameCount;
        }

        private void UpdateChoiceReveal(bool textComplete)
        {
            if (choiceButtons.Count == 0 || choicesReady || !textComplete) return;

            choicesRevealAlpha = Mathf.MoveTowards(choicesRevealAlpha, 1f, Time.unscaledDeltaTime * 5f);
            choicesCanvasGroup.alpha = choicesRevealAlpha;
            if (choicesRevealAlpha < 1f) return;

            choicesReady = true;
            choicesCanvasGroup.interactable = true;
            choicesCanvasGroup.blocksRaycasts = true;
            UpdateChoiceSelection();
        }

        private void BuildChoices(DialogueChoice[] choices)
        {
            selectedChoiceIndex = 0;
            for (int index = 0; index < choices.Length; index++)
            {
                DialogueChoice choice = choices[index];
                string text = DialoguePlayerNameTokens.Replace(
                    choice?.DisplayText ?? "(Respuesta sin texto)");
                int lines = Mathf.Max(1, Mathf.CeilToInt(text.Length / 82f));
                float height = Mathf.Clamp(22f + lines * 26f, 48f, 74f);
                GameObject buttonObject = new GameObject(
                    $"Choice_{index + 1}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                buttonObject.transform.SetParent(choicesRoot, false);
                RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0f, 1f);
                buttonRect.anchorMax = new Vector2(1f, 1f);
                buttonRect.pivot = new Vector2(0.5f, 1f);
                float preceding = 8f;
                for (int previous = 0; previous < index; previous++)
                    preceding += choiceButtons[previous].GetComponent<RectTransform>().sizeDelta.y + 7f;
                buttonRect.anchoredPosition = new Vector2(0f, -preceding);
                buttonRect.sizeDelta = new Vector2(0f, height);

                Image image = buttonObject.GetComponent<Image>();
                image.color = new Color(0.42f, 0.34f, 0.52f, 0f);
                Button button = buttonObject.GetComponent<Button>();
                button.transition = Selectable.Transition.None;

                TMP_Text label = Text("Label", buttonRect, Vector2.zero, Vector2.one, 16f);
                label.text = text;
                label.enableWordWrapping = true;
                label.enableAutoSizing = true;
                label.fontSizeMin = 18f;
                label.fontSizeMax = 24f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.margin = new Vector4(46f, 3f, 12f, 3f);
                Image pointer = DialogueChoiceVisuals.CreatePixelArrow(buttonRect, 24f);
                choiceBackgrounds.Add(image);
                choicePointers.Add(pointer);
                int choiceIndex = index;
                DialogueChoiceVisuals.AddPointerEnter(button, () =>
                {
                    selectedChoiceIndex = choiceIndex;
                    UpdateChoiceSelection();
                });
                button.onClick.AddListener(() => SelectChoice(choiceIndex));
                choiceButtons.Add(button);
            }
            UpdateChoiceSelection();
        }

        private void SelectChoice(int index)
        {
            DialogueChoice[] choices = currentNode?.VisibleChoices;
            if (sequence == null || choices == null || index < 0 || index >= choices.Length ||
                Time.frameCount == inputFrame || !choicesReady) return;
            if (bodyText.maxVisibleCharacters < bodyText.textInfo.characterCount)
            {
                RevealAllText();
                return;
            }

            inputFrame = Time.frameCount;
            RetroFeedbackAudio.PlayInteraction();
            string targetId = choices[index].TargetLineId;
            if (IsEndTarget(targetId)) CompleteConversation();
            else GoToNode(targetId);
        }

        private void GoToNode(string nodeId)
        {
            if (IsEndTarget(nodeId))
            {
                CompleteConversation();
                return;
            }
            if (!catalog.TryGetNode(sequence, nodeId, out currentNode))
            {
                Debug.LogError($"El nodo '{nodeId}' no existe o está duplicado en el diálogo '{sequence.id}'.", this);
                Close();
                return;
            }
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

        private void UpdateChoiceSelection()
        {
            if (choiceButtons.Count == 0) return;
            selectedChoiceIndex = Mathf.Clamp(selectedChoiceIndex, 0, choiceButtons.Count - 1);
            EventSystem.current?.SetSelectedGameObject(choiceButtons[selectedChoiceIndex].gameObject);
            for (int index = 0; index < choiceButtons.Count; index++)
            {
                choicePointers[index].gameObject.SetActive(index == selectedChoiceIndex);
                choiceBackgrounds[index].color = index == selectedChoiceIndex
                    ? new Color32(130, 108, 165, 72)
                    : new Color(0.42f, 0.34f, 0.52f, 0f);
            }
        }

        private void ClearChoiceButtons()
        {
            foreach (Button button in choiceButtons)
                if (button != null) Destroy(button.gameObject);
            choiceButtons.Clear();
            choiceBackgrounds.Clear();
            choicePointers.Clear();
        }

        private void SetHint(string message)
        {
            if (mobileLayout)
            {
                hintText.text = choiceButtons.Count > 0
                    ? "Toca una respuesta para elegir"
                    : $"Toca E para {message.ToLowerInvariant()}";
                return;
            }

            hintText.text = message;
        }
        public static int LastClosedFrame { get; private set; } = -1;
        private void OnDisable() { if (sequence != null) Close(); }
        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (instance != this) return;
            Close(); instance = null;
        }
        private void BuildInterface()
        {
            var go = new GameObject("CinematicDialogue", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080);scaler.matchWidthOrHeight = 0.5f;
            Bar("TopBar", new Vector2(0,0.86f), Vector2.one);
            mobileLayout = SocialProyectoCenigraf.Mobile.MobileControls.IsMobileDevice;
            lowerBar = Bar("BottomBar", Vector2.zero, new Vector2(1, mobileLayout ? 0.38f : 0.27f));
            RectTransform lower = lowerBar;
            speakerText = Text("Speaker", lower, new Vector2(0.06f,0.73f), new Vector2(0.94f,0.95f),28);
            speakerText.color = new Color(0.75f,0.68f,0.9f);
            bodyText = Text("DialogueText", lower, new Vector2(0.06f,0.23f), new Vector2(0.94f,0.72f),32);
            bodyText.enableAutoSizing = true;bodyText.fontSizeMin = 18;bodyText.fontSizeMax = 32;
            hintText = Text("Controls", lower, new Vector2(0.06f,0.04f), new Vector2(0.94f,0.19f),20);
            bodyText.richText = true;
            if (mobileLayout)
            {
                bodyText.rectTransform.anchorMin = new Vector2(0.06f, 0.23f);
                bodyText.rectTransform.anchorMax = new Vector2(0.94f, 0.72f);
                hintText.rectTransform.anchorMin = new Vector2(0.06f, 0.04f);
                hintText.rectTransform.anchorMax = new Vector2(0.94f, 0.19f);
            }
            hintText.color = new Color(0.7f,0.7f,0.7f);hintText.alignment=TextAlignmentOptions.BottomRight;
            GameObject choicesObject = new GameObject("Choices", typeof(RectTransform));
            choicesObject.transform.SetParent(lower, false);
            choicesRoot = choicesObject.GetComponent<RectTransform>();
            choicesCanvasGroup = choicesObject.AddComponent<CanvasGroup>();
            choicesRoot.anchorMin = mobileLayout ? new Vector2(0.04f, 0.27f) : new Vector2(0.06f, 0.12f);
            choicesRoot.anchorMax = mobileLayout ? new Vector2(0.96f, 0.74f) : new Vector2(0.96f, 0.64f);
            choicesRoot.offsetMin = choicesRoot.offsetMax = Vector2.zero;
            choicesObject.SetActive(false);
            go.SetActive(false);
        }
        private RectTransform Bar(string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            go.transform.SetParent(canvas.transform,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=go.GetComponent<Image>();image.color=Color.black;image.raycastTarget=false;
            return rect;
        }
        private TMP_Text Text(string name,RectTransform parent,Vector2 min,Vector2 max,float size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));
            go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var text=go.GetComponent<TextMeshProUGUI>();text.font=TMP_Settings.defaultFontAsset;
            text.fontSize=size;text.color=Color.white;text.richText=true;text.raycastTarget=false;
            text.alignment=TextAlignmentOptions.TopLeft;
            return text;
        }
    }
}
