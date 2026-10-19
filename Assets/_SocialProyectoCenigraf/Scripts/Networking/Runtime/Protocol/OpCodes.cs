namespace Networking.Runtime.Protocol
{
    /// <summary>
    /// Catálogo de códigos de operación numéricos sincronizados con el servidor .NET.
    /// </summary>
    public static class OpCodes
    {
        /// <summary>
        /// Comprobación de latido (Ping).
        /// </summary>
        public const short Ping = 1;

        /// <summary>
        /// Respuesta a la comprobación de latido (Pong).
        /// </summary>
        public const short Pong = 2;

        /// <summary>
        /// Solicitud de inicio de sesión o registro emitida por el cliente Unity.
        /// </summary>
        public const short AuthRequest = 100;

        /// <summary>
        /// Respuesta del servidor confirmando o denegando la autenticación.
        /// </summary>
        public const short AuthResponse = 101;

        /// <summary>
        /// Notificación recibida cuando un nuevo jugador ingresa a la partida.
        /// </summary>
        public const short UserJoined = 200;

        /// <summary>
        /// Notificación recibida cuando un jugador se desconecta de la partida.
        /// </summary>
        public const short UserLeft = 201;

        /// <summary>
        /// Sincronización del listado de jugadores ya conectados al ingresar.
        /// </summary>
        public const short UserListSync = 202;

        /// <summary>
        /// Sincronización de posición 3D en el mundo virtual.
        /// </summary>
        public const short PositionUpdate = 300;

        /// <summary>
        /// Envío de un mensaje de chat desde el cliente.
        /// </summary>
        public const short ChatSendMessage = 400;

        /// <summary>
        /// Difusión de mensaje de chat recibida desde el servidor.
        /// </summary>
        public const short ChatBroadcast = 401;

        /// <summary>
        /// Historial de mensajes recientes recibido al ingresar.
        /// </summary>
        public const short ChatHistory = 402;

        /// <summary>
        /// Notificación de error emitida por el servidor.
        /// </summary>
        public const short ErrorNotification = 500;

        /// <summary>
        /// Anuncio global del sistema emitido por el servidor.
        /// </summary>
        public const short SystemAnnouncement = 501;
    }
}
