# Cobertura documental y pendientes - NIWI-MU

## Estado y base de revisión

- Corte: 18 de octubre de 2026.
- Código revisado: `main`, commit `1f1abe5`, que integra el PR 7 y la resolución `e2eca4f`.
- Referencia normativa de comportamiento: código C#, assets, prefabs y `ProjectSettings` de esa revisión.
- Esta actualización modifica documentación; no agrega capacidades al juego ni publica una build.
- **Implementado** describe código existente. **Parcial** distingue una infraestructura de una funcionalidad terminada. **Pendiente de validar** identifica pruebas aún necesarias.

## Diagnóstico de la documentación anterior

`main.tex` y `Resumen_Avances_Cenigraf.tex` describen la arquitectura y el estado de agosto. Son antecedentes útiles, pero su texto sobre Estudiante, escenas habilitadas y recogida del carnet no debe interpretarse como el contrato actual de los sistemas nuevos.

Había instrucciones breves dentro de `Scripts/Dialogue`, `Missions`, `Rewards`, `Audio`, `Mobile` y `World/Teleport`. Faltaba una explicación técnica consolidada en `_Doc` sobre sus dependencias, campos, duración de estado, fallbacks, configuración y límites. `WEB_PREVIEW.md` cubría la primera demo, pero no diferenciaba claramente la entrada del constructor automático de la entrada de un build manual.

## Matriz de cobertura incorporada

| Área | Cobertura anterior en `_Doc` | Ampliación realizada |
|---|---|---|
| Escenas y roles | Resumen histórico con Estudiante y SceneDemo | Lista actual, Aprendiz, normalización de `student`, destino de rol frente al callback del submenú |
| Tilemaps y profundidad | Arquitectura y decoraciones generales | Sorting Layer frente a jerarquía, WorldYSort, SortPoint y limitaciones de un Tilemap ordenado como unidad |
| Modular Interaction | Sin contrato técnico consolidado | Campos, trigger, ocupantes, jugador local, diálogo opcional y misión al entrar |
| Diálogo | Sistema anterior del oso | Catálogos, API, escritura progresiva, exclusión modal y cadenas de Plaza/Lobby/ABC |
| Misiones | Sin descripción del nuevo registro | Activación idempotente, evento, consulta, duración y ausencia de evaluación de objetivos |
| Recompensas | Carnet anterior | Definición, prefab, recepción/confirmación/cancelación y promoción de rol |
| Teleports | Sin desarrollo completo | IDs, permisos de salida, fallback, cámara, cobertura negra, tiempos y colocación |
| Audio retro | Sin descripción | Cuatro voces, síntesis, frecuencia/duración/volumen, deduplicación y botones dinámicos |
| Móvil | Sin descripción | Detección, puente JS, escala CSS, safe area, radio de joystick, multitáctil e interacción contextual |
| Fuente y encabezado | Tipografía no actualizada en el resumen | NIWI-MU, TMP, autoajuste y corrección de Ellipsis a Truncate |
| Build y despliegue | Solo primera demo | Build manual frente a StartScene automático, ZIP/HTTP/hosting y significado de tamaño comprimido |
| Integración con main | Informe local Daniel/Carlos de agosto | Resolución posterior, autoridad de red y preservación de GUID/configuración WebGL |
| Pruebas | Estrategia general | Evidencia de sesión, validador reproducible y matriz pendiente en hardware real |

El detalle está en [Actualizacion_Tecnica_NIWI_MU.tex](Actualizacion_Tecnica_NIWI_MU.tex) y su [PDF](Actualizacion_Tecnica_NIWI_MU.pdf). El índice general está en [README.md](README.md).

## Capacidades que todavía faltan

