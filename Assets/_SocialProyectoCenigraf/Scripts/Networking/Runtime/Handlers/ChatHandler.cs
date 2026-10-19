using System;
using Networking.Runtime.Protocol;
using Networking.Runtime.Protocol.DTOs;
using UnityEngine;

namespace Networking.Runtime.Handlers
{
    /// <summary>
    /// Manejador del sistema de chat en Unity (<see cref="OpCodes.ChatBroadcast"/> y <see cref="OpCodes.ChatHistory"/>).
    /// </summary>
    public sealed class ChatHandler : IPacketHandler
    {
        /// <summary>
        /// Código de operación numérico asignado (<see cref="OpCodes.ChatBroadcast"/>).
        /// </summary>
        public short OpCode => OpCodes.ChatBroadcast;

        /// <summary>
        /// Evento emitido cuando un nuevo mensaje de chat es recibido en tiempo real.
        /// </summary>
        public event Action<ChatBroadcastPayload> OnChatMessageReceived;

        /// <summary>
        /// Evento emitido cuando se recibe el historial inicial de mensajes al conectar.
        /// </summary>
        public event Action<ChatHistoryPayload> OnChatHistoryReceived;

        /// <summary>
        /// Procesa la difusión de un mensaje de chat individual.
        /// </summary>
        /// <param name="payloadJson">JSON con el DTO <see cref="ChatBroadcastPayload"/>.</param>
        public void Handle(string payloadJson)
        {
            var message = JsonUtility.FromJson<ChatBroadcastPayload>(payloadJson);
            if (message == null) return;

            OnChatMessageReceived?.Invoke(message);
        }

        /// <summary>
        /// Procesa la carga útil que contiene el historial de mensajes de chat.
        /// </summary>
        /// <param name="payloadJson">JSON con el DTO <see cref="ChatHistoryPayload"/>.</param>
        public void HandleChatHistory(string payloadJson)
        {
            var history = JsonUtility.FromJson<ChatHistoryPayload>(payloadJson);
            if (history == null) return;

            OnChatHistoryReceived?.Invoke(history);
        }
    }
}
