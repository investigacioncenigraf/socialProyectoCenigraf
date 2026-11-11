using UnityEngine;

namespace SocialProyectoCenigraf.World.NPC
{
    internal static class NPCLayerOutlineMaterial
    {
        private const string ResourcePath = "Shaders/CenigrafNPCOutlineTint";
        private const string ShaderName = "Cenigraf/NPC Layer Outline Tint";
        private static Material sharedMaterial;

        public static Material GetShared(Shader assignedShader)
        {
            if (assignedShader == null && sharedMaterial != null)
                return sharedMaterial;

            Shader shader = assignedShader;
            if (shader == null)
            {
                shader = Resources.Load<Shader>(ResourcePath);
                if (shader == null) shader = Shader.Find(ShaderName);
            }

            if (shader == null)
                return null;

            if (sharedMaterial != null && sharedMaterial.shader == shader)
                return sharedMaterial;

            if (sharedMaterial != null)
            {
                if (Application.isPlaying) Object.Destroy(sharedMaterial);
                else Object.DestroyImmediate(sharedMaterial);
            }

            sharedMaterial = new Material(shader)
            {
                name = "Cenigraf NPC Layer Outline (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            return sharedMaterial;
        }
    }
}
