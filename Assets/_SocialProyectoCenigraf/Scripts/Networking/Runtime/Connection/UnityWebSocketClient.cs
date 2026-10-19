using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Networking.Runtime.Core;

namespace Networking.Runtime.Connection
{
    /// <summary>
    /// Implementación de cliente WebSocket de alto rendimiento para Unity basada en <see cref="ClientWebSocket"/> nativo.
    /// Garantiza envíos thread-safe y despacha todos los eventos al hilo principal de Unity.
    /// </summary>
    public sealed class UnityWebSocketClient : IWebSocketClient
    {
        private ClientWebSocket _clientWebSocket;
        private CancellationTokenSource _connectionCts;
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
        private readonly int _bufferSize;
        private int _isDisposed;

        /// <summary>
        /// Obtiene el estado actual de la conexión de red.
        /// </summary>
        public NetworkClientState State { get; private set; } = NetworkClientState.Disconnected;

        /// <summary>
        /// Evento emitido en el hilo principal cuando el WebSocket se conecta exitosamente.
        /// </summary>
        public event Action OnConnected;

        /// <summary>
        /// Evento emitido en el hilo principal cuando el WebSocket se desconecta.
        /// </summary>
        public event Action<string> OnDisconnected;

        /// <summary>
        /// Evento emitido en el hilo principal cuando se recibe un mensaje de texto completo.
        /// </summary>
        public event Action<string> OnMessageReceived;

        /// <summary>
        /// Evento emitido en el hilo principal ante cualquier error de red.
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// Inicializa una nueva instancia del cliente WebSocket para Unity.
        /// </summary>
        /// <param name="bufferSize">Tamaño en bytes del búfer de lectura (por defecto 4096 bytes).</param>
        public UnityWebSocketClient(int bufferSize = 4096)
        {
            _bufferSize = bufferSize > 0 ? bufferSize : 4096;
            _clientWebSocket = new ClientWebSocket();
        }

        /// <summary>
        /// Inicia la conexión asíncrona hacia el servidor WebSocket.
        /// </summary>
        /// <param name="serverUrl">Dirección URL del servidor (ej. "ws://localhost:8080/").</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Tarea que representa la conexión.</returns>
        public async Task ConnectAsync(string serverUrl, CancellationToken cancellationToken = default)
        {
            if (State == NetworkClientState.Connected || State == NetworkClientState.Connecting)
            {
                return;
            }

            State = NetworkClientState.Connecting;
            _connectionCts = new CancellationTokenSource();

            try
            {
                if (_clientWebSocket.State == WebSocketState.Open || _clientWebSocket.State == WebSocketState.Connecting)
                {
                    _clientWebSocket.Dispose();
                    _clientWebSocket = new ClientWebSocket();
                }

                var uri = new Uri(serverUrl);
                await _clientWebSocket.ConnectAsync(uri, cancellationToken);

                State = NetworkClientState.Connected;

                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    OnConnected?.Invoke();
                });

                // Iniciar el bucle continuo de escucha en segundo plano
                _ = Task.Run(() => ReceiveLoopAsync(_connectionCts.Token));
            }
            catch (Exception ex)
            {
                State = NetworkClientState.Disconnected;

                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    OnError?.Invoke($"Fallo al conectar: {ex.Message}");
                    OnDisconnected?.Invoke(ex.Message);
                });
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[_bufferSize];

            try
            {
                while (_clientWebSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        WebSocketReceiveResult result;

                        do
                        {
                            result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await DisconnectAsync("Servidor solicitó cierre.", cancellationToken);
                                return;
                            }

                            memoryStream.Write(buffer, 0, result.Count);

                        } while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string messageText = Encoding.UTF8.GetString(memoryStream.ToArray());

                            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                            {
                                OnMessageReceived?.Invoke(messageText);
                            });
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación voluntaria
            }
            catch (Exception ex)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    OnError?.Invoke($"Error de recepción en WebSocket: {ex.Message}");
                });
            }
            finally
            {
                State = NetworkClientState.Disconnected;
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    OnDisconnected?.Invoke("Conexión finalizada.");
                });
            }
        }

        /// <summary>
        /// Envía un mensaje de texto de forma segura y no bloqueante mediante exclusión mutua.
        /// </summary>
        /// <param name="message">Cadena de texto con el paquete a emitir.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Tarea que representa el envío.</returns>
        public async Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            if (_clientWebSocket.State != WebSocketState.Open)
            {
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(bytes);

            await _sendSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_clientWebSocket.State == WebSocketState.Open)
                {
                    await _clientWebSocket.SendAsync(segment, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
                }
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        /// <summary>
        /// Cierra la conexión de forma limpia.
        /// </summary>
        /// <param name="reason">Causa de la desconexión.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Tarea que representa la desconexión.</returns>
        public async Task DisconnectAsync(string reason = "Desconexión normal", CancellationToken cancellationToken = default)
        {
            if (_clientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, cancellationToken);
                }
                catch
                {
                    // Ignorar si el extremo remoto ya cerró
                }
            }

            State = NetworkClientState.Disconnected;
            _connectionCts?.Cancel();
        }

        /// <summary>
        /// Libera los recursos no administrados del WebSocket y los semáforos de sincronización.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            {
                return;
            }

            _connectionCts?.Cancel();
            _connectionCts?.Dispose();
            _sendSemaphore?.Dispose();
            _clientWebSocket?.Dispose();
        }
    }
}
