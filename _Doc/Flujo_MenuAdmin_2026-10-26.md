# Navegación de regreso del menú administrativo

## Estado

Actualización del 26 de octubre de 2026. Existen dos presentaciones del menú administrativo: el panel generado dentro de `InitialDemoScene` y la escena anterior `Deprecate/MenuAdmin.unity`. Ambas cuentan ahora con un botón **Volver**.

## Flujo de datos vigente

1. `RoleSelectionButton.HandleClick` registra el rol mediante `GameSessionStore.SelectRole`.
2. `InitialMenuFlowController.TryHandleRole` detecta `entryPoint = Administration` y muestra `AdministrationPanel` en la misma escena. No carga `MenuAdmin`.
3. El panel consulta `CanCreatePublications`: Administrativo puede abrir Eventos y Docente no.
4. **Volver** llama a `ShowRoleSelection`: elimina el panel generado y muestra de nuevo Header/RolePanel. No borra el store de sesión ni cambia el rol seleccionado. Al elegir otro rol se realiza un nuevo despacho normal.
5. **Ir al juego** conserva su destino `Plaza_Central`.

`MenuBackButton` crea `BTN_Back` fuera del contenedor de opciones: cuadrado de 80 × 80 unidades de UI, anclado arriba a la izquierda, pivote (0, 1) y margen de 32 unidades. El CanvasScaler adapta estas medidas a la resolución. Las dos opciones centrales conservan su disposición. Participa en el audio automático de botones y el atajo B conserva su ruta de regreso.

Para colocar el arte, asignar **Back Button Sprite** en `InitialMenuFlowController` (menú vigente) o `NavigationMenuScreen` (escena MenuAdmin). `ICO_Back` reserva 48 × 48 unidades y conserva la proporción del Sprite. Mientras el campo esté vacío se muestra el carácter `<` como referencia de regreso. Asignar el Sprite en el controlador, no en los objetos generados, ya que estos se reconstruyen al abrir el menú.

## Escena MenuAdmin abierta directamente

`UI_MenuAdmin → NavigationMenuScreen` expone:

| Campo | Configuración en MenuAdmin |
|---|---|
| Show Back Button | Activado |
| Back Button Sprite | Vacío; icono pendiente |
| Back Fallback Scene Name | `InitialDemoScene` |

El botón generado se llama `BTN_Back` y llama a `SceneNavigationService.GoBackOrTo`:

- Con historial disponible, usa `GoBack` y consume la escena anterior.
- Sin historial, carga la escena fallback sin añadir MenuAdmin al historial. Esto evita un ciclo de regreso al abrir directamente la escena desde el Editor.
- Durante carga o viaje por teleport no inicia una navegación adicional.
- No realiza acciones sobre `GameSessionStore`, recompensas, misiones ni repositorios de publicaciones.

`GoBack` valida ahora el destino con `Peek` antes de hacer `Pop`; una escena no disponible no destruye la entrada del historial.

La opción antigua **Ir al juego** se actualiza de `SceneDemo` a `Plaza_Central`, coherente con el recorrido actual. La opción antigua **Eventos** conserva su referencia a `EventsScene`, que no está habilitada en la lista global actual. Para gestionar publicaciones, usar el flujo vigente de `InitialDemoScene`; añadir el botón Volver no reactiva esa escena anterior ni modifica el sistema de publicaciones.

## Validación

La prueba de integración comprueba entrada directa en MenuAdmin sin historial, retorno al menú inicial sin ciclos, regreso desde el panel de Administrativo y Docente, preservación de permisos y rol de sesión, y retorno real mediante historial de escenas. MenuAdmin solo se habilita en la copia de pruebas para verificar esta última ruta; la Scene List del proyecto no se cambia.

Comprobar visualmente el botón en el Editor y, antes de distribuirlo, generar una build nueva. Una publicación anterior no incorpora este cambio automáticamente.

## Regreso desde la lista de eventos

La lista de publicaciones incorpora el mismo `BTN_Back`, posición y Sprite del menú administrativo. Invoca `BackFromPublications`, compartido con el atajo B: cierra primero una confirmación de eliminación si está abierta; en caso contrario vuelve al menú administrativo. No modifica el repositorio de eventos ni el rol seleccionado.
