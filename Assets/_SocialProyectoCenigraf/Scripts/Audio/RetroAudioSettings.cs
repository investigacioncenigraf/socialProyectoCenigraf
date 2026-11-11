using UnityEngine;

namespace SocialProyectoCenigraf.Audio
{
    // Theme of a single scene. An entry without clip leaves that scene in silence.
    [System.Serializable]
    public sealed class SceneMusicEntry
    {
        [Tooltip("Nombre de la escena, tal como aparece en Build Profiles.")]
        public string sceneName;
        [Tooltip("Tema de esa escena. Vacío la deja en silencio.")]
        public AudioClip clip;
        [Tooltip("Segundo del clip donde arranca cada repetición.")]
        public float loopStartSeconds;
        [Tooltip("Segundo donde reinicia el loop. 0 usa el clip completo.")]
        public float loopEndSeconds;
    }

    [CreateAssetMenu(menuName = "Social Proyecto Cenigraf/Audio/Retro Audio Settings")]
    public sealed class RetroAudioSettings : ScriptableObject
    {
        [Header("Volumen de efectos retro")]
        [Range(0f, 1f)] public float hoverVolume = 0.07f;
        [Range(0f, 1f)] public float letterVolume = 0.08f;
        [Range(0f, 1f)] public float interactionVolume = 0.14f;
        [Range(0f, 1f)] public float rewardVolume = 0.16f;
        [Range(0f, 1f)] public float missionVolume = 0.16f;
        [Range(0f, 1f)] public float discoveryVolume = 0.18f;

        [Header("Música de las escenas")]
        [Tooltip("Suena el tema de mundo en todas las escenas menos la de login.")]
        public bool worldMusicEnabled = true;
        [Tooltip("Tema por defecto de las escenas de juego.")]
        public AudioClip worldMusicClip;
        [Range(0f, 1f)] public float worldMusicVolume = 0.3f;
        [Tooltip("Segundos del fundido de entrada al llegar a una escena.")]
        public float worldMusicFadeInSeconds = 1.25f;
        [Tooltip("Segundos del fundido de salida al abandonar una escena.")]
        public float worldMusicFadeOutSeconds = 0.6f;
        [Tooltip("Silencio entre el fundido de salida y el de entrada. Se cuenta desde que el tema anterior queda en silencio.")]
        public float worldMusicEnterDelaySeconds = 2f;
        [Tooltip("Segundo del clip donde arranca cada repetición, para saltarse el silencio inicial.")]
        public float worldMusicLoopStartSeconds = 0f;
        [Tooltip("Segundo donde reinicia el loop, para saltarse el silencio final. 0 usa el clip completo.")]
        public float worldMusicLoopEndSeconds = 0f;
        [Tooltip("Temas propios por escena. Una escena sin entrada usa el tema por defecto.")]
        public SceneMusicEntry[] worldMusicScenes = new SceneMusicEntry[0];

        // Null means the scene has no entry of its own and takes the default theme.
        public SceneMusicEntry FindSceneMusic(string sceneName)
        {
            if (worldMusicScenes == null || string.IsNullOrEmpty(sceneName)) return null;

            foreach (SceneMusicEntry entry in worldMusicScenes)
            {
                if (entry != null && entry.sceneName == sceneName) return entry;
            }

            return null;
        }

        [Header("Música de login")]
        [Tooltip("Suena el tema en la pantalla de login y lo apaga al iniciar sesión.")]
        public bool loginMusicEnabled = true;
        [Tooltip("Tema que suena en loop mientras el jugador escribe su usuario.")]
        public AudioClip loginMusicClip;
        [Range(0f, 1f)] public float loginMusicVolume = 0.35f;
        [Tooltip("Segundos que tarda el tema en aparecer al arrancar el juego.")]
        public float loginMusicFadeInSeconds = 1.25f;
        [Tooltip("Segundos que tarda en apagarse cuando el jugador inicia sesión.")]
        public float loginMusicFadeOutSeconds = 2.5f;
        [Tooltip("Segundo del clip donde arranca cada repetición del loop.")]
        public float loginMusicLoopStartSeconds = 0f;
        [Tooltip("Segundo donde reinicia el loop, para saltarse el silencio final del tema. 0 usa el clip completo.")]
        public float loginMusicLoopEndSeconds = 59.6f;

        [Header("Interacción")]
        [Tooltip("Reemplaza el tono generado del botón de interacción. Vacío usa el tono de siempre.")]
        public AudioClip interactionClip;
    }
}
