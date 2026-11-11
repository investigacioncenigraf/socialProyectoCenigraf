# Sistema de personajes no jugables (NPC)

**Estado:** implementado; la configuración concreta se revisa en cada prefab y escena.  
**Fecha de revisión:** 6 de noviembre de 2026.

Esta guía describe cómo se construyen, ven e interactúan los NPC de NIWI-MU. No presupone que todos los NPC tengan diálogo o sigan al jugador: esos comportamientos se eligen por personaje.

## ¿Qué es?

Un NPC es un personaje del mundo compuesto por un GameObject y componentes especializados. El prefab proporciona una base reutilizable; la escena decide dónde aparece y qué configuración concreta usa.

El sistema se divide en estas partes:

- **Aspecto y animación:** `NPCLayeredSkinController` toma sprites de un `PlayerSkinCatalog` y los presenta como capas: sombra, piernas, cuerpo, accesorio, manos y cabeza. Usa un perfil de orden para dibujarlas en la secuencia correcta.
- **Colores:** `NPCLayerColorController` permite teñir el personaje completo o ajustar capas por separado. También puede reemplazar los píxeles fuente `#3C3C3C` por un color de contorno configurable.
- **Zonas:** `NPCInteractionZone` detecta al jugador para habilitar una interacción. `NPCWarningZone` detecta que el jugador se acercó y puede activar el seguimiento visual.
- **Indicador:** `NPCInteractionIndicatorController` enciende o apaga el icono de interacción, por ejemplo la tecla E.
- **Comportamiento opcional:** `NPCPlayerTrackingController` orienta el personaje hacia el jugador mientras este está en la zona de advertencia. No lo mueve ni implementa navegación.
- **Interacción:** un componente de interacción enlaza la zona con el sistema que debe abrirse: diálogo clásico, `DialogoOfficial` o la capa vacía de pantalla completa usada por el rival.

Componentes principales: `NPCLayeredSkinController`, `NPCLayerColorController`, `NPCPlayerTrackingController`, `NPCInteractionZone`, `NPCWarningZone`, `NPCInteractionIndicatorController`, `NPCDialogueInteractionController`, `NPCDialogoOfficialInteraction` y `NPCFullscreenOverlayInteraction`.

## ¿Para qué se usa?

El sistema sirve para:

- Colocar personajes con apariencia animada y reutilizar prefabs entre escenas.
- Definir el alcance visible de interacción y de acercamiento, con gizmos de color en la vista **Scene** para editar sus colliders.
- Mostrar un indicador cuando el jugador está en alcance.
- Hacer que un personaje gire para mirar al jugador al acercarse.
- Conectar el personaje con una conversación de un `DialogueCatalog` o con otra experiencia interactiva.
- Ajustar apariencia, animación, orientación y colores por personaje sin modificar las texturas fuente.

Un NPC no obtiene movimiento de navegación, combate, decisiones autónomas ni una misión por el solo hecho de usar este prefab. Esas conductas requieren sistemas y componentes adicionales.

## ¿Cómo se usa?

### 1. Elegir una base

En `Assets/_SocialProyectoCenigraf/Prefabs/Entities/NPCs/` están las bases actuales:

- `NPCs.prefab`: personaje con skin por capas, zona de advertencia, seguimiento, indicador y diálogo configurado.
- `NPC_Rival.prefab`: personaje con skin por capas y una interacción que abre una capa negra vacía a pantalla completa con 60 % de opacidad.

Duplica el prefab que más se parezca al comportamiento que necesitas y renómbralo. Mantén las referencias entre los componentes del prefab al modificarlo. Al colocar una instancia en una escena, cambia posición y los campos propios de ese personaje desde el Inspector.

Configura un único componente principal de interacción por NPC. Selecciona el tipo de interacción que necesita ese personaje; no dejes activos simultáneamente los controladores clásico, oficial y overlay, porque todos escuchan la entrada de interacción.

### 2. Configurar la apariencia y animación

En `NPCLayeredSkinController` revisa:

