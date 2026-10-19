# Manejadores de Paquetes en Unity (`Runtime/Handlers`)

Este módulo implementa el desacoplamiento entre el cliente de transporte WebSocket y la lógica de gameplay en Unity, aplicando el principio de responsabilidad única (*SRP*).

---

## Componentes

### 1. `PacketDispatcher.cs`
- Enrutador que parsea la cabecera `NetworkPacket`.
- Mantiene suscripciones de callbacks o clases `IPacketHandler` asociadas a un `OpCode`.

### 2. `AuthHandler.cs`
- Maneja:
  - `OpCodes.AuthResponse` (`101`): Resultado del login.
  - `OpCodes.UserJoined` (`200`): Entrada de otro jugador.
  - `OpCodes.UserLeft` (`201`): Salida de otro jugador.
  - `OpCodes.UserListSync` (`202`): Lista completa de jugadores en el servidor.

### 3. `PositionHandler.cs`
- Maneja `OpCodes.PositionUpdate` (`300`): Coordenadas de otros jugadores para alimentar controladores de interpolación o avatares remotos.

### 4. `ChatHandler.cs`
- Maneja:
  - `OpCodes.ChatBroadcast` (`401`): Nuevos mensajes en tiempo real.
  - `OpCodes.ChatHistory` (`402`): Mensajes anteriores al unirse.
