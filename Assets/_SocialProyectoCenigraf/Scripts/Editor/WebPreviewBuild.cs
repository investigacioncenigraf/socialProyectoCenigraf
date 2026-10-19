using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SocialProyectoCenigraf.Editor
{
    public static class WebPreviewBuild
    {
        private const string StartScene = "Assets/_SocialProyectoCenigraf/Scenes/Salas/Plaza_Central.unity";
        [MenuItem("Cenigraf/Build/Compilar versión Web de prueba")]
        public static void Run()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new InvalidOperationException("Instala WebGL Build Support para esta versión de Unity desde Unity Hub.");
            string output = Environment.GetEnvironmentVariable("CENIGRAF_WEB_OUTPUT");
            if (string.IsNullOrWhiteSpace(output)) output = Path.GetFullPath("Builds/WebGL");
            if (!File.Exists(StartScene)) throw new FileNotFoundException("Falta la escena inicial de la demo.", StartScene);
            string[] scenes = new[] { StartScene }.Concat(EditorBuildSettings.scenes
                .Where(s => s.enabled && File.Exists(s.path) && s.path != StartScene).Select(s => s.path)).ToArray();
            var oldCompression = PlayerSettings.WebGL.compressionFormat;
            bool oldFallback = PlayerSettings.WebGL.decompressionFallback;
            bool oldHashes = PlayerSettings.WebGL.nameFilesAsHashes;
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.WebGL.nameFilesAsHashes = true;
                BuildReport result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });
                if (result.summary.result != BuildResult.Succeeded)
                    throw new Exception("Falló la compilación Web: " + result.summary.result + ", errores: " + result.summary.totalErrors);
                Debug.Log("CENIGRAF WEB BUILD SUCCEEDED: " + output + " (" + result.summary.totalSize + " bytes)");
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = oldCompression;
                PlayerSettings.WebGL.decompressionFallback = oldFallback;
                PlayerSettings.WebGL.nameFilesAsHashes = oldHashes;
            }
        }
    }
}
