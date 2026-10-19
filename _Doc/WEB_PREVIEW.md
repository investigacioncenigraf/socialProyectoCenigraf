# Build y publicación Web - NIWI-MU

## Estado

Revisión documental: 18 de octubre de 2026, `main` en `1f1abe5`. Unity `6000.3.18f1` con WebGL Build Support. Esta guía no genera ni publica una build por sí misma.

La primera demo publicada comenzaba directamente en Plaza Central. Los sonidos, controles táctiles y demás cambios posteriores solo aparecen en un hosting después de reconstruir y sustituir sus archivos.

## Dos formas de construir, con entradas distintas

| Ruta | Escena inicial | Uso |
|---|---|---|
| Unity → File → Build Profiles → Web → Build | Primera escena de la lista del perfil | Flujo completo con selección de roles |
| Menú Cenigraf → Build → Compilar versión Web de prueba | `Plaza_Central`, fijada en código | Prueba rápida del recorrido |
| `python3 tools/build_web_preview.py` | `Plaza_Central`, usa el mismo constructor | Prueba desde copia independiente, sin bloquear el editor original |

En la lista global actual, `Scenes/Deprecate/InitialDemoScene.unity` está habilitada y es la primera; Plaza Central, Sala ABC y Lobby también están habilitadas. `Deprecate/SceneDemo.unity` está deshabilitada. Revisar si el Build Profile activo utiliza una lista propia.

Cambiar Scene List **no modifica** `WebPreviewBuild.StartScene`: el constructor de demo antepone expresamente Plaza Central. Para iniciar con el menú, utilizar la compilación manual o cambiar de forma consciente esa constante y validar otra vez el recorrido.

## Build manual desde Unity

1. Guardar escenas y assets; salir del modo Play.
2. Abrir **File → Build Profiles**, seleccionar **Web** y activar la plataforma si hace falta.
3. Comprobar la Scene List y la escena de entrada.
4. Desactivar **Development Build** para la entrega de prueba pública.
5. En **Player Settings → Web → Publishing Settings**, usar **Compression Format = Gzip** y **Decompression Fallback activado** para una entrega estática sin configuración especial de cabeceras.
6. Generar en una carpeta nueva o controlada, por ejemplo `Builds/WebGL`.
7. Probar por HTTP antes de crear el ZIP.

El fallback incluye descompresión en el loader y produce archivos `.unityweb`; no confundir esta opción con el ZIP que se utiliza para subir los archivos.

## Constructor automatizado

Desde la raíz Unity:

```bash
python3 tools/build_web_preview.py
```

Parámetros disponibles:

```bash
python3 tools/build_web_preview.py --output /ruta/absoluta/NIWI-MU-Web
python3 tools/build_web_preview.py --editor /ruta/al/ejecutable/de/Unity
```

- Lee la versión desde `ProjectSettings/ProjectVersion.txt`.
- Por defecto busca Unity Hub en la ruta de macOS; en otro SO indicar `--editor`.
- Copia Assets, Packages y ProjectSettings a `Builds/WebPreviewProject`, conservando la Library de esa copia para reutilizar importaciones.
- No captura cambios sin guardar y no debe ejecutarse dos veces simultáneamente sobre la misma copia.
- Deja la salida en `Builds/WebGL` y el log en `Builds/web-preview-build.log`.
- Invoca `SocialProyectoCenigraf.Editor.WebPreviewBuild.Run`, que usa Gzip, fallback, nombres con hash y `BuildOptions.None`; restaura sus opciones de compresión al terminar.
- El constructor C# admite la variable `CENIGRAF_WEB_OUTPUT`; el script Python la establece según `--output`.

## Prueba local

```bash
python3 -m http.server 8080 --directory Builds/WebGL
```

Abrir `http://localhost:8080`. No abrir `index.html` mediante `file://`. Hacer clic dentro del juego para dar foco al teclado y habilitar audio conforme a las políticas del navegador.

