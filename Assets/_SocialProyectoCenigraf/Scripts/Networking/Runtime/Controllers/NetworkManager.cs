using System;
using System.Threading.Tasks;
using Networking.Runtime.Connection;
using Networking.Runtime.Core;
using Networking.Runtime.Handlers;
using Networking.Runtime.Protocol;
using Networking.Runtime.Protocol.DTOs;
using UnityEngine;

namespace Networking.Runtime.Controllers
{
    /// <summary>
    /// Administrador central de red para Unity. Coordina el ciclo de vida del cliente WebSocket,
    /// el enrutamiento de paquetes y expone la API de alto nivel para gameplay y UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkManager : MonoBehaviour
    {
        private static NetworkManager _instance;

        [Header("Configuración del Servidor")]
        [Tooltip("Dirección WebSocket del servidor .NET (ej. ws://localhost:8080/)")]
        [SerializeField] private string _serverUrl = "ws://localhost:8080/";

        [Tooltip("Conectar automáticamente al iniciar la escena.")]
        [SerializeField] private bool _autoConnectOnStart = false;

        [Header("Autenticación Automática")]
        [Tooltip("Autenticarse automáticamente tras conectar.")]
        [SerializeField] private bool _autoAuthenticate = true;

        [Tooltip("Nombre de usuario para inicio de sesión automático. Si se deja 'Player', se le añadirá un sufijo aleatorio único.")]
        [SerializeField] private string _defaultUsername = "Player";

        private IWebSocketClient _client;
        private PacketDispatcher _dispatcher;

        /// <summary>
        /// Instancia Singleton de <see cref="NetworkManager"/>.
        /// </summary>
        public static NetworkManager Instance => _instance;

        /// <summary>
        /// Indica o establece si se debe autenticar automáticamente tras conectar.
        /// </summary>
        public bool AutoAuthenticate
        {
            get => _autoAuthenticate;
            set => _autoAuthenticate = value;
        }

        /// <summary>
        /// Nombre de usuario predeterminado.
        /// </summary>
        public string DefaultUsername
        {
            get => _defaultUsername;
            set => _defaultUsername = value;
        }

        /// <summary>
        /// Manejador de eventos de autenticación y presencia.
        /// </summary>
        public AuthHandler Auth { get; private set; }

        /// <summary>
        /// Manejador de sincronización de movimiento y posición.
        /// </summary>
        public PositionHandler Position { get; private set; }

        /// <summary>
        /// Manejador de eventos de chat.
        /// </summary>
        public ChatHandler Chat { get; private set; }

        /// <summary>
        /// Estado actual de la conexión de red.
        /// </summary>
        public NetworkClientState State => _client?.State ?? NetworkClientState.Disconnected;

        /// <summary>
        /// Identificador local del usuario asignado tras autenticación exitosa.
        /// </summary>
        public string LocalUserId { get; private set; }

        /// <summary>
        /// Nombre de usuario del jugador local.
        /// </summary>
        public string LocalUsername { get; private set; }

        /// <summary>
        /// Evento emitido cuando la conexión física se establece con éxito.
        /// </summary>
        public event Action OnConnected;

        /// <summary>
        /// Evento emitido ante una desconexión del servidor.
        /// </summary>
        public event Action<string> OnDisconnected;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            UnityMainThreadDispatcher.Initialize();
            InitializeNetworkComponents();
        }

        private async void Start()
        {
            if (_autoConnectOnStart)
            {
                await ConnectAsync();

                if (_autoAuthenticate)
                {
                    string username = string.IsNullOrWhiteSpace(_defaultUsername) ? "Player" : _defaultUsername;
                    if (username == "Player")
                    {
                        username += $"_{UnityEngine.Random.Range(100, 999)}";
                    }

                    await AuthenticateAsync(username, 1);
                }
            }
        }

