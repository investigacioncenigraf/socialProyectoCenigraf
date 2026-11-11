# Catálogo e inventario de ítems

Crea o edita `Asset/Items/ItemCatalog_Example.asset` para declarar ítems. Cada definición tiene un ID estable, nombre visible, descripción, icono y reglas de acumulación. Un diálogo puede entregar un ítem al completarse con `itemCatalog`, `completionItemId` y `completionItemQuantity`.

El catálogo de ejemplo incluye `carnet_estudiantil` y `pocion_salud_basica`. La ruta `InscritoRecompensa` de `Estudiante_Angustiado_Carnet` concede una Poción de Salud Básica y muestra la recompensa al terminar esa rama.

`PlayerItemInventory` registra cantidades durante la partida y conserva el inventario al cambiar de escena. El inventario se reinicia al iniciar una nueva sesión de juego y todavía no se guarda al cerrar la aplicación ni tiene una pantalla de inventario.

Desde código se puede consultar `PlayerItemInventory.HasItem("carnet_estudiantil")`, `GetQuantity(id)` o `Items`. El evento `ItemAdded` informa cuando cambia la cantidad de un ítem.