| Campo | Uso |
|---|---|
| `Skin Catalog` | Catálogo de skins disponible para personajes. |
| `Layer Order Profile` | Orden visual de las capas del cuerpo. |
| `Skin Id` | ID existente en el catálogo. Si no existe, se registra un error y no se resuelve el aspecto. |
| `Animation Type` | Familia de animación, por ejemplo idle o caminar hacia delante/atrás. |
| `Facing Direction` | Orientación inicial del personaje. |
| `Play Animation` | Activa o detiene el avance de fotogramas. |
| `Frame Duration Milliseconds` | Duración de cada fotograma. |
| `Frames Per Animation` | Cantidad de fotogramas que recorre la animación, de 1 a 4. |
| `Start At Frame One` | Empieza por el primer fotograma al reiniciar la animación. |

El controlador necesita referencias a los renderers de las capas. Si cambias la estructura visual, conserva o reasigna los renderers de sombra, piernas izquierda y derecha, cuerpo, accesorio, manos y cabeza.

Para cambiar el skin desde código, `SetSkin(id)` resuelve otro ID del catálogo. `SetAnimation(...)` cambia la animación; `SetFacingDirection(...)` cambia la orientación. `SetMovement(...)` orienta y selecciona una animación de caminar según el vector recibido, pero el NPC no se desplaza por sí mismo.

### 3. Cambiar los colores

En `NPCLayerColorController`:

- `Character Color` asigna el mismo color a las capas del personaje cuando se cambia en el Inspector o mediante `SetCharacterColor`.
- Los campos de **Individual Layer Colors** permiten afinar sombra, piernas, cuerpo, accesorios, manos, cabeza y cara.
- `Outline Color` es el color de reemplazo para los píxeles fuente indicados en `Outline Source Color`.
- El valor fuente inicial es `#3C3C3C`; `Outline Color Tolerance` controla cuánto pueden variar los píxeles fuente para coincidir.
- `SetOutlineColor(color)` permite cambiar el color de reemplazo en ejecución.

El reemplazo se aplica por shader a cada capa, incluida la cabeza. El color no se pinta permanentemente en los archivos de textura.

### 4. Ajustar la zona de interacción y el indicador

En el hijo `InteractionZone`:

1. Ajusta el `BoxCollider2D` para cubrir el alcance donde se puede hablar o interactuar.
2. `NPCInteractionZone` fuerza el collider a `Is Trigger` y detecta al jugador local por su `PlayerMovementController`.
3. Ajusta el color `Area Color` para identificar la zona en la vista **Scene**. El gizmo muestra un relleno y un borde.
4. Configura `NPCInteractionIndicatorController` con el GameObject y `SpriteRenderer` del icono de interacción.

El indicador se muestra cuando el jugador entra en la zona y se oculta al salir, iniciar una interacción o desactivar el componente. La disponibilidad también depende de que no haya una interfaz modal o un bloqueo de entrada.

La zona puede invocar `On Player Entered` y `On Player Exited`. Para conectar eventos, usa las listas de eventos serializadas del componente en el Inspector.

### 5. Configurar la zona de advertencia y seguimiento

Si el NPC debe girar cuando el jugador se aproxima:

1. Ajusta el `BoxCollider2D` del hijo `WarningZone` para que sea más amplio que la zona de interacción si quieres anticipar el giro.
2. `NPCWarningZone` fuerza `Is Trigger` y busca objetos con el tag `Player` (o su Rigidbody2D con ese tag).
3. Asigna el `NPCPlayerTrackingController` del personaje.
4. Activa `Tracking Enabled` y deja `Requires Warning Zone` habilitado si el seguimiento solo debe ocurrir dentro de esa área.
5. Ajusta `Horizontal Dead Zone` para evitar cambios de orientación cuando el jugador está prácticamente a la misma altura horizontal.
6. Cambia `Area Color` para identificar la zona de advertencia en la vista **Scene**.

El seguimiento cambia la orientación hacia la izquierda o derecha y conserva si el sprite está mostrando el frente o la espalda. Al salir el jugador, se elimina el objetivo actual. La zona de advertencia depende del tag `Player`; la zona de interacción depende del componente de movimiento local.

### 6. Conectar el tipo de interacción

#### Diálogo oficial

Usa `NPCDialogoOfficialInteraction` para abrir una entrada del catálogo:

