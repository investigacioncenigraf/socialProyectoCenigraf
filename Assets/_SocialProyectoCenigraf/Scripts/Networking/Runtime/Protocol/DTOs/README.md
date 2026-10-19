# Objetos de Transferencia de Datos en Unity (`Runtime/Protocol/DTOs`)

Este módulo contiene todos los DTOs serializables de Unity estructurados para corresponderse con los modelos y paquetes del servidor .NET.

---

## Compatibilidad de Serialización

Todas las estructuras y clases cuentan con el atributo `[Serializable]` y nombres de campos en minúscula (*camelCase*), haciéndolas 100% compatibles tanto con:
- `UnityEngine.JsonUtility` (nativo de Unity sin overhead).
- `System.Text.Json` de .NET.
- `Newtonsoft.Json` (Json.NET).

---

## DTOs Incluidos
- **`Vector3Dto`**: Representación serializable ligera de coordenadas 3D para evitar enviar la estructura pesada de Unity por la red.
- **`AuthRequestPayload`** y **`AuthResponsePayload`**: Negociación de inicio de sesión.
- **`UserSyncPayload`** y **`UserListSyncPayload`**: Notificación de jugadores en el servidor.
- **`PositionUpdatePayload`**: Envío y recepción de movimiento espacial.
- **`ChatSendMessagePayload`**, **`ChatBroadcastPayload`** y **`ChatHistoryPayload`**: Ciclo de vida completo del chat.
- **`ErrorPayload`**: Manejo unificado de excepciones y rechazos.
