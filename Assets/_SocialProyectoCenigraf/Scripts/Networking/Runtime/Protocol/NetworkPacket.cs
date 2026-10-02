using System;

namespace Networking.Runtime.Protocol
{
    /// <summary>
    /// Envoltorio sobre (Envelope) estándar para todos los mensajes de red recibidos y enviados en Unity.
    /// Compatible nativamente con <see cref="UnityEngine.JsonUtility"/> y serializadores JSON estándar.
    /// </summary>
    [Serializable]
    public class NetworkPacket
    {
        /// <summary>
        /// Código de operación que identifica la acción o tipo de carga.
        /// </summary>
        public short opCode;

        /// <summary>
        /// Carga útil del mensaje serializada en formato JSON.
        /// </summary>
        public string payload;

        /// <summary>
        /// Marca temporal Unix en milisegundos.
        /// </summary>
        public long timestamp;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="NetworkPacket"/>.
        /// </summary>
        public NetworkPacket()
        {
        }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="NetworkPacket"/> con el código y payload especificados.
        /// </summary>
        /// <param name="opCode">Código de operación.</param>
        /// <param name="payload">Cadena JSON con la carga útil.</param>
        public NetworkPacket(short opCode, string payload)
        {
            this.opCode = opCode;
            this.payload = payload;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