| Prioridad | Pendiente real | Criterio para considerarlo resuelto |
|---|---|---|
| Alta | Pruebas en móviles físicos | Registrar dispositivo, SO, navegador y build; cubrir dos dedos, orientación, notch, carga, audio y recuperación de foco |
| Alta | Suite permanente de regresión móvil/autoridad | Versionar pruebas del flujo y ejecutarlas automáticamente; los fixtures temporales de sesión no constituyen CI |
| Alta | Transporte multijugador Web validado | Probar conexión, envío, recepción, reconexión y autorización con un transporte compatible y endpoint desplegado |
| Alta antes de multijugador en salas | Llegada de teleport por identidad local | Resolver receptor por autoridad/playerId; el gate actual coloca al primer jugador registrado y presupone uno local |
| Media | Progreso y finalización de misiones | Modelo de estado, condiciones/eventos de objetivo, transición a completada y recompensa idempotente |
| Media | Guardado de progreso | Persistir/versionar misiones, recompensas, rol e inventario; probar recuperación y migraciones |
| Media | Destino del submenú configurable | Exponer la escena de «Ir al juego» como dato si se necesita editarla desde Inspector |
| Media | Accesibilidad y opciones de audio/control | Preferencias de volumen, tamaño/posición del HUD, revisión de contraste y cobertura de caracteres |

## Límites detectados que deben conservarse explícitos

- `objective` de una misión es texto. No existe una comparación automática contra el mundo ni un método de misión completada.
- `HasReceived` indica recepción, no confirmación. Esc puede cerrar una recompensa pendiente; el rol cambia al confirmar.
- La recompensa nueva no reemplaza automáticamente las banderas de `Interaction_oso` ni los bloqueos de `ObjetoRecogible`.
- Los permisos del teleport se evalúan en la salida y en el cliente; no autorizan todo acceso al destino ni protegen un servidor.
- El registro móvil elige por distancia al Transform; no existe prioridad explícita para empates. E se oculta para áreas sin acción manual válida.
- La interfaz móvil usa el EventSystem de la escena. Una escena nueva sin módulo UI táctil requiere configuración adicional.
- La cobertura negra corresponde a `TeleportTravel`; no cubre automáticamente todas las llamadas a `SceneNavigationService`.
- La corrección Truncate evita exigir el glifo de ellipsis en el encabezado; no modifica el atlas de la fuente.
- El tamaño de 22 MiB pertenece a la primera build comprimida revisada. No es una medición de cada build posterior ni de su memoria de ejecución.
- `Network` y `Networking` contienen transportes diferentes; el bridge de replicación del jugador no constituye por sí solo una conexión online.

## Evidencia y reproducibilidad

Durante las modificaciones anteriores se verificaron compilación, referencias de las cuatro escenas habilitadas y el contrato de snapshots mediante `IntegrationValidation.Run`. En la copia de pruebas se ejecutaron dos casos de integración adicionales: flujo táctil y rechazo de entrada/trigger de un jugador remoto. Los dos pasaron.

También se comprobaron distribuciones simuladas de 874 × 402, 402 × 874 y 1194 × 834 y casos de identificación de navegador. Esto no sustituye la prueba real de sensores táctiles, DPI, rendimiento, audio o safe area.

Los fixtures móviles temporales y sus logs quedaron fuera del repositorio; por tanto, no se documenta un comando de Test Runner como si esos tests ya estuvieran versionados. El comando disponible y sus límites se describen en el manual técnico. En esta tarea se validan los documentos y su PDF, no se vuelve a compilar/publicar el juego.

## Mantenimiento documental

- Conservar los informes de agosto como antecedentes fechados.
- Actualizar la revisión, lista de escenas y tabla de capacidades al modificar la implementación.
- Revisar las notas cortas junto al código cuando cambien sus contratos: por ejemplo, el README de audio todavía menciona dos voces, mientras el código actual contiene cuatro; la ruta inicial de ejemplo del README de teleport no es el prefab general actual.
- No duplicar archivos `.meta` ni cambiar GUID al reorganizar escenas. La carpeta actual de las escenas anteriores es `Scenes/Deprecate`.
- Si se incorpora guardado, finalización de misiones o un transporte Web nuevo, actualizar explícitamente los estados «Parcial/Pendiente» y agregar evidencia de aceptación.
