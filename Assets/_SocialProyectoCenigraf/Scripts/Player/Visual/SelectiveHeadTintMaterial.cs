using UnityEngine;

namespace SocialProyectoCenigraf.Player.Visual
{
    public static class SelectiveHeadTintMaterial
    {
        private const string ResourcePath =
            "Shaders/CenigrafSelectiveHeadTint";
        private const string ShaderName = "Cenigraf/Selective Head Tint";

        private static Material sharedMaterial;

        public static Material Shared
        {
            get
            {
                if (sharedMaterial != null)
                {
                    return sharedMaterial;
                }

                Shader shader = Resources.Load<Shader>(ResourcePath);

                if (shader == null)
                {
                    shader = Shader.Find(ShaderName);
                }

                if (shader == null)
                {
                    Debug.LogError(
                        $"The selective head tint shader '{ShaderName}' " +
                        "could not be loaded.");
                    return null;
                }

                sharedMaterial = new Material(shader)
                {
                    name = "Cenigraf Selective Head Tint (Runtime)",
                    hideFlags = HideFlags.HideAndDontSave
                };
                return sharedMaterial;
            }
        }
    }
}
