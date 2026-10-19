using System;
using System.Collections.Generic;

namespace Networking.Runtime.Protocol.DTOs
{
    /// <summary>
    /// Vector tridimensional espacial para sincronización de posiciones compatible con Unity y el servidor .NET.
    /// </summary>
    [Serializable]
    public struct Vector3Dto
    {
        /// <summary>
        /// Componente horizontal X (minúscula para Unity).
        /// </summary>
        public float x;

        /// <summary>
        /// Componente vertical Y (altura, minúscula para Unity).
        /// </summary>
        public float y;

        /// <summary>
        /// Componente de profundidad Z (minúscula para Unity).
        /// </summary>
        public float z;

        /// <summary>
        /// Componente horizontal X alternativo (mayúscula desde servidor .NET).
        /// </summary>
        public float X;

        /// <summary>
        /// Componente vertical Y alternativo (mayúscula desde servidor .NET).
        /// </summary>
        public float Y;

        /// <summary>
        /// Componente de profundidad Z alternativo (mayúscula desde servidor .NET).
        /// </summary>
        public float Z;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="Vector3Dto"/>.
        /// </summary>
        /// <param name="x">Eje X.</param>
        /// <param name="y">Eje Y.</param>
        /// <param name="z">Eje Z.</param>
        public Vector3Dto(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// Convierte este DTO a un Vector3 nativo de Unity resolviendo diferencias de mayúsculas/minúsculas.
        /// </summary>
        public UnityEngine.Vector3 ToVector3()
        {
            float posX = x != 0f ? x : X;
            float posY = y != 0f ? y : Y;
            float posZ = z != 0f ? z : Z;
            return new UnityEngine.Vector3(posX, posY, posZ);
        }

        /// <summary>
        /// Vector nulo que representa (0, 0, 0).
        /// </summary>
        public static Vector3Dto Zero => new Vector3Dto(0f, 0f, 0f);
    }

    /// <summary>
    /// Carga útil para solicitud de autenticación desde Unity.
    /// </summary>
    [Serializable]
    public class AuthRequestPayload
    {
        /// <summary>
        /// Nombre de usuario para el inicio de sesión.
        /// </summary>
        public string username;

        /// <summary>
        /// Rol solicitado (1 = Player).
        /// </summary>
        public int role = 1;
    }

    /// <summary>
    /// Carga útil con la respuesta de autenticación del servidor.
    /// </summary>
    [Serializable]
    public class AuthResponsePayload
    {
        /// <summary>
        /// Indica si la autenticación fue aceptada.
        /// </summary>
        public bool success;

        /// <summary>
        /// Identificador único asignado al usuario.
        /// </summary>
        public string userId;

        /// <summary>
        /// Nombre de usuario confirmado.
        /// </summary>
        public string username;

        /// <summary>
        /// Rol asignado por el servidor.
        /// </summary>
        public int role;

        /// <summary>
        /// Posición inicial de aparición en el mundo.
        /// </summary>
        public Vector3Dto spawnPosition;

        /// <summary>
        /// Mensaje de error si la solicitud falló.
        /// </summary>
        public string errorMessage;
    }

    /// <summary>
    /// Datos de presencia de un usuario conectado.
    /// </summary>
    [Serializable]
    public class UserSyncPayload
    {
        /// <summary>
        /// Identificador único del usuario.
        /// </summary>
        public string userId;

        /// <summary>
        /// Nombre público del usuario.
        /// </summary>
        public string username;

        /// <summary>
        /// Rol en el servidor.
        /// </summary>
        public int role;

        /// <summary>
        /// Posición espacial actual.
        /// </summary>
        public Vector3Dto position;
    }

    /// <summary>
    /// Envoltorio para listas de usuarios (necesario para compatibilidad estricta con JsonUtility en Unity).
    /// </summary>
    [Serializable]
    public class UserListSyncPayload
    {
        /// <summary>
        /// Lista de usuarios conectados actualmente.
        /// </summary>
        public List<UserSyncPayload> users = new List<UserSyncPayload>();
    }

    /// <summary>
    /// Carga útil para emitir y recibir actualizaciones de movimiento 3D.
    /// </summary>
    [Serializable]
    public class PositionUpdatePayload
    {
        /// <summary>
        /// Identificador del usuario que se desplaza.
        /// </summary>
        public string userId;

        /// <summary>
        /// Coordenadas tridimensionales.
        /// </summary>
        public Vector3Dto position;

        /// <summary>
        /// Marca de tiempo de origen del cliente emisor.
        /// </summary>
        public long clientTimestamp;
    }

    /// <summary>
    /// Carga útil enviada desde Unity para publicar un mensaje en el chat.
    /// </summary>
    [Serializable]
    public class ChatSendMessagePayload
    {
        /// <summary>
        /// Canal de destino (ej. "Global").
        /// </summary>
        public string channel = "Global";

        /// <summary>
        /// Texto del mensaje.
        /// </summary>
        public string content;
    }

    /// <summary>
    /// Carga útil recibida con un mensaje de chat emitido por el servidor.
    /// </summary>
    [Serializable]
    public class ChatBroadcastPayload
    {
        /// <summary>
        /// Identificador único del mensaje.
        /// </summary>
        public string messageId;

        /// <summary>
        /// Identificador del usuario emisor.
        /// </summary>
        public string senderId;

        /// <summary>
        /// Nombre de usuario del emisor.
        /// </summary>
        public string senderUsername;

        /// <summary>
        /// Rol del usuario emisor.
        /// </summary>
        public int senderRole;

        /// <summary>
        /// Canal o sala a la que pertenece el mensaje.
        /// </summary>
        public string channel;

        /// <summary>
        /// Texto del mensaje.
        /// </summary>
        public string content;

        /// <summary>
        /// Marca de tiempo Unix en milisegundos.
        /// </summary>
        public long timestamp;
    }

    /// <summary>
    /// Carga útil con la colección de mensajes previos entregada al ingresar.
    /// </summary>
    [Serializable]
    public class ChatHistoryPayload
    {
        /// <summary>
        /// Canal del historial.
        /// </summary>
        public string channel = "Global";

        /// <summary>
        /// Colección de mensajes históricos.
        /// </summary>
        public List<ChatBroadcastPayload> messages = new List<ChatBroadcastPayload>();
    }

    /// <summary>
    /// Carga útil con detalles de un error notificado por el servidor.
    /// </summary>
    [Serializable]
    public class ErrorPayload
    {
        /// <summary>
        /// Código identificador del error.
        /// </summary>
        public string errorCode;

        /// <summary>
        /// Mensaje explicativo del error.
        /// </summary>
        public string message;
    }
}
