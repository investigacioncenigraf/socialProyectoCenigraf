# Boceto de diálogo cinematográfico

Arrastra `Prefabs/Interactions/Modular_Interaction.prefab` a una escena con jugador. Incluye el catálogo de ejemplo y la cadena `bienvenida_sala_abc`. Entra en su BoxCollider2D y pulsa E.

- E abre; mientras se escribe completa la frase; otra pulsación avanza. En la última frase cierra. Esc cancela.
- Dos franjas negras ocupan la parte superior e inferior; la inferior muestra nombre, texto progresivo e instrucciones. El texto admite tildes y saltos de línea; este primer boceto no incluye opciones ni ramificaciones.
- El movimiento del jugador que inicia se pausa y se restaura al cerrar. El icono se oculta durante la conversación.

## Crear contenido

Project → Create → Cenigraf → Dialogue Catalog. En Dialogues agrega una entrada con ID único y sus Lines (Speaker y Text). En cada Modular Interaction asigna Dialogue Catalog y selecciona Cadena de diálogo. Un catálogo puede contener varias cadenas y compartirse entre objetos. Para dejar un área sin diálogo, quita el catálogo o elige Sin diálogo.

Edita el ejemplo en `Asset/Dialogue/DialogueCatalog_Example.asset`. Para ajustar el aspecto de este boceto, revisa BuildInterface en DialoguePresenter.cs; la velocidad está definida por charactersPerSecond (35 por defecto). El Canvas se crea automáticamente y se destruye con la escena. No se requiere EventSystem para los controles de teclado.
