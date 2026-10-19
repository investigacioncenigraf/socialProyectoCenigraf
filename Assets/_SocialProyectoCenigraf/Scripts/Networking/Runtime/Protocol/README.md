# Protocolo de Red en Unity (`Runtime/Protocol`)

Este módulo define las especificaciones del protocolo en el cliente de juego Unity, garantizando paridad estricta con el servidor .NET.

---

## Componentes

### 1. `OpCodes.cs`
Catálogo numérico de códigos de operación compartidos con el backend. Cualquier paquete emitido o recibido debe usar uno de estos códigos constantes.

### 2. `NetworkPacket.cs`
Envoltorio estándar (*Envelope*) que viaja por el canal de WebSocket.
- `opCode`: Indica la operación.
- `payload`: Cadena de texto JSON con el DTO correspondiente.
- `timestamp`: Marca de tiempo en milisegundos Unix para latencia e interpolación.

### 3. `DTOs/`
Modelos de datos serializables para intercambio de información.
