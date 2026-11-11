# Sonidos retro

Los tonos se generan en memoria: no requieren archivos de audio ni asignaciones en cada prefab. `RetroFeedbackAudio` conserva voces 2D separadas entre escenas para no cortar una señal con otra.

Ajusta `Resources/Audio/RetroAudioSettings.asset` en el Inspector:

- **Letter Volume**: 0.08 por defecto. Un bit breve al revelar letras o números del diálogo cinematográfico; espacios y signos quedan en silencio. Mostrar toda una frase con E no dispara una ráfaga de sonidos.
- **Interaction Volume**: 0.14 por defecto. Un tono distinto al abrir un diálogo desde ModularInteraction, avanzar o completar texto con E y activar un teleport válido. E fuera de una zona, destinos inválidos y accesos denegados quedan en silencio.

**Reward Volume**: 0.16 por defecto. Melodía original ascendente de 0.6 segundos al obtener una recompensa por primera vez durante la partida. Reabrir una recompensa pendiente no repite la celebración.

**Mission Volume**: 0.16 por defecto. Aviso ascendente breve cuando se desbloquea el Camino de Inducción del Templo de los Cuatro Caminos; suena al comenzar el fundido de la capa.

Cada volumen admite 0 para silenciarlo. Los tonos tienen entrada/salida suave y no acumulan varias reproducciones del mismo tipo dentro de un frame. El servicio no escucha el teclado: solo lo llaman las acciones aceptadas.

Los botones UI reciben `RetroButtonAudio` automáticamente, incluidos los menús generados en ejecución. Hover Volume (0.07) controla el tono al entrar el cursor sobre un botón habilitado. El clic y la activación con teclado usan Interaction Volume; las confirmaciones duplicadas en un mismo frame se reproducen una sola vez. No hace falta modificar cada prefab.

## Música de login

`LoginMusicPlayer` se crea solo en el primer frame de la primera escena, sin objetos en la jerarquía ni referencias que asignar. Arranca únicamente si la escena cargada tiene un `LoginController` y todavía no hay sesión, así que entrar a jugar desde cualquier otra escena del Editor no dispara el tema. El objeto vive en `DontDestroyOnLoad` para que el fundido termine aunque el Lobby ya esté cargado, y el tema vuelve si se regresa a la pantalla de login.

Se apaga cuando `GameSessionStore` despacha `StartSession`, es decir en el mismo instante en que `LoginController` acepta el usuario, antes de cambiar de escena. Si alguna otra ruta saca al jugador del login sin iniciar sesión, el cambio de escena también lanza el fundido.

Ajustes en `Resources/Audio/RetroAudioSettings.asset`:

- **Login Music Enabled**: activa o desactiva todo el sistema.
- **Login Music Clip**: `Inicio_Cinegraff 01`. Importado con *Preload Audio Data* para que suene en el primer frame y no un par de frames después.
- **Login Music Volume**: 0.35 por defecto. Se relee cada frame, así que se puede ajustar con el juego corriendo y se escucha el cambio al instante. El resto de los valores se leen al arrancar.
- **Login Music Fade In Seconds**: 1.25. El tema ya trae su propia entrada de 2 segundos; el fundido solo evita el chasquido inicial.
- **Login Music Fade Out Seconds**: 1.35. Es el fundido que se escucha al iniciar sesión, medido para terminar justo cuando la pantalla de carga entrega el Lobby y el tema de mundo toma su lugar.
- **Login Music Loop Start / End Seconds**: 0 y 59.6. El tema dura 61.2 segundos pero su música termina en 59.6: los últimos 1.6 son silencio. Al recortar el final, cada repetición pasa de la salida suave a la entrada suave sin un hueco muerto. Con Loop End en 0 (o mayor que el clip) se usa el loop nativo de Unity sobre el clip completo.

## Música de las escenas

`WorldMusicPlayer` es la voz de música del resto del juego: una sola, persistente entre escenas, creada sola en el primer frame y sin nada que asignar en la jerarquía. Cada cambio de escena hace la transición completa aunque el tema sea el mismo: fundido de salida → silencio corto → fundido de entrada. El fundido de salida arranca en el momento en que empieza el cambio, no cuando ya cargó la escena nueva, así que en los teleports se escucha completo detrás de la pantalla de carga.

La pantalla de login queda fuera: ahí manda `LoginMusicPlayer`. Las dos usan la misma prueba (`LoginMusicPlayer.IsLoginScene`), así que nunca suenan las dos a la vez por error.

Ajustes en `Resources/Audio/RetroAudioSettings.asset`:

- **World Music Enabled**: activa o desactiva todo el sistema.
- **World Music Clip**: `Inicio_Senigraf 02`, el tema por defecto de todas las escenas.
- **World Music Volume**: 0.3. También se relee cada frame, se puede ajustar con el juego corriendo.
- **World Music Fade In / Fade Out Seconds**: 1.25 y 0.6.
- **World Music Enter Delay Seconds**: 2. El silencio entre los dos fundidos. Se cuenta desde que el tema anterior queda realmente en silencio, no desde que arranca el cambio de escena, e incluye esperar a que el tema de login termine su propio fundido: así los dos temas nunca se enciman.
- **World Music Loop Start / End Seconds**: 0.42 y 59.8. El clip dura 62.2 segundos pero abre con 0.42 de silencio y cierra con 2.5: recortando ambos extremos el loop queda limpio.
- **World Music Scenes**: lista para darle tema propio a una escena. Cada entrada lleva el nombre de la escena tal como aparece en Build Profiles, su clip y sus puntos de loop. Una escena sin entrada usa el tema por defecto; una entrada con el clip vacío deja esa escena en silencio.

## Volumen del jugador

`AudioVolumeSettings` guarda dos valores de 0 a 1, **Music** y **Effects**, que multiplican toda la mezcla que define `RetroAudioSettings`. No reemplazan esos volúmenes: los escalan, así que la proporción entre un bit de letra y una melodía de recompensa se conserva. Se guardan en `PlayerPrefs` (`audio.music` y `audio.effects`), sobreviven a los cambios de escena y a cerrar el juego, y arrancan en 50% la música y 75% los efectos. Esos valores por defecto están en `AudioVolumeSettings` (`DefaultMusic` y `DefaultEffects`) y solo aplican mientras el jugador no haya movido los deslizadores: una vez guardado, manda lo guardado.

Las tres voces los leen en vivo, cada frame: `RetroFeedbackAudio` multiplica Effects en sus seis tonos, y `LoginMusicPlayer` y `WorldMusicPlayer` multiplican Music sobre su propio volumen. Mover el control se escucha de inmediato, sin reiniciar nada.

La interfaz está en la página **Audio** del menú flotante (`GameplayMenuWindow`): dos deslizadores con su porcentaje, que se sincronizan al abrir la página y escriben a disco al salir de ella. El evento `AudioVolumeSettings.Changed` existe para cualquier otra UI que deba reflejar el valor.
