# Sonidos retro

Los tonos se generan en memoria: no requieren archivos de audio ni asignaciones en cada prefab. `RetroFeedbackAudio` conserva dos voces 2D entre escenas.

Ajusta `Resources/Audio/RetroAudioSettings.asset` en el Inspector:

- **Letter Volume**: 0.08 por defecto. Un bit breve al revelar letras o números del diálogo cinematográfico; espacios y signos quedan en silencio. Mostrar toda una frase con E no dispara una ráfaga de sonidos.
- **Interaction Volume**: 0.14 por defecto. Un tono distinto al abrir un diálogo desde ModularInteraction, avanzar o completar texto con E y activar un teleport válido. E fuera de una zona, destinos inválidos y accesos denegados quedan en silencio.

**Reward Volume**: 0.16 por defecto. Melodía original ascendente de 0.6 segundos al obtener una recompensa por primera vez durante la partida. Reabrir una recompensa pendiente no repite la celebración.

Cada volumen admite 0 para silenciarlo. Los tonos tienen entrada/salida suave y no acumulan varias reproducciones del mismo tipo dentro de un frame. El servicio no escucha el teclado: solo lo llaman las acciones aceptadas.

Los botones UI reciben `RetroButtonAudio` automáticamente, incluidos los menús generados en ejecución. Hover Volume (0.07) controla el tono al entrar el cursor sobre un botón habilitado. El clic y la activación con teclado usan Interaction Volume; las confirmaciones duplicadas en un mismo frame se reproducen una sola vez. No hace falta modificar cada prefab.
