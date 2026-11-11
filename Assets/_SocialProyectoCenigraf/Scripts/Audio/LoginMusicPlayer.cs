using SocialProyectoCenigraf.Session.State;
using SocialProyectoCenigraf.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SocialProyectoCenigraf.Audio
{
    // Login theme. It starts on the first frame of the first scene, which the Unity
    // splash is still covering, loops while the player types, and fades out the moment
    // the session starts. It lives in DontDestroyOnLoad so the fade can finish after the
    // lobby has already been loaded.
    [DisallowMultipleComponent]
    public sealed class LoginMusicPlayer : MonoBehaviour
    {
        private const float MaxFadeStep = 0.1f;

        private static LoginMusicPlayer instance;

        private RetroAudioSettings settings;
        private AudioSource source;
        private GameSessionStore store;
        private float peakVolume = 0.35f;
        private float fadeInSeconds = 1.25f;
        private float fadeOutSeconds = 2.5f;
        private float loopStart;
        private float loopEnd;
        private float lastStartTime = float.NegativeInfinity;
        private bool active;
        private bool fadingOut;

        public static bool IsPlaying => instance != null && instance.active;

        /// <summary>Audible, fade out included. The scene theme waits on this so the two
        /// never overlap during the first scene change.</summary>
        internal static bool IsSounding =>
            instance != null && (instance.active || instance.fadingOut);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        // AfterSceneLoad is the earliest audible moment of the game: the first scene
        // already has its AudioListener and the splash screen is still drawn over it.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (instance != null || !IsLoginScene())
            {
                return;
            }

            RetroAudioSettings audioSettings = LoadSettings();
            if (audioSettings == null ||
                !audioSettings.loginMusicEnabled ||
                audioSettings.loginMusicClip == null ||
                GameSessionStore.Instance.State.HasActiveSession)
            {
                return;
            }

            instance = new GameObject(nameof(LoginMusicPlayer))
                .AddComponent<LoginMusicPlayer>();
        }

        public static void FadeOut()
        {
            if (instance != null)
            {
                instance.BeginFadeOut();
            }
        }

        private static RetroAudioSettings LoadSettings() =>
            Resources.Load<RetroAudioSettings>("Audio/RetroAudioSettings");

        // The theme belongs to the login screen, not to a scene name: playing from any
        // other scene in the Editor must stay silent. WorldMusicPlayer reads it too, so
        // the scene theme stays out of the login.
        internal static bool IsLoginScene() => LoginController.ExistsInScene();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            settings = LoadSettings();
            AudioClip clip = settings != null ? settings.loginMusicClip : null;
            if (clip == null)
            {
                Destroy(gameObject);
                return;
            }

            peakVolume = ResolvePeak();
            fadeInSeconds = Mathf.Max(0f, settings.loginMusicFadeInSeconds);
            fadeOutSeconds = Mathf.Max(0f, settings.loginMusicFadeOutSeconds);
            loopStart = Mathf.Clamp(
                settings.loginMusicLoopStartSeconds,
                0f,
                Mathf.Max(0f, clip.length - 1f));
            loopEnd = settings.loginMusicLoopEndSeconds > loopStart + 1f
                ? Mathf.Min(settings.loginMusicLoopEndSeconds, clip.length)
                : clip.length;

            source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            // Unity's own loop is sample perfect; the manual one only exists to cut the
            // silence a theme can carry after its fade out.
            source.loop = loopEnd >= clip.length - 0.01f;

            store = GameSessionStore.Instance;
            store.StateChanged += HandleSessionChanged;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            StartTheme();
        }

        private float ResolvePeak() => settings == null
            ? 0f
            : Mathf.Clamp01(settings.loginMusicVolume) * AudioVolumeSettings.Music;

        private void OnDestroy()
        {
            if (store != null)
            {
                store.StateChanged -= HandleSessionChanged;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;

            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            // A synchronous scene load can leave a huge frame behind it; a capped step
            // keeps the fade smooth instead of jumping to the end of it.
            float delta = Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep);

            if (fadingOut)
            {
                float fadeOutStep = fadeOutSeconds <= 0f
                    ? peakVolume
                    : peakVolume * delta / fadeOutSeconds;
                source.volume = Mathf.Max(0f, source.volume - fadeOutStep);
                if (source.volume <= 0.001f)
                {
                    source.volume = 0f;
                    source.Stop();
                    fadingOut = false;
                }

                return;
            }

            if (!active)
            {
                return;
            }

            // Re-read every frame: both the asset and the player's own volume can move
            // while the theme is sounding.
            peakVolume = ResolvePeak();

            if (source.volume > peakVolume)
            {
                source.volume = peakVolume;
            }
            else if (source.volume < peakVolume)
            {
                float fadeInStep = fadeInSeconds <= 0f
                    ? peakVolume
                    : peakVolume * delta / fadeInSeconds;
                source.volume = Mathf.Min(peakVolume, source.volume + fadeInStep);
            }

            if (source.loop)
            {
                return;
            }

            bool reachedLoopEnd = source.time >= loopEnd;
            bool stalled = !source.isPlaying &&
                Time.unscaledTime - lastStartTime > 0.25f;
            if (reachedLoopEnd || stalled)
            {
                Rewind();
            }
        }

        private void StartTheme()
        {
            active = true;
            fadingOut = false;
            source.volume = fadeInSeconds <= 0f ? peakVolume : 0f;
            source.Stop();
            source.Play();
            lastStartTime = Time.unscaledTime;
        }

        private void Rewind()
        {
            source.Stop();
            source.Play();
            if (loopStart > 0.01f)
            {
                source.time = loopStart;
            }

            lastStartTime = Time.unscaledTime;
        }

        private void BeginFadeOut()
        {
            if (!active || fadingOut)
            {
                return;
            }

            active = false;

            if (fadeOutSeconds <= 0f || source.volume <= 0.001f)
            {
                source.volume = 0f;
                source.Stop();
                return;
            }

            fadingOut = true;
        }

        private void HandleSessionChanged(
            GameSessionStateData state,
            GameSessionAction action)
        {
            if (action.Type == GameSessionActionType.StartSession ||
                state.HasActiveSession)
            {
                BeginFadeOut();
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive)
            {
                return;
            }

            bool wantsMusic = IsLoginScene() &&
                !GameSessionStore.Instance.State.HasActiveSession;

            if (wantsMusic)
            {
                // Coming back to the login screen (logout or Back) brings the theme back.
                if (!active)
                {
                    StartTheme();
                }

                return;
            }

            BeginFadeOut();
        }
    }
}
