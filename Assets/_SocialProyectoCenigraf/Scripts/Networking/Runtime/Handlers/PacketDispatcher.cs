using System;
using System.Collections.Generic;
using Networking.Runtime.Protocol;
using UnityEngine;

namespace Networking.Runtime.Handlers
{
    /// <summary>
    /// Despachador central de paquetes en Unity que parsea el sobre de red y deriva la carga útil
    /// a los manejadores registrados según el código de operación.
    /// </summary>
    public sealed class PacketDispatcher
    {
        private readonly Dictionary<short, List<Action<string>>> _handlers = new Dictionary<short, List<Action<string>>>();

        /// <summary>
        /// Registra un manejador que implemente <see cref="IPacketHandler"/>.
        /// </summary>
        /// <param name="handler">Instancia del manejador.</param>
        public void RegisterHandler(IPacketHandler handler)
        {
            if (handler == null) return;
            RegisterCallback(handler.OpCode, handler.Handle);
        }

        /// <summary>
        /// Registra una función callback asociada a un código de operación determinado.
        /// </summary>
        /// <param name="opCode">Código de operación.</param>
        /// <param name="callback">Acción que procesará la carga útil JSON.</param>
        public void RegisterCallback(short opCode, Action<string> callback)
        {
            if (callback == null) return;

            if (!_handlers.TryGetValue(opCode, out var list))
            {
                list = new List<Action<string>>();
                _handlers[opCode] = list;
            }

            list.Add(callback);
        }

        /// <summary>
        /// Da de baja una función callback previamente registrada.
        /// </summary>
        /// <param name="opCode">Código de operación.</param>
        /// <param name="callback">Callback a remover.</param>
        public void UnregisterCallback(short opCode, Action<string> callback)
        {
            if (_handlers.TryGetValue(opCode, out var list))
            {
                list.Remove(callback);
            }
        }

        /// <summary>
        /// Deserializa el paquete en sobre entrante y ejecuta los manejadores asociados.
        /// </summary>
        /// <param name="rawJson">Texto JSON completo del sobre de red.</param>
        public void Dispatch(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson)) return;

            NetworkPacket packet;
            try
            {
                packet = JsonUtility.FromJson<NetworkPacket>(rawJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PacketDispatcher] Fallo al parsear sobre de red: {ex.Message}");
                return;
            }

            if (packet == null) return;

            // Manejo automático de Pong
            if (packet.opCode == OpCodes.Pong)
            {
                return;
            }

            if (_handlers.TryGetValue(packet.opCode, out var callbacks))
            {
                for (int i = 0; i < callbacks.Count; i++)
                {
                    try
                    {
                        callbacks[i]?.Invoke(packet.payload);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[PacketDispatcher] Error ejecutando manejador para OpCode {packet.opCode}: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[PacketDispatcher] No se encontró ningún manejador registrado para el OpCode: {packet.opCode}");
            }
        }
    }
}
