# Sistema de diálogo

Todo el juego usa un único catálogo, `Data/Dialogue/DialogueCatalog.asset`: cada conversación ocupa una sola entrada con un ID único. Si en algún momento hace falta otro `DialogueCatalog`, el código lo admite, pero la convención es mantener uno solo para que los IDs no se dispersen. Sus líneas son nodos dentro de esa entrada, no conversaciones independientes. `DialoguePresenter` conserva las conversaciones lineales y delega las conversaciones con nodos a `DialogoOfficial`, que presenta respuestas seleccionables.

## Crear una conversación con ramificaciones

En el catálogo agrega una sola entrada en **Dialogues**. Configura su `id`, `startNodeId` y la lista `nodes`. Cada nodo representa una línea, por ejemplo `Line1`, `Line2` y `Line3`, e incluye `speaker`, `text` y sus `choices`.

- `nextNodeId` del nodo es el destino automático al pulsar continuar.
- `conditionalRoutes` permite desviar ese avance: se evalúan en orden y se toma el destino de la primera condición cumplida; si ninguna se cumple, se usa `nextNodeId` o la opción `Default`.
- `conditionalStartRoutes` aplica las mismas condiciones al abrir la conversación y puede llevar a una línea inicial alternativa; si ninguna coincide, se usa `startNodeId`.
- Tipos de condición: `ItemOwned` consulta `PlayerItemInventory`, `MissionActive` y `MissionCompleted` consultan `MissionJournal`, `TalkedToCharacter` consulta el `speakerCharacterId` de diálogos completados y `DialogueCompleted` consulta el ID de una conversación completada.
- `speakerCharacterId` identifica al personaje para registrar que se completó una conversación con él. El registro dura la partida actual y se puede consultar desde otras conversaciones.
- Una opción con `key: Default` define ese destino automático y no aparece como botón.
- Las demás opciones aparecen como botones. `key` puede ser un identificador CamelCase; si `text` queda vacío se presenta como palabras separadas. `text` permite definir una etiqueta visible exacta, con acentos y espacios.
- El destino de una opción va en `nextNodeId`. Usa `*END*` para terminar. Un destino vacío también se considera final por compatibilidad con datos anteriores.
- `completionReward` entrega una recompensa al terminar. `missionCatalog` junto con `missionId` activa una misión en ese mismo momento; déjalos vacíos cuando no corresponda.
- `itemCatalog` junto con `completionItemId` agrega un ítem al inventario al terminar. `completionItemQuantity` define la cantidad.
- Un nodo terminal puede definir su propio `completionItemCatalog`, `completionItemId`, cantidad y `completionReward`; solo se entrega si se llega a ese nodo. Sirve para recompensas exclusivas de una rama.
- Un nodo terminal también puede viajar a otra escena: `travelSceneName` con el nombre de la escena tal como aparece en Build Profiles, y `travelTeleportId` con el ID del teleport de llegada, o vacío para dejar al jugador en el punto que esa escena define. El viaje usa la misma pantalla de carga de los teleports, con su fundido y su transición de música, y arranca después de cerrar el diálogo. Así se construye un teleport conversado: una línea pregunta a dónde ir y cada respuesta lleva a un nodo terminal con su escena. La entrada `Sabio_de_los_cuatro_caminos_bienvenida` lo usa en la opción «Quiero salir del templo», con destinos al Lobby, la Plaza Central y la Sala ABC.
- Un mismo nodo no debe entregar recompensa y viajar a la vez: el viaje descargaría la escena que sostiene la ventana de recompensa, así que en ese caso se omite el viaje y se escribe un aviso en consola.
- Los textos admiten `*NAME*`, `*LASTNAME*` y `*FULLNAME*`, sin distinguir mayúsculas y aceptando espacios antes del `*` de cierre. Se resuelven desde `GameSessionStore.State.PlayerName`: la primera palabra es el nombre, las restantes el apellido y el texto completo se conserva para `*FULLNAME*`.

La entrada `Sala_ABC_Bienvenida` muestra una conversación completa en un solo registro, con respuestas, retornos entre líneas, recompensa y el campo de misión opcional.

La entrada `Recepcionista_entrega_de_carnet` muestra el uso de `*NAME*`, entrega el carnet al finalizar la ruta inicial y abre directamente una despedida distinta si el jugador ya lo posee.

La entrada `Recepcionista_2_sala_ABC` muestra una ruta condicional: después de la línea de revisión, verifica `carnet_estudiantil` y presenta el mensaje de acceso concedido o denegado.

## Compatibilidad

Las conversaciones lineales antiguas siguen usando `lines`. Los NPC pueden seguir referenciando el mismo catálogo y el mismo ID; las escenas no necesitan crear una entrada por cada línea. El movimiento se pausa mientras el diálogo está abierto y se restaura al cerrar.

## Qué presentador se usa

Lo elige el jugador en el menú del juego, en **Interfaz → Animación de texto**: `Default` presenta con `DialogoOfficial` y `Retro` con `DialoguePresenter`, la cinemática anterior, que conserva los nodos y las respuestas y se navega con E/Enter y las flechas, o tocando los botones.

La elección se guarda en `TextAnimationSettings` (`PlayerPrefs`, clave `ui.textAnimation`) y aplica a **todas las escenas y a todas las conversaciones, sin excepción**: antes el formato de la entrada decidía, porque las lineales siempre iban a `DialoguePresenter`, pero los dos presentadores saben presentar nodos y `lines`, así que `DialoguePresenter.TryOpen` ahora enruta solo por el modo. Antes también la escena decidía: `Templo_De_Los_4_Caminos` forzaba `LegacyCinematic` por nombre, y eso se eliminó.

`DebugStore.DialogueMode` sigue siendo el interruptor que lee el código de diálogo, y se puede despachar desde otros sistemas con `DebugStore.Instance.Dispatch(DebugAction.SetDialogueMode(...))`, pero `DebugDialogueSceneMode` vuelve a aplicar la preferencia del jugador al arrancar, al cambiar de escena y cada vez que ésta cambia: un despacho manual solo dura hasta el siguiente cambio de escena.

Las respuestas de ambos presentadores se muestran en vertical y alineadas a la izquierda. La opción seleccionada muestra una flecha pixel y un resaltado; el cursor también cambia la selección al pasar sobre una respuesta. Los textos pueden usar etiquetas TMP `<color=#RRGGBB>...</color>` para mantener los colores de referencia de cada camino.
