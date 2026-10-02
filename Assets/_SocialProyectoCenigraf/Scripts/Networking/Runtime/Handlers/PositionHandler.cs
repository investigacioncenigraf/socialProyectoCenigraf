using System;
using Networking.Runtime.Protocol;
using Networking.Runtime.Protocol.DTOs;
using UnityEngine;

namespace Networking.Runtime.Handlers
{
    /// <summary>
    /// Manejador de paquetes de movimiento espacial en Unity (<see cref="OpCodes.PositionUpdate"/>).
    /// Deserializa coordenadas remotas de otros jugadores y las expone mediante eventos para interpolación.
    /// </summary>
    public sealed class PositionHandler : IPacketHandler
    {
        /// <summary>
        /// Código de operación numérico asignado (<see cref="OpCodes.PositionUpdate"/>).
        /// </summary>
        public short OpCode => OpCodes.PositionUpdate;

        /// <summary>
        /// Evento emitido cuando se recibe una actualización de posición de un jugador remoto.
        /// </summary>
        public event Action<PositionUpdatePayload> OnRemotePositionReceived;

        /// <summary>
        /// Procesa la carga útil del paquete de posición entrante.
        /// </summary>
        /// <param name="payloadJson">JSON con el DTO <see cref="PositionUpdatePayload"/>.</param>
        public void Handle(string payloadJson)
        {
            var update = JsonUtility.FromJson<PositionUpdatePayload>(payloadJson);
            if (update == null) return;

            OnRemotePositionReceived?.Invoke(update);
        }
    }
}
