using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    public sealed class GameplayHUDView : MonoBehaviour
    {
        private static readonly Color SceneLabelBackground =
            new Color32(30, 45, 61, 232);
        private static readonly Color SceneLabelText =
            new Color32(244, 249, 255, 255);

        private TMP_Text sceneNameText;
        private RectTransform sceneNameLabelRect;

        [Header("Player summary")]
        [SerializeField] private Graphic playerAvatarGraphic;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerLevelText;
        [SerializeField] private RectTransform playerExperienceFill;
        [SerializeField] private TMP_Text playerRoleText;
        [SerializeField] private Button playerInfoButton;

        [Header("Menu")]
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            CreateSceneNameLabel();
            UpdateSceneName(SceneManager.GetActiveScene(), SceneManager.GetActiveScene());
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += UpdateSceneName;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= UpdateSceneName;
        }

        private void CreateSceneNameLabel()
        {
            Transform hudRoot = transform.Find("SafeArea/HUD_Root");
            if (hudRoot == null) hudRoot = transform;

            GameObject backgroundObject = new GameObject(
                "SceneNameLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIRoundedRectangle));
            backgroundObject.transform.SetParent(hudRoot, false);

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(1f, 1f);
            backgroundRect.anchorMax = new Vector2(1f, 1f);
            backgroundRect.pivot = new Vector2(1f, 1f);
            backgroundRect.anchoredPosition = new Vector2(0f, -92f);
            backgroundRect.sizeDelta = new Vector2(220f, 50f);
            sceneNameLabelRect = backgroundRect;

            UIRoundedRectangle background = backgroundObject.GetComponent<UIRoundedRectangle>();
            background.color = SceneLabelBackground;
            background.raycastTarget = false;

            GameObject textObject = new GameObject(
                "SceneNameText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(backgroundRect, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 7f);
            textRect.offsetMax = new Vector2(-14f, -7f);

            sceneNameText = textObject.GetComponent<TextMeshProUGUI>();
            sceneNameText.font = TMP_Settings.defaultFontAsset;
            sceneNameText.fontSize = 16f;
            sceneNameText.color = SceneLabelText;
            sceneNameText.alignment = TextAlignmentOptions.Center;
            sceneNameText.enableWordWrapping = true;
            sceneNameText.overflowMode = TextOverflowModes.Overflow;
            sceneNameText.raycastTarget = false;
        }

        private void UpdateSceneName(UnityEngine.SceneManagement.Scene previous, UnityEngine.SceneManagement.Scene current)
        {
            if (sceneNameText == null) return;

            string sceneName = current.IsValid() ? current.name : string.Empty;
            sceneNameText.text = sceneName.Replace('_', ' ');
            sceneNameText.ForceMeshUpdate();
            sceneNameLabelRect.sizeDelta = new Vector2(
                220f,
                Mathf.Max(50f, sceneNameText.preferredHeight + 20f));
        }

        [Header("Chat")]
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private ScrollRect chatMessages;
        [SerializeField] private RectTransform chatMessagesContent;
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private Button chatSendButton;
        [SerializeField] private Button chatToggleButton;
        [SerializeField] private Button chatCloseButton;
        [SerializeField] private Button chatDismissButton;
        [SerializeField] private GameObject chatNotification;

        public Graphic PlayerAvatarGraphic => playerAvatarGraphic;
        public TMP_Text PlayerNameText => playerNameText;
        public TMP_Text PlayerLevelText => playerLevelText;
        public RectTransform PlayerExperienceFill => playerExperienceFill;
        public TMP_Text PlayerRoleText => playerRoleText;
        public Button PlayerInfoButton => playerInfoButton;
        public Button MenuButton => menuButton;
        public GameObject ChatPanel => chatPanel;
        public ScrollRect ChatMessages => chatMessages;
        public RectTransform ChatMessagesContent => chatMessagesContent;
        public TMP_InputField ChatInput => chatInput;
        public Button ChatSendButton => chatSendButton;
        public Button ChatToggleButton => chatToggleButton;
        public Button ChatCloseButton => chatCloseButton;
        public Button ChatDismissButton => chatDismissButton;
        public GameObject ChatNotification => chatNotification;

        public void Configure(
            Graphic avatar,
            TMP_Text playerName,
            TMP_Text playerLevel,
            RectTransform experienceFill,
            TMP_Text playerRole,
            Button infoButton,
            Button targetMenuButton,
            GameObject targetChatPanel,
            ScrollRect messages,
            RectTransform messagesContent,
            TMP_InputField input,
            Button sendButton,
            Button toggleButton,
            Button closeButton,
            Button dismissButton,
            GameObject notification)
        {
            playerAvatarGraphic = avatar;
            playerNameText = playerName;
            playerLevelText = playerLevel;
            playerExperienceFill = experienceFill;
            playerRoleText = playerRole;
            playerInfoButton = infoButton;
            menuButton = targetMenuButton;
            chatPanel = targetChatPanel;
            chatMessages = messages;
            chatMessagesContent = messagesContent;
            chatInput = input;
            chatSendButton = sendButton;
            chatToggleButton = toggleButton;
            chatCloseButton = closeButton;
            chatDismissButton = dismissButton;
            chatNotification = notification;
        }
    }
}