        private void InitializeNetworkComponents()
        {
            _client = new UnityWebSocketClient();
            _dispatcher = new PacketDispatcher();

            Auth = new AuthHandler();
            Position = new PositionHandler();
            Chat = new ChatHandler();

            // Registro de manejadores de paquetes
            _dispatcher.RegisterHandler(Auth);
            _dispatcher.RegisterCallback(OpCodes.UserJoined, Auth.HandleUserJoined);
            _dispatcher.RegisterCallback(OpCodes.UserLeft, Auth.HandleUserLeft);
            _dispatcher.RegisterCallback(OpCodes.UserListSync, Auth.HandleUserListSync);

            _dispatcher.RegisterHandler(Position);

            _dispatcher.RegisterHandler(Chat);
            _dispatcher.RegisterCallback(OpCodes.ChatHistory, Chat.HandleChatHistory);

            // Suscripción a eventos del cliente WebSocket
            _client.OnConnected += HandleClientConnected;
            _client.OnDisconnected += HandleClientDisconnected;
            _client.OnMessageReceived += HandleClientMessageReceived;
            _client.OnError += (err) => Debug.LogError($"[NetworkManager] Error de red: {err}");

            // Guardar datos de sesión al autenticar
            Auth.OnAuthCompleted += (res) =>
            {
                if (res.success)
                {
                    LocalUserId = res.userId;
                    LocalUsername = res.username;
                    Debug.Log($"[NetworkManager] Autenticado exitosamente como: {LocalUsername} ({LocalUserId})");
                }
                else
                {
                    Debug.LogWarning($"[NetworkManager] Autenticación rechazada: {res.errorMessage}");
                }
            };
        }

        private void HandleClientConnected()
        {
            Debug.Log("[NetworkManager] Conectado exitosamente al servidor WebSocket.");
            OnConnected?.Invoke();
        }

        private void HandleClientDisconnected(string reason)
        {
            Debug.Log($"[NetworkManager] Desconectado del servidor: {reason}");
            LocalUserId = null;
            LocalUsername = null;
            OnDisconnected?.Invoke(reason);
        }

        private void HandleClientMessageReceived(string rawJson)
        {
            _dispatcher.Dispatch(rawJson);
        }

        /// <summary>
        /// Inicia la conexión con el servidor WebSocket configurado.
        /// </summary>
        /// <returns>Tarea que representa la conexión.</returns>
        public async Task ConnectAsync()
        {
            await _client.ConnectAsync(_serverUrl);
        }

        /// <summary>
        /// Cierra la conexión de forma limpia.
        /// </summary>
        /// <param name="reason">Motivo del cierre.</param>
        /// <returns>Tarea que representa la desconexión.</returns>
        public async Task DisconnectAsync(string reason = "Desconexión por usuario")
        {
            await _client.DisconnectAsync(reason);
        }

        /// <summary>
        /// Envía una solicitud de autenticación con el nombre de usuario y rol especificados.
        /// </summary>
        /// <param name="username">Nombre de usuario deseado.</param>
        /// <param name="role">Rol solicitado (1 = Player).</param>
        /// <returns>Tarea de envío.</returns>
        public async Task AuthenticateAsync(string username, int role = 1)
        {
            var payload = new AuthRequestPayload
            {
                username = username,
                role = role
            };

            var packet = new NetworkPacket(OpCodes.AuthRequest, JsonUtility.ToJson(payload));
            await _client.SendAsync(JsonUtility.ToJson(packet));
        }

        /// <summary>
        /// Envía la actualización de posición espacial del jugador local al servidor.
        /// </summary>
        /// <param name="worldPosition">Posición tridimensional en el mundo.</param>
        /// <returns>Tarea de envío.</returns>
        public async Task SendPositionUpdateAsync(Vector3 worldPosition)
        {
            if (string.IsNullOrEmpty(LocalUserId)) return;

            var payload = new PositionUpdatePayload
            {
                userId = LocalUserId,
                position = new Vector3Dto(worldPosition.x, worldPosition.y, worldPosition.z),
                clientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var packet = new NetworkPacket(OpCodes.PositionUpdate, JsonUtility.ToJson(payload));
            await _client.SendAsync(JsonUtility.ToJson(packet));
        }

        /// <summary>
        /// Envía un mensaje de chat hacia el canal seleccionado.
        /// </summary>
        /// <param name="message">Texto a emitir.</param>
        /// <param name="channel">Canal destinatario (por defecto "Global").</param>
        /// <returns>Tarea de envío.</returns>
        public async Task SendChatMessageAsync(string message, string channel = "Global")
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var payload = new ChatSendMessagePayload
            {
                channel = channel,
                content = message
            };

            var packet = new NetworkPacket(OpCodes.ChatSendMessage, JsonUtility.ToJson(payload));
            await _client.SendAsync(JsonUtility.ToJson(packet));
        }

        private void OnDestroy()
        {
            _client?.Dispose();
        }
    }
}
