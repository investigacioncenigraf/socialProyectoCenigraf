# Entorno de Ejecución en Unity (`Runtime`)

El directorio `Runtime` contiene todo el código ejecutable de red para el cliente de Unity, estructurado en capas desacopladas con inversión de dependencias y responsabilidad única.

---

## Estructura Modular

```
Runtime/
├── Core/               # Sincronización de hilos (UnityMainThreadDispatcher) y estados
│   └── README.md
├── Connection/         # Cliente nativo ClientWebSocket y abstracción IWebSocketClient
│   └── README.md
├── Protocol/           # OpCodes, sobres de red (NetworkPacket) y DTOs serializables
│   ├── DTOs/
│   │   └── README.md
│   └── README.md
├── Handlers/           # Enrutador y manejadores de paquetes (Auth, Position, Chat)
│   └── README.md
├── Controllers/        # Componentes MonoBehaviour para la escena (NetworkManager, Sync, ChatUI)
│   └── README.md
└── README.md
```

---

## Flujo de Datos en el Cliente Unity

```mermaid
graph TD
    WS[WebSocket Server] <-->|JSON Stream| UWS[UnityWebSocketClient]
    UWS -->|Receives Background Thread| DISP[UnityMainThreadDispatcher]
    DISP -->|Main Thread Queue| PD[PacketDispatcher]
    PD -->|OpCode 101/200/201/202| AH[AuthHandler]
    PD -->|OpCode 300| PH[PositionHandler]
    PD -->|OpCode 401/402| CH[ChatHandler]
    
    AH --> NM[NetworkManager]
    PH --> PNS[PlayerNetworkSync - Remote Lerp]
    CH --> CUI[ChatUIController - Text UI]
    
    PNS -->|OpCode 300 Local 20Hz| NM
    CUI -->|OpCode 400 Send Message| NM
    NM -->|Serialized Packet| UWS
```
