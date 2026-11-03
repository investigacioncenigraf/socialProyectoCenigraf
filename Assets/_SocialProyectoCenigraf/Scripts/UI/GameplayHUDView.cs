using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    public sealed class GameplayHUDView : MonoBehaviour
    {
        [Header("Player summary")]
        [SerializeField] private Graphic playerAvatarGraphic;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerLevelText;
        [SerializeField] private RectTransform playerExperienceFill;
        [SerializeField] private TMP_Text playerRoleText;
        [SerializeField] private Button playerInfoButton;

        [Header("Menu")]
        [SerializeField] private Button menuButton;

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
