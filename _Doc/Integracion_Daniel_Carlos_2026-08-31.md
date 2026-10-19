# Integración local de Daniel y Carlos

## Estado

- Base: `main` en `5e32986`.
- Daniel: `f89d562` (`origin/_rama_programacion_Daniel`).
- Carlos: `eb060bf` (`origin/_rama_programacion_Carlos`).
- Rama preparada: `integration/daniel-carlos-main-20260831`.
- No se ha actualizado `main`, publicado cambios ni modificado las ramas de origen.
- El stash de GitHub Desktop de Carlos se conserva intacto (solo contiene `.DS_Store`).

## Resoluciones

- `SceneDemo.unity` se integró por objetos y sus identificadores, evitando que el merge textual mezclara componentes de objetos distintos.
- Se conservó el prefab de Player creado por Daniel, actualizado con los componentes, estado, colores y personalización que ya existían en `main`. Sus GUID e identificadores de prefab se conservaron.
- Se conservaron las calibraciones de profundidad de las decoraciones realizadas por Daniel.
- Se incorporaron el oso, diálogos, carnet, audio, áreas restringidas y recursos de Carlos.
- Se conserva un único EventSystem: el de `main`. No se añadió el segundo sistema de eventos de Carlos.
- Se asignó el tag `Player` al prefab, necesario para activar los diálogos de Carlos.
- `RecogerObjeto.cs` se renombró a `ObjetoRecogible.cs`, conservando el `.meta` y su GUID, para que coincida con su clase MonoBehaviour. Se corrigieron también los identificadores de clase de la escena.
- Se conservaron las fuentes y configuración TextMesh Pro de la integración actual: los recursos de Carlos eran duplicados o serializaciones anteriores de esas mismas fuentes.
- Los límites de cámara de Carlos se aplican antes de `CameraAction.SetPosition`, en vez de modificar el Transform después del estado. Si el zoom excede el tamaño del mapa, se centra ese eje.

## Decisiones que conviene revisar visualmente

- `ObjDecoration_` fue eliminado en Carlos, pero `main` lo había reubicado a X = -9.97. Se conservó el objeto completo y su conexión al padre para no descartar esa modificación de `main`.
- Se mantiene activa la skin por capas `Demo` y su personalización. La imagen `Skin_2_aprendiz` de Carlos y su referencia en el objeto visual antiguo se conservan, pero ese visual sigue inactivo para no superponer dos personajes.

## Validación realizada

- Compilación e importación con Unity `6000.3.18f1` en una copia de trabajo independiente.
- Apertura de `InitialDemoScene` y `SceneDemo` sin scripts faltantes ni referencias rotas detectadas.
- Un solo `PlayerStateStore`, conectado al prefab, y un solo EventSystem.
- Referencias de cámara, previsualización, personalización y carnet al Player verificadas.
- Las dos áreas restringidas siguen referenciadas por el carnet.
- Pruebas de límites de cámara normales y con zoom mayor que el mapa.
- Identificadores únicos y referencias locales completas en los 1021 objetos serializados de la escena y 44 del prefab.
- Resultado automatizado: `INTEGRATION VALIDATION PASSED`, salida 0.

La validación automatizada no sustituye una prueba visual de jugabilidad. Antes de publicar, probar:

1. Entrar desde el menú con un rol y comprobar el movimiento/colisiones.
2. Abrir personalización con `M`, seleccionar colores, guardar y comprobar el resultado.
3. Hablar con el oso usando `E`, obtener el carnet y verificar la apertura de las áreas restringidas.
4. Comprobar los límites de cámara y el retorno con `B`.

## Integrar después de aprobar la prueba

Desde la carpeta original del proyecto, con `main` limpio y actualizado:

```sh
git fetch origin
git switch main
git merge --ff-only origin/main
git merge --ff-only integration/daniel-carlos-main-20260831
```

Si el último comando rechaza el avance rápido porque alguien actualizó `main`, detenerse y volver a integrar esos cambios; no forzar. Después de revisar el resultado, se puede publicar `main` mediante el flujo acordado del equipo o publicar la rama de integración y abrir un pull request.
