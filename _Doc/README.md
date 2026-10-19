# Documentación técnica - Proyecto Cenigraf / NIWI-MU

Esta carpeta conserva la arquitectura histórica y la documentación del comportamiento implementado. **NIWI-MU** es el nombre visible del juego; carpetas, namespaces y menús mantienen Cenigraf para preservar compatibilidad.

## Lectura recomendada

| Documento | Alcance y estado |
|---|---|
| [Actualización técnica NIWI-MU - PDF](Actualizacion_Tecnica_NIWI_MU.pdf) | Manual técnico con corte al 18/10/2026: interacciones, diálogos, misiones, recompensas, roles, teleports, audio, móvil, build y validación |
| [Fuente LaTeX del manual](Actualizacion_Tecnica_NIWI_MU.tex) | Editable; conserva tipografía, colores, tablas y estructura del documento de arquitectura |
| [Cobertura y pendientes](COBERTURA_Y_PENDIENTES_2026-10-18.md) | Auditoría de documentación, límites reales y criterios para completar lo pendiente |
| [Build y publicación Web](WEB_PREVIEW.md) | Procedimiento operativo; distingue inicio manual en menú del constructor de demo que antepone Plaza |
| [Arquitectura de estados](Arquitectura_Estados_Cenigraf.pdf) / [fuente](main.tex) | Diseño y contratos de referencia de agosto; capítulos editables en `sections/` |
| [Resumen de avances de agosto](Resumen_Avances_Cenigraf.pdf) / [fuente](Resumen_Avances_Cenigraf.tex) | Documento histórico; sus roles, escenas y carnet no describen todas las ampliaciones posteriores |
| [Integración Daniel/Carlos](Integracion_Daniel_Carlos_2026-08-31.md) | Informe histórico local; no es el estado de integración actual de main |

## Estructura

- `main.tex`, `sections/`, `images/`: arquitectura de referencia y diagramas.
- `Resumen_Avances_Cenigraf.tex`: resumen independiente de agosto.
- `Actualizacion_Tecnica_NIWI_MU.tex`: manual independiente de las modificaciones recientes.
- `build/`: salida temporal de compilación y revisión; los PDFs finales se conservan en esta carpeta `_Doc`.
- Archivos Markdown: índice, auditoría, instrucciones operativas e informes fechados.

## Compilación

Requiere Tectonic. Desde `_Doc`:

```bash
tectonic --outdir build Actualizacion_Tecnica_NIWI_MU.tex
cp build/Actualizacion_Tecnica_NIWI_MU.pdf Actualizacion_Tecnica_NIWI_MU.pdf
```

Para la arquitectura histórica:

```bash
tectonic --outdir build main.tex
cp build/main.pdf Arquitectura_Estados_Cenigraf.pdf
```

Para el resumen histórico:

```bash
tectonic --outdir build Resumen_Avances_Cenigraf.tex
```

La copia al PDF final se realiza después de revisar el renderizado, tablas, rutas, índice y paginación. El nuevo manual es autocontenido; la arquitectura principal incluye archivos relativos de `sections/` e imágenes del repositorio.

## Criterio de documentación

- Etiquetar cada capacidad como **implementada**, **parcial** o **pendiente**, separando implementación de validación.
- Tomar el código C#, los assets y `ProjectSettings` como referencia del estado real.
- Especificar revisión y fecha de corte; conservar los informes históricos sin presentarlos como configuración vigente.
- Documentar propietario del dato, duración, API, campos, efectos, errores, fallbacks y dependencias.
- No presentar un catálogo de misiones como motor de objetivos, un registro de recompensa como inventario persistente ni permisos locales como seguridad de servidor.
- Distinguir pruebas automatizadas versionadas, pruebas temporales de sesión y verificación física pendiente.
- Una modificación de documentación no compila ni publica automáticamente el juego.
