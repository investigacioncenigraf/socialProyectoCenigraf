# Teleports entre escenas

1. Selecciona el objeto `Teleport_SalaAbc_to_PlazaCentral` y usa Add Component → Scene Teleport; o arrastra `Prefabs/Teleport_SalaAbc_to_PlazaCentral.prefab` a tu escena.
2. Configura **Teleport Id** (por ejemplo `ABC_001`), **Escena destino** (arrastra el archivo `.unity`) y **Destination Teleport Id** (`024`). Los códigos son texto y distinguen `024` de `24`.
3. Ajusta **Box Collider 2D → Size / Offset**, o Edit Collider. Debe ser **Is Trigger**. El jugador ya tiene Rigidbody2D, por lo que el teleport no necesita otro cuerpo.
4. En la escena destino agrega otro Scene Teleport y asigna **Teleport Id = 024**. Déjalo en la posición de llegada, o asigna un Transform en **Spawn Point** para separar llegada y zona de interacción. Puedes dejar su destino vacío si es solo un punto de llegada.
5. Para volver, asigna en el segundo teleport la escena Sala_ABC y el ID `ABC_001`.
6. Habilita ambas escenas en Build Profiles → Scene List. El inspector ofrece un botón para habilitar la escena destino seleccionada. Guarda las dos escenas.
7. En Play entra en la zona y pulsa E. No se activa al entrar ni al mantener la tecla pulsada. Para un aviso visual, asigna un objeto hijo en **Interaction Prompt**.

Cada escena destino debe contener su jugador local activo (PlayerMovementController y PlayerStateStore), como la demo. El sistema utiliza ese jugador; no crea uno ni conserva el anterior. Si no existe el ID, está vacío o se repite, conserva la posición inicial del jugador en la escena destino y muestra una advertencia. Si la escena no está habilitada, permanece en la escena actual. No interactúa mientras se escribe en un InputField.

Para crear otro punto: GameObject → 2D Object → Scene Teleport. Los IDs deben ser únicos dentro de cada escena. Ubica el Spawn Point en suelo transitable fuera de paredes y colliders.

## Aviso visual

El prefab `Teleport_.prefab` incluye `InteractionPrompt → Frame → Visual`. El cuadro se muestra al entrar el jugador en la zona y se oculta al salir. Funciona también antes de asignar la escena destino. `Visual` usa provisionalmente la imagen existente de la tecla E. Para cambiarla, abre el prefab, selecciona `Visual` y arrastra un Sprite a `Image → Source Image`. Conserva Color blanco para mostrar los colores originales. El tamaño se ajusta en su Rect Transform; la posición del aviso se ajusta en InteractionPrompt. El objeto permanece desactivado inicialmente; actívalo temporalmente en Prefab Mode para previsualizar y desactívalo antes de guardar.

## Acceso por rol

En cada teleport activa **Restringir por rol**, asigna el catálogo de roles y marca los **Roles excluidos**. Solo esos roles quedan bloqueados; la lista vacía o la opción desactivada permiten el acceso a todos. Se evalúa PlayerStateStore.State.RoleId del jugador local al pulsar E dentro del área, por lo que un cambio de rol tiene efecto sin salir y volver a entrar. Si falta el ID se utiliza visitor, igual que el estado del jugador.

Cuando se rechaza el viaje, la escena y la posición se conservan y se muestra **Mensaje de acceso denegado** durante tres segundos. No bloquea el movimiento. Configura todas las entradas que conduzcan al lugar que quieras restringir; estas reglas regulan la salida desde cada teleport, no las llegadas a un punto de spawn.

## Transición de carga y respawn

El viaje crea una pantalla negra persistente con «Cargando…» antes de cargar la escena. Mantiene una cobertura mínima de un segundo, resuelve el ID de llegada y retira la pantalla con un fundido de 0.35 segundos. Si la carga tarda más, espera hasta que esté lista.

Durante la preparación desactiva temporalmente las cámaras de la escena de llegada, la simulación física y el movimiento del jugador, y oculta sus renderers. Después de inicializar la escena coloca el cuerpo físico y el estado del jugador en el destino. CameraFollowController.SnapToTarget reinicia el historial del retardo y la velocidad del suavizado, respeta los límites y ajusta la cámara directamente a la posición resuelta. El seguimiento normal conserva sus opciones de retardo y suavizado después del viaje.

Los controles y la física se restauran al terminar el fundido. Si falta el ID o está duplicado se usa el spawn por defecto bajo la misma cobertura. Las escenas no disponibles se rechazan antes de mostrar la carga. No se permite navegar hacia atrás durante un viaje. Los tiempos están definidos en TeleportTravel (minimumLoadingSeconds y fadeSeconds).

### Plantilla de carga e icono

`Resources/UI/LoadingScreen.prefab` contiene el fondo negro y el indicador inferior derecho, con márgenes de 48 px a la derecha y 40 px abajo en la resolución de referencia 1920×1080. Para añadir el icono, selecciona `LoadingIndicator → IconSlot → LoadingIcon` y asigna un Sprite en Image → Source Image. Al cargar, el script activa la imagen si tiene un Sprite; el espacio está reservado por un marco discreto de 64×64.

Una LoadingCamera persistente, sin objetos visibles (Culling Mask = Nothing), sigue renderizando negro mientras las cámaras de llegada están desactivadas. Se apaga únicamente después de restaurar una cámara de la escena, antes del fundido, para evitar el aviso de Game View sin cámaras renderizando.
