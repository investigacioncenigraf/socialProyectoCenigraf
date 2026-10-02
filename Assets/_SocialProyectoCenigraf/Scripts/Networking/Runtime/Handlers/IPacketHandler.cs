namespace Networking.Runtime.Handlers
{
    /// <summary>
    /// Define el contrato de procesamiento de paquetes entrantes en el cliente Unity.
    /// </summary>
    public interface IPacketHandler
    {
        /// <summary>
        /// Obtiene el código de operación numérico que este manejador es capaz de procesar.
        /// </summary>
        short OpCode { get; }

        /// <summary>
        /// Procesa la carga útil deserializada del paquete en el hilo principal de Unity.
        /// </summary>
        /// <param name="payloadJson">Cadena con el JSON de la carga útil.</param>
        void Handle(string payloadJson);
    }
}