En escritorio: WASD/flechas y E. En smartphone/tablet: joystick y botón E contextual. Durante diálogo, E continúa; la recompensa tiene su propio botón Continuar. Reducir la ventana de PC no activa automáticamente los controles móviles.

Si se usa una carpeta de salida diferente, ajustar `--directory` a esa ruta. Detener el servidor al finalizar.

## Paquete de publicación

La raíz del ZIP debe contener directamente:

```text
index.html
Build/
TemplateData/
...otros archivos generados que necesite la build
```

Comprimir **el contenido** de la carpeta, conservando estructura, nombres y mayúsculas. No subir Assets, Library, la copia `WebPreviewProject` ni el proyecto Unity completo. Conservar la licencia de Press Start 2P en la distribución.

Para itch.io:

1. Crear o editar el proyecto y elegir tipo **HTML**.
2. Subir el ZIP y marcar **This file will be played in the browser**.
3. Configurar el área de juego, botón de pantalla completa y **Click to Play**.
4. Guardar y probar la vista previa: escena inicial, controles, carga de otra sala, sonido y confirmación del carnet.
5. Elegir la visibilidad apropiada y publicar cuando la prueba esté lista. Una página no listada no debe confundirse con autorización privada de servidor.
6. Para actualizar, reemplazar el ZIP por la build nueva y volver a comprobar el flujo. El editor no sincroniza cambios automáticamente.

Referencias: [guía oficial HTML5 de itch.io](https://itch.io/docs/creators/html5), [Decompression Fallback de Unity](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/playersettings/webgl/decompressionfallback).

## Tamaño de descarga y memoria

La build anterior revisada ocupaba aproximadamente 22 MiB comprimidos: 12 MiB de WebAssembly y 10 MiB de datos, además de archivos menores. Es una medición histórica, no una garantía para la revisión actual.

Comparar siempre la misma plataforma y configuración. Una build nativa, archivos descomprimidos, símbolos, caché Library y RAM del navegador son medidas distintas. Un juego comprimido de 22 MiB puede ocupar más memoria al ejecutarse.

## Diagnóstico

| Síntoma | Revisar |
|---|---|
| Abre Plaza en lugar del menú | Si se utilizó `WebPreviewBuild`/script Python; su StartScene es Plaza |
| Falta una sala o el teleport rechaza el viaje | Scene List del perfil, escena habilitada y guardada, ruta de destino |
| HTTP 404/403 en recursos | `index.html` en la raíz, estructura relativa y mayúsculas del ZIP |
| Error de descompresión | Gzip/fallback coherentes y archivos procedentes de la misma build; no mezclar versiones |
| No aparecen controles en PC al reducir ventana | Es el comportamiento esperado: se detecta dispositivo, no solo ancho |
| No responde el joystick en móvil | EventSystem/InputSystemUIInputModule, build nueva, canvas y eventos táctiles; comprobar dos dedos en hardware real |
| No suena al abrir la página | Primer gesto del usuario, AudioListener, volumen del dispositivo y `RetroAudioSettings` |
| Warning de ellipsis | Header con Overflow Truncate; otros textos pueden necesitar glifos o fallbacks adicionales |
| El juego abre pero no conecta al servidor | Compatibilidad del transporte y endpoint de red; un hosting estático no despliega el backend |

## Sitio anterior de pruebas

La demo inicial se publicó en `https://cenigraf-parque-central-20261017.awakesquid14.chatgpt.site`, privada para su propietario en el momento de creación. Los archivos locales del sitio se conservaron en el directorio hermano `web-preview`, fuera de la raíz Unity. No es un requisito para publicar en otro proveedor ni una dependencia del juego. Esta revisión documental no verifica su disponibilidad ni cambia su audiencia.

## Alcance de validación pendiente

Probar la build subida, no solo el Editor: Safari iOS, Chrome Android, tablet, orientación horizontal/vertical, notch, dos dedos, pérdida de foco, varias transiciones, audio y consumo de memoria. La capa de controles móviles no demuestra compatibilidad del transporte multijugador incluido por otras ramas.
