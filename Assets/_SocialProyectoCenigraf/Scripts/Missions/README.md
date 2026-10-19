# Misiones opcionales en Modular Interaction

En el inspector activa **Activar misión al entrar**, asigna un **Catálogo de misiones** y selecciona la **Misión**. El prefab incluye MissionCatalog_Example / explorar_sala_abc, pero la casilla viene desactivada. El jugador la recibe al entrar al trigger, sin pulsar E.

El diálogo es independiente: puedes tener diálogo informativo, misión sin diálogo o ambos. Al desmarcar la casilla no se activan nuevas misiones desde esa zona; las ya recibidas no se eliminan.

Crea contenido en Project → Create → Cenigraf → Mission Catalog. Cada entrada contiene ID, Title, Description y Objective. Los IDs deben ser únicos en todos los catálogos de la partida.

MissionJournal.ActiveMissions expone el registro; IsActive(id) consulta si está recibida y MissionActivated notifica nuevas activaciones. Se conserva entre escenas y se reinicia en una partida nueva. La consola muestra la activación. Este primer registro no incluye HUD, guardado al cerrar el juego, progreso automático, finalización ni recompensas. El texto Objective es descriptivo.
