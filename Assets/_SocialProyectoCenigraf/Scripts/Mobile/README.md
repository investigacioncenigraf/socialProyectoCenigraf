# Controles móviles

Se crean automáticamente en smartphones y tablets, también en WebGL (incluido iPad con navegador en modo escritorio). No aparecen en PC ni por reducir la ventana del navegador. No hay que modificar cada escena o prefab.

- Base de joystick de 200 × 200 unidades de UI y mango de 100 × 100. En WebGL se ajusta la escala a los píxeles CSS del canvas: la base mantiene 200 px sin crecer excesivamente en tablets o pantallas Retina. Si el lado corto seguro es menor de 402 px, se reduce proporcionalmente para que ambos controles quepan. En apps nativas se usa la densidad de pantalla.
- Zona muerta del 12 %, movimiento analógico y diagonales limitadas. El mango permanece dentro del círculo exterior. Solo el dedo que inició el arrastre lo controla; otro dedo puede pulsar E.
- E solo aparece con un diálogo válido o teleport en rango. Si se superponen áreas se elige la más cercana. Los permisos del teleport siguen aplicándose.
- Durante un diálogo se oculta la palanca, E continúa el texto y se reserva espacio para el botón. Las recompensas utilizan su propio botón Continuar, táctil.
- Menús y transiciones ocultan el HUD. Soltar, perder foco, desactivar la palanca y cambiar de escena cancelan el movimiento.

Para probar en Unity sin teléfono, usa Device Simulator para simular un dispositivo móvil. El Game View de escritorio conserva teclado/gamepad. Para Web, genera una nueva build y vuelve a subirla al hosting; comprueba en Safari iOS/Chrome Android, horizontal y vertical, multitáctil y con notch.