1. Asigna el `Dialogue Catalog`.
2. Escribe en `Dialogue Id` el ID exacto de la conversación.
3. Asigna la zona de interacción y el controlador del indicador. Si se dejan sin asignar, el componente intenta encontrarlos en el NPC o sus hijos.
4. Asigna retratos opcionales. Si el NPC tiene `NPCLayeredSkinController`, se utiliza su sprite y color de cabeza actuales como retrato.

La entrada se abre con **E** al estar dentro de la zona; en móvil se registra como destino de interacción. La ventana emergente se construye desde `DialogoOfficial` y el movimiento del jugador se bloquea mientras corresponda.

#### Diálogo clásico

`NPCDialogueInteractionController` conecta el NPC con un catálogo e ID usando `DialoguePresenter`. Se conserva para las escenas o personajes que aún dependen de la presentación clásica. Configura la referencia al catálogo, ID, zona e indicador.

Para entender nodos, opciones y condiciones consulta [Sistema de diálogos](SISTEMA_DE_DIALOGOS.md).

#### Interacción de pantalla completa

`NPCFullscreenOverlayInteraction` se usa actualmente en `NPC_Rival.prefab`. Configura la zona, el indicador y el color del fondo. Al interactuar abre una capa vacía negra al 60 %; se cierra con **Esc**, **Enter**, **E**, el botón Este del mando o un toque. Este overlay es una pantalla de interacción, no una conversación.

### 7. Verificar el NPC en Play Mode

1. Coloca la instancia en una escena con cámara y jugador.
2. Comprueba que el skin ID exista y que las capas tengan renderers asignados.
3. En la vista **Scene**, selecciona o enfoca el NPC y revisa que las zonas de interacción y advertencia sean visibles y tengan el tamaño esperado.
4. Entra en Play Mode y acércate: el NPC debe girar dentro de la advertencia si el seguimiento está activo.
5. Entra en la zona de interacción y confirma que aparezca el icono.
6. Presiona **E** o usa el control móvil. Confirma que se abre solamente el sistema asignado a ese NPC y que movimiento e interacción se recuperan al cerrarlo.

## Solución de problemas

| Síntoma | Qué revisar |
|---|---|
| El aspecto no aparece | Que `Skin Catalog`, `Skin Id`, perfil de orden y renderers estén asignados; revisa errores de skin en la Console. |
| La zona se ve en Scene pero no detecta al jugador | Que el collider esté habilitado y marcado como trigger; que la instancia tenga su Rigidbody2D y que el jugador esté activo y acepte entrada local. |
| No aparece el indicador | Que el `SpriteRenderer` y el GameObject del indicador estén asignados y que no haya un diálogo, recompensa o bloqueo activo. |
| El NPC no gira | Que `Tracking Enabled` esté activo, `WarningZone` esté vinculado y el jugador tenga el tag `Player`. Revisa también `Requires Warning Zone`. |
| El diálogo no abre | Que solo haya un controlador principal de interacción activo, que catálogo e ID sean correctos y que la zona contenga al jugador. Revisa la Console. |
| El color de contorno no cambia | Comprueba el color fuente `#3C3C3C`, la tolerancia, el shader y que los renderers de todas las capas estén enlazados al controlador de color. |

## Prefabs y código de referencia

- Prefabs: `Assets/_SocialProyectoCenigraf/Prefabs/Entities/NPCs/NPCs.prefab` y `NPC_Rival.prefab`.
- Skin y orientación por capas: `Scripts/World/NPC/NPCLayeredSkinController.cs`.
- Colores: `Scripts/World/NPC/NPCLayerColorController.cs`.
- Zona de interacción e indicador: `NPCInteractionZone.cs` y `NPCInteractionIndicatorController.cs`.
- Advertencia y seguimiento: `NPCWarningZone.cs` y `NPCPlayerTrackingController.cs`.
- Interacciones: `NPCDialogueInteractionController.cs`, `NPCDialogoOfficialInteraction.cs` y `NPCFullscreenOverlayInteraction.cs`.
- Configuración de ejemplo en el Editor: `Scripts/Editor/Player/NPCSkinSetupEditor.cs`.
