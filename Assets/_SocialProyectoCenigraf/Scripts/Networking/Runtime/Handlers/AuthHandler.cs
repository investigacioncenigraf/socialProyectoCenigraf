using System;
using Networking.Runtime.Protocol;
using Networking.Runtime.Protocol.DTOs;
using UnityEngine;

namespace Networking.Runtime.Handlers
{
    /// <summary>
    /// Manejador de eventos de autenticación y presencia de jugadores en Unity.
    /// Procesa respuestas del servidor, entradas y salidas de jugadores y sincronización de lista inicial.
    /// </summary>
    public sealed class AuthHandler : IPacketHandler
    {
        /// <summary>
        /// Código de operación principal asignado a este manejador (<see cref="OpCodes.AuthResponse"/>).
        /// </summary>
        public short OpCode => OpCodes.AuthResponse;

        /// <summary>
        /// Evento emitido cuando la respuesta de autenticación ha sido procesada.
        /// </summary>
        public event Action<AuthResponsePayload> OnAuthCompleted;

        /// <summary>
        /// Evento emitido cuando un nuevo jugador ingresa a la sesión de juego.
        /// </summary>
        public event Action<UserSyncPayload> OnUserJoined;

        /// <summary>
        /// Evento emitido cuando un jugador se desconecta de la sesión de juego.
        /// </summary>
        public event Action<UserSyncPayload> OnUserLeft;

        /// <summary>
        /// Evento emitido cuando se recibe el listado inicial de jugadores conectados al iniciar sesión.
        /// </summary>
        public event Action<UserListSyncPayload> OnUserListSynchronized;

        /// <summary>
        /// Procesa la carga útil del paquete de respuesta de autenticación.
        /// </summary>
        /// <param name="payloadJson">JSON con el DTO <see cref="AuthResponsePayload"/>.</param>
        public void Handle(string payloadJson)
        {
            var response = JsonUtility.FromJson<AuthResponsePayload>(payloadJson);
            if (response == null) return;

            OnAuthCompleted?.Invoke(response);
        }

        /// <summary>
        /// Procesa la notificación de un nuevo usuario que se une al mundo.
        /// </summary>
        /// <param name="payloadJson">JSON con el DTO <see cref="UserSyncPayload"/>.</param>
        public void HandleUserJoined(string payloadJson)
        {
            var user = JsonUtility.FromJson<UserSyncPayload>(payloadJson);
            if (user == null) return;

            OnUserJoined?.Invoke(user);
        }

        /// <summary>
        /// Procesa la notificación de un usuario que se desconectó del mundo.
        /// </summary>
        /// <param name="payloadJson">JSON con el DTO <see cref="UserSyncPayload"/>.</param>
        public void HandleUserLeft(string payloadJson)
        {
            var user = JsonUtility.FromJson<UserSyncPayload>(payloadJson);
            if (user == null) return;

            OnUserLeft?.Invoke(user);
        }

        /// <summary>
        /// Procesa el paquete de sincronización de la lista de jugadores conectados.
        /// </summary>
        /// <param name="payloadJson">JSON con el DTO <see cref="UserListSyncPayload"/>.</param>
        public void HandleUserListSync(string payloadJson)
        {
            // Se utiliza el envoltorio UserListSyncPayload para parseo seguro en JsonUtility
            var listPayload = JsonUtility.FromJson<UserListSyncPayload>("{\"users\":" + payloadJson + "}");
            if (listPayload == null) return;

            OnUserListSynchronized?.Invoke(listPayload);
        }
    }
}
