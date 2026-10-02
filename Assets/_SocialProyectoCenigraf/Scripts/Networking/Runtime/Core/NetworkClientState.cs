namespace Networking.Runtime.Core
{
    /// <summary>
    /// Representa los diferentes estados del ciclo de vida de la conexión del cliente de red en Unity.
    /// </summary>
    public enum NetworkClientState
    {
        /// <summary>
        /// El cliente se encuentra totalmente desconectado del servidor.
        /// </summary>
        Disconnected = 0,

        /// <summary>
        /// El socket se encuentra en proceso de establecimiento de conexión con el servidor.
        /// </summary>
        Connecting = 1,

        /// <summary>
        /// La conexión WebSocket se encuentra abierta y lista para enviar o recibir paquetes.
        /// </summary>
        Connected = 2,

        /// <summary>
        /// El usuario ha completado satisfactoriamente el proceso de autenticación con el servidor.
        /// </summary>
        Authenticated = 3,

        /// <summary>
        /// La conexión se ha interrumpido y el cliente está intentando reconectarse automáticamente.
        /// </summary>
        Reconnecting = 4
    }
}
