# Controladores de Gameplay y UI en Unity (`Runtime/Controllers`)

Este módulo proporciona los componentes `MonoBehaviour` de alto nivel que se añaden directamente a la jerarquía de la escena o prefabricados (*Prefabs*) en Unity.

---

## Componentes

### 1. `NetworkManager.cs`
- Componente singleton y persistente (`DontDestroyOnLoad`).
- Actúa como la fachada principal del sistema de red para el juego.
- Expone métodos de alto nivel para gameplay:
  - `ConnectAsync()`
  - `DisconnectAsync()`
  - `AuthenticateAsync(username, role)`
  - `SendPositionUpdateAsync(position)`
  - `SendChatMessageAsync(message, channel)`

### 2. `PlayerNetworkSync.cs`
- Componente que se adjunta al prefab del jugador.
- **Modo Local (`isLocalPlayer = true`)**:
  - Lee periódicamente `transform.position`.
  - Comprueba si el desplazamiento supera el umbral (`minimumMovementThreshold`).
  - Emite paquetes de movimiento con la frecuencia fijada por `syncRateHz` (ej. 20 Hz / cada 50 ms).
- **Modo Remoto (`isLocalPlayer = false`)**:
  - Escucha actualizaciones del servidor dirigidas a su `remoteUserId`.
  - Suaviza el movimiento hacia la posición objetivo mediante interpolación lineal esférica (`Vector3.Lerp`), eliminando tirones y artefactos visuales.

### 3. `ChatUIController.cs`
- Conecta directamente con componentes estándar de Unity uGUI (`InputField`, `Button`, `Text`, `ScrollRect`).
- Formatea automáticamente los nombres de los usuarios según su rango o rol con códigos de color enriquecidos:
  - <font color="#FF3333">**[Admin]**</font>: Rojo
  - <font color="#33FF33">**[Moderador]**</font>: Verde
  - <font color="#33CCFF">**[Jugador]**</font>: Cian
- Efectúa auto-desplazamiento vertical al final de la conversación con cada mensaje nuevo.

### 4. `NetworkPlayerSpawner.cs`
- Administra el ciclo de vida de instanciación de jugadores remotos y locales.
- Se suscribe automáticamente a los eventos de `NetworkManager.Instance.Auth`:
  - `OnUserListSynchronized`: Instancia todos los jugadores remotos que ya estaban conectados en la sala.
  - `OnUserJoined`: Instancia dinámicamente nuevos jugadores remotos cuando entran.
  - `OnUserLeft`: Destruye el avatar del jugador que se desconecte.
  - `OnDisconnected`: Limpia todos los avatares remotos activos para evitar entidades residuales.
- Asigna automáticamente en tiempo de ejecución al prefab remoto:
  - `IsLocalPlayer = false`
  - `RemoteUserId = user.userId`
- Soporta instanciación automática opcional del jugador local (`spawnLocalPlayerAutomatically = true`).

