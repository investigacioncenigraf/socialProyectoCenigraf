#!/usr/bin/env python3
"""Build a WebGL preview from a saved snapshot without locking the open Unity project."""
import argparse
import os
from pathlib import Path
import re
import shutil
import subprocess

project = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, default=project / 'Builds' / 'WebGL')
parser.add_argument('--editor', type=Path)
args = parser.parse_args()
version = re.search(r'm_EditorVersion: (.+)', (project / 'ProjectSettings/ProjectVersion.txt').read_text()).group(1)
editor = args.editor or Path('/Applications/Unity/Hub/Editor') / version / 'Unity.app/Contents/MacOS/Unity'
if not editor.is_file():
    parser.error('No se encontró Unity. Indica --editor con la ruta al ejecutable.')
snapshot = project / 'Builds' / 'WebPreviewProject'
snapshot.mkdir(parents=True, exist_ok=True)
for name in ('Assets', 'Packages', 'ProjectSettings'):
    destination = snapshot / name
    if destination.exists():
        shutil.rmtree(destination)
    shutil.copytree(project / name, destination)
output = args.output.resolve()
if output == snapshot or snapshot in output.parents:
    parser.error('La salida debe estar fuera de la copia temporal del proyecto.')
log = project / 'Builds' / 'web-preview-build.log'
environment = os.environ.copy()
environment['CENIGRAF_WEB_OUTPUT'] = str(output)
print(f'Compilando escenas guardadas con Unity {version}. Log: {log}', flush=True)
result = subprocess.run([str(editor), '-batchmode', '-nographics', '-quit', '-buildTarget', 'WebGL',
    '-projectPath', str(snapshot), '-executeMethod', 'SocialProyectoCenigraf.Editor.WebPreviewBuild.Run',
    '-logFile', str(log)], env=environment)
if result.returncode or not (output / 'index.html').is_file():
    raise SystemExit(f'No se generó la build. Revisa {log}')
print(f'Build lista en {output}. Sírvela por HTTP; no abras index.html como archivo local.')
