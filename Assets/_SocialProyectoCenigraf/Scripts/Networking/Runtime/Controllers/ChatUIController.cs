using System.Text;
using Networking.Runtime.Protocol.DTOs;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.Runtime.Controllers
{
    /// <summary>
    /// Controlador de interfaz de usuario de chat en tiempo real para Unity.
    /// Conecta los campos de entrada, botones de envío y visualización de mensajes con formato enriquecido.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChatUIController : MonoBehaviour
    {
        [Header("Referencias de UI (uGUI)")]
        [Tooltip("Campo de texto donde el usuario redacta el mensaje.")]
        [SerializeField] private InputField _chatInputField;

        [Tooltip("Botón para emitir el mensaje redactado.")]
        [SerializeField] private Button _sendButton;

        [Tooltip("Componente Text donde se acumula y muestra el historial del chat.")]
        [SerializeField] private Text _chatDisplayArea;

        [Tooltip("ScrollRect contenedor para autodesplazar hacia abajo con cada mensaje.")]
        [SerializeField] private ScrollRect _chatScrollRect;

        [Header("Configuración")]
        [Tooltip("Canal predeterminado del chat.")]
        [SerializeField] private string _currentChannel = "Global";

        [Tooltip("Límite máximo de líneas visibles en pantalla para prevenir consumo excesivo de memoria.")]
        [SerializeField] private int _maxDisplayMessages = 100;

        private readonly StringBuilder _chatHistoryBuilder = new StringBuilder();
        private int _currentMessageCount = 0;

        private void Start()
        {
            if (_sendButton != null)
            {
                _sendButton.onClick.AddListener(OnSendButtonClicked);
            }

            if (_chatInputField != null)
            {
                // Enviar al presionar Enter/Return
                _chatInputField.onEndEdit.AddListener((text) =>
                {
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        OnSendButtonClicked();
                    }
                });
            }

            // Suscripción a eventos de chat a través de NetworkManager
            if (NetworkManager.Instance != null && NetworkManager.Instance.Chat != null)
            {
                NetworkManager.Instance.Chat.OnChatMessageReceived += HandleChatMessageReceived;
                NetworkManager.Instance.Chat.OnChatHistoryReceived += HandleChatHistoryReceived;
            }
        }

        private void OnSendButtonClicked()
        {
            if (_chatInputField == null || string.IsNullOrWhiteSpace(_chatInputField.text))
            {
                return;
            }

            string textToSend = _chatInputField.text.Trim();
            _chatInputField.text = string.Empty;
            _chatInputField.ActivateInputField();

            _ = NetworkManager.Instance.SendChatMessageAsync(textToSend, _currentChannel);
        }

        private void HandleChatMessageReceived(ChatBroadcastPayload payload)
        {
            AppendMessageToChat(payload.senderUsername, payload.content, payload.senderRole);
        }

        private void HandleChatHistoryReceived(ChatHistoryPayload historyPayload)
        {
            if (historyPayload.messages == null) return;

            for (int i = 0; i < historyPayload.messages.Count; i++)
            {
                var msg = historyPayload.messages[i];
                AppendMessageToChat(msg.senderUsername, msg.content, msg.senderRole);
            }
        }

        private void AppendMessageToChat(string username, string content, int role)
        {
            string roleColorHex = GetRoleColorHex(role);
            string formattedLine = $"<color={roleColorHex}><b>[{username}]</b></color>: {content}\n";

            _chatHistoryBuilder.Append(formattedLine);
            _currentMessageCount++;

            if (_chatDisplayArea != null)
            {
                _chatDisplayArea.text = _chatHistoryBuilder.ToString();
            }

            // Autodesplazamiento hacia el fondo del scroll
            if (_chatScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private static string GetRoleColorHex(int role)
        {
            return role switch
            {
                3 => "#FF3333", // Admin (Rojo)
                2 => "#33FF33", // Moderator (Verde)
                1 => "#33CCFF", // Player (Cian)
                _ => "#AAAAAA"  // Guest (Gris)
            };
        }

        private void OnDestroy()
        {
            if (_sendButton != null)
            {
                _sendButton.onClick.RemoveListener(OnSendButtonClicked);
            }

            if (NetworkManager.Instance != null && NetworkManager.Instance.Chat != null)
            {
                NetworkManager.Instance.Chat.OnChatMessageReceived -= HandleChatMessageReceived;
                NetworkManager.Instance.Chat.OnChatHistoryReceived -= HandleChatHistoryReceived;
            }
        }
    }
}
