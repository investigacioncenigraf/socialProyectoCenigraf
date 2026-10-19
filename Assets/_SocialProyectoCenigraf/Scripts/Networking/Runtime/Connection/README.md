# Módulo de Conexión de Red (`Runtime/Connection`)

Este módulo encapsula la conexión física y la transmisión de tramas WebSocket utilizando la clase nativa de .NET `System.Net.WebSockets.ClientWebSocket`, sin requerir plugins de la Asset Store ni bibliotecas de terceros.

---

## Componentes

### 1. `IWebSocketClient.cs`
- Contrato abstracto que desacopla la lógica del juego de la librería de red.
- Facilita la creación de clientes simulados (*Mock Clients*) para pruebas unitarias sin levantar el servidor.

### 2. `UnityWebSocketClient.cs`
- **Manejo de Hilos**: Todos los eventos (`OnConnected`, `OnMessageReceived`, `OnDisconnected`, `OnError`) se redirigen automáticamente a través de `UnityMainThreadDispatcher` para que cualquier suscriptor en Unity pueda invocar funciones gráficas o modificar `Transform` de forma inmediata.
- **Sincronización de Envíos**: Utiliza un `SemaphoreSlim(1,1)` para evitar el error común de concurrencia en WebSockets cuando múltiples scripts envían paquetes a la vez.
- **Búfer Dinámico**: Reconstruye tramas fragmentadas con `MemoryStream` garantizando recepción íntegra sin límites estrictos de tamaño.
