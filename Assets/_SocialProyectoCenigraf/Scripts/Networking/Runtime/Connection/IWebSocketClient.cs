using System;
using System.Threading;
using System.Threading.Tasks;
using Networking.Runtime.Core;

namespace Networking.Runtime.Connection
{
    /// <summary>
    /// Define el contrato de abstracción para el cliente WebSocket en Unity.
    /// </summary>
    public interface IWebSocketClient : IDisposable
    {
        /// <summary>
        /// Obtiene el estado actual de la conexión del cliente de red.
        /// </summary>
        NetworkClientState State { get; }

        /// <summary>
        /// Evento emitido cuando el cliente logra conectarse con éxito al servidor.
        /// </summary>
        event Action OnConnected;

        /// <summary>
        /// Evento emitido cuando el cliente se desconecta o se pierde la conexión.
        /// </summary>
        event Action<string> OnDisconnected;

        /// <summary>
        /// Evento emitido cuando se recibe un mensaje de texto completo desde el servidor.
        /// </summary>
        event Action<string> OnMessageReceived;

        /// <summary>
        /// Evento emitido ante cualquier error de red o socket.
        /// </summary>
        event Action<string> OnError;

        /// <summary>
        /// Inicia la conexión asíncrona hacia la dirección URL del servidor WebSocket especificada.
        /// </summary>
        /// <param name="serverUrl">Dirección URL completa (ej. "ws://localhost:8080/").</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Tarea que representa el intento de conexión.</returns>
        Task ConnectAsync(string serverUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cierra la conexión de forma limpia y ordenada.
        /// </summary>
        /// <param name="reason">Motivo de la desconexión.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Tarea asíncrona de desconexión.</returns>
        Task DisconnectAsync(string reason = "Desconexión por usuario", CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía una trama de texto serializada al servidor de forma no bloqueante y thread-safe.
        /// </summary>
        /// <param name="message">Cadena de texto con el paquete JSON a enviar.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Tarea que representa el envío.</returns>
        Task SendAsync(string message, CancellationToken cancellationToken = default);
    }
}
