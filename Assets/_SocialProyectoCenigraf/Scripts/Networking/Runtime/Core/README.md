# Módulo Núcleo del Cliente (`Runtime/Core`)

Este módulo proporciona los componentes esenciales de infraestructura para ejecutar operaciones de red asíncronas dentro del motor Unity.

---

## Componentes

### 1. `UnityMainThreadDispatcher.cs`
- **Problema que resuelve**: Unity prohíbe manipular la mayoría de sus APIs (`GameObject`, `Transform`, `UnityEngine.UI`) desde hilos secundarios en segundo plano. Dado que los WebSockets operan en un hilo asíncrono en segundo plano, cualquier actualización directa causaría excepciones fatales.
- **Solución**: Mantiene una cola concurrente (`ConcurrentQueue<Action>`) que despacha y ejecuta de forma segura todas las tareas pendientes en el hilo principal durante el método `Update()`.

### 2. `NetworkClientState.cs`
- Enumeración del ciclo de vida de la conexión del cliente: `Disconnected`, `Connecting`, `Connected`, `Authenticated` y `Reconnecting`.
