# Ventana de recompensas

Prefab: `Resources/UI/RewardWindow.prefab`. Es un Canvas completo y centrado, con fondo atenuado, imagen, nombre del objeto, descripción y botón Continuar. Usa Press Start 2P.

## Prueba independiente

Arrastra RewardWindow a la jerarquía. Marca **Show Test On Start** y entra en Play. También puedes usar el menú del componente **Probar recompensa: Carnet de aprendiz** mientras estás en Play. La prueba realiza una entrega durante la partida: al confirmar Continuar también aplica el rol configurado.

Para previsualizar el diseño en Prefab Mode activa temporalmente el hijo Backdrop. Déjalo desactivado al guardar. Ajusta colores y dimensiones en RewardPanel y sus hijos.

## Entrega desde diálogo

En un Dialogue Catalog, cada cadena tiene el campo **Completion Reward**. Asigna una RewardDefinition para mostrarla al completar toda la cadena. El diálogo `recepcion_lobby_entrega_carnet` ya tiene `Reward_CarnetAprendiz`.

Esc durante el diálogo cancela sin entregar. Continuar, E o Enter confirman la recompensa y aplican su Role On Continue. Esc cierra sin confirmar ni cambiar el rol; al repetir el diálogo se puede volver a mostrar una entrega pendiente. Una entrega ya recibida no se duplica. La ventana pausa el movimiento y lo restaura al cerrar. Bloquea otras interacciones mientras esté abierta.

## Nuevas recompensas

Project → Create → Cenigraf → Reward. Configura ID único, Display Name, Description e Icon. La misma ventana muestra cualquier definición. La entrega real se registra una vez por ID y partida; persiste entre escenas, no al cerrar el juego. Se puede consultar con RewardWindow.HasReceived(id). No modifica el antiguo sistema de recogida del oso ni sus condiciones de acceso.

Desde código: `RewardWindow.GrantAndShow(reward, player)`. Carga el prefab automáticamente desde Resources; no es necesario añadirlo a todas las escenas.

## Carnet y rol Aprendiz

Reward_CarnetAprendiz tiene Role On Continue = ROLE_Apprentice. Al confirmar, actualiza PlayerStateStore y GameSessionStore con el ID apprentice. Así se conserva el rol al cambiar de escena y los teleports lo evalúan inmediatamente. El antiguo ID student se interpreta como apprentice para conservar compatibilidad con estados y exclusiones anteriores. El ID de objeto carnet_estudiantil se mantiene estable; el nombre visible es Carnet de aprendiz.
