# Documentación de estados - Proyecto Cenigraf

Esta carpeta contiene la documentación LaTeX de la arquitectura de estados aplicada al proyecto Unity.

## Estructura

- `main.tex`: documento principal.
- `sections/`: capítulos editables.
- `images/`: diagramas de referencia incorporados al proyecto.
- `build/`: archivos generados por LaTeX y PDF final.

## Compilación

Desde esta carpeta:

```bash
tectonic --outdir build main.tex
```

El compilador genera `build/main.pdf`. Para publicar una versión estable, copie ese resultado como `Arquitectura_Estados_Cenigraf.pdf` en esta carpeta. Las rutas del documento son relativas, por lo que `_Doc` puede versionarse y compilarse sin depender de `Downloads`.

## Criterio de lectura

El documento etiqueta explícitamente cada elemento como **implementado**, **parcial** o **planeado**. Los diagramas son una especificación de diseño; el código C# del repositorio es la referencia para el estado actual de la implementación.

## Resumen de avances (31 de agosto de 2026)

`Resumen_Avances_Cenigraf.tex` es un documento independiente que resume Redux, personaje, mundo, Object Decoration, carnet, roles, escenas y publicaciones. Su versión compilada es `Resumen_Avances_Cenigraf.pdf`. No sustituye la documentación de arquitectura anterior.

Para compilar el resumen desde `_Doc`:

```bash
tectonic Resumen_Avances_Cenigraf.tex
```
