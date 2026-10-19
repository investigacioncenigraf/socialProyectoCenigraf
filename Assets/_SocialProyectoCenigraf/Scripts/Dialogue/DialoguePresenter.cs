using SocialProyectoCenigraf.Player.Movement;
using TMPro;
using SocialProyectoCenigraf.Audio;
using UnityEngine;
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
        private DialogueSequence sequence;
        private int lineIndex;
        private float revealed;
        private int inputFrame;
        private Canvas canvas;
        private TMP_Text speakerText, bodyText, hintText;
        private PlayerMovementController player;
        private bool restoreMovement;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; LastClosedFrame = -1; }

        public static bool TryOpen(DialogueCatalog catalog, string id, PlayerMovementController sourcePlayer)
        {
            if (IsOpen || SocialProyectoCenigraf.Rewards.RewardWindow.IsOpen || sourcePlayer == null) return false;
            if (catalog == null || !catalog.TryGet(id, out var dialogue))
            {
                Debug.LogWarning($"Diálogo '{id}' vacío, inexistente o duplicado en el catálogo.");
                return false;
            }
            if (instance == null)
                instance = new GameObject("DialogueSystem").AddComponent<DialoguePresenter>();
            instance.Open(dialogue, sourcePlayer);
            return true;
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            SceneManager.activeSceneChanged += OnSceneChanged;
            BuildInterface();
        }
        private void OnSceneChanged(Scene previous, Scene next) => Close();
        private void Open(DialogueSequence dialogue, PlayerMovementController sourcePlayer)
        {
            sequence = dialogue; lineIndex = 0;
            player = sourcePlayer; restoreMovement = player.enabled;
            player.enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
            canvas.gameObject.SetActive(true);
            ShowLine();
        }
        private void ShowLine()
        {
            var line = sequence.lines[lineIndex];
            speakerText.text = line?.speaker ?? "";
            bodyText.text = line?.text ?? "";
            bodyText.maxVisibleCharacters = 0;
            bodyText.ForceMeshUpdate();
            revealed = 0; inputFrame = Time.frameCount;
            hintText.text = SocialProyectoCenigraf.Mobile.MobileControls.IsMobileDevice
                ? "Toca E para mostrar el texto" : "E · Mostrar texto completo     Esc · Cerrar";
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
            if (bodyText.maxVisibleCharacters >= total)
                hintText.text = lineIndex + 1 < sequence.lines.Length
                    ? (SocialProyectoCenigraf.Mobile.MobileControls.IsMobileDevice ? "Toca E para continuar" : "E · Continuar     Esc · Cerrar")
                    : (SocialProyectoCenigraf.Mobile.MobileControls.IsMobileDevice ? "Toca E para terminar" : "E · Terminar     Esc · Cerrar");
            if (Time.frameCount == inputFrame || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (!Keyboard.current.eKey.wasPressedThisFrame) return;
            Advance();
        }
        public static void AdvanceCurrent()
        {
            if (IsOpen) instance.Advance();
        }
        public void Advance()
        {
            if (sequence == null || Time.frameCount == inputFrame) return;
            RetroFeedbackAudio.PlayInteraction();
            int total = bodyText.textInfo.characterCount;
            inputFrame = Time.frameCount;
            if (bodyText.maxVisibleCharacters < total) { revealed = total; bodyText.maxVisibleCharacters = total; return; }
            if (++lineIndex >= sequence.lines.Length)
            {
                var reward = sequence.completionReward;
                var recipient = player;
                Close();
                if(reward != null) SocialProyectoCenigraf.Rewards.RewardWindow.GrantAndShow(reward,recipient);
            }
            else ShowLine();
        }
        public void Close()
        {
            if (player != null && restoreMovement) player.enabled = true;
            player = null; restoreMovement = false; sequence = null;
            if (canvas != null) canvas.gameObject.SetActive(false);
            // Keep E from reopening the same dialogue during this frame.
            LastClosedFrame = Time.frameCount;
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
            var go = new GameObject("CinematicDialogue", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080);scaler.matchWidthOrHeight = 0.5f;
            Bar("TopBar", new Vector2(0,0.86f), Vector2.one);
            RectTransform lower = Bar("BottomBar", Vector2.zero, new Vector2(1, SocialProyectoCenigraf.Mobile.MobileControls.IsMobileDevice ? 0.38f : 0.27f));
            speakerText = Text("Speaker", lower, new Vector2(0.06f,0.73f), new Vector2(0.94f,0.95f),28);
            speakerText.color = new Color(0.75f,0.68f,0.9f);
            bodyText = Text("DialogueText", lower, new Vector2(0.06f,0.23f), new Vector2(0.94f,0.72f),32);
            bodyText.enableAutoSizing = true;bodyText.fontSizeMin = 18;bodyText.fontSizeMax = 32;
            hintText = Text("Controls", lower, new Vector2(0.06f,0.04f), new Vector2(0.94f,0.19f),20);
            if (SocialProyectoCenigraf.Mobile.MobileControls.IsMobileDevice)
            {
                bodyText.rectTransform.anchorMax = new Vector2(0.78f, 0.72f);
                hintText.rectTransform.anchorMax = new Vector2(0.78f, 0.19f);
            }
            hintText.color = new Color(0.7f,0.7f,0.7f);hintText.alignment=TextAlignmentOptions.BottomRight;
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
            text.fontSize=size;text.color=Color.white;text.richText=false;text.raycastTarget=false;
            text.alignment=TextAlignmentOptions.TopLeft;
            return text;
        }
    }
}
