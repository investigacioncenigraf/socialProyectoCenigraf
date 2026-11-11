using UnityEngine;
using UnityEngine.SceneManagement;

namespace SocialProyectoCenigraf.Audio
{
    // Theme of the game scenes. A single persistent voice: it fades out when a scene
    // change begins and fades back in once the next scene is ready, so every transition
    // breathes even while both scenes share the same track. The login screen has its own
    // theme and is skipped here.
    [DisallowMultipleComponent]
    public sealed class WorldMusicPlayer : MonoBehaviour
    {
        private const float MaxFadeStep = 0.1f;

        private static WorldMusicPlayer instance;

        private RetroAudioSettings settings;
        private AudioSource source;
        private AudioClip pendingClip;
        private float pendingLoopStart;
        private float pendingLoopEnd;
        private bool hasPending;
        private float loopStart;
        private float loopEnd;
        private float level;
        private float targetLevel;
        private float enterTimer;
        private float lastStartTime = float.NegativeInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            RetroAudioSettings audioSettings =
                Resources.Load<RetroAudioSettings>("Audio/RetroAudioSettings");
            if (audioSettings == null || !audioSettings.worldMusicEnabled)
            {
                return;
            }

            GetInstance().EnterScene(SceneManager.GetActiveScene().name);
        }

        // Called as a scene change starts, while the scene being left is still audible.
        public static void FadeOutForSceneChange()
        {
            if (instance != null)
            {
                instance.targetLevel = 0f;
            }
        }

        private static WorldMusicPlayer GetInstance()
        {
            if (instance == null)
            {
                instance = new GameObject(nameof(WorldMusicPlayer))
                    .AddComponent<WorldMusicPlayer>();
            }

            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            settings = Resources.Load<RetroAudioSettings>("Audio/RetroAudioSettings");

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            // A duplicate instance destroys itself in Awake and still runs this frame.
            if (source == null)
            {
                return;
            }

            // A scene load leaves a very long frame behind it; a capped step keeps the
            // fades audible instead of jumping straight to their end.
            float delta = Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep);

            float fadeInSeconds = settings != null
                ? Mathf.Max(0f, settings.worldMusicFadeInSeconds)
                : 1.25f;
            float fadeOutSeconds = settings != null
                ? Mathf.Max(0f, settings.worldMusicFadeOutSeconds)
                : 0.6f;

            if (level < targetLevel)
            {
                level = fadeInSeconds <= 0f
                    ? targetLevel
                    : Mathf.Min(targetLevel, level + delta / fadeInSeconds);
            }
            else if (level > targetLevel)
            {
                level = fadeOutSeconds <= 0f
                    ? targetLevel
                    : Mathf.Max(targetLevel, level - delta / fadeOutSeconds);
            }

            // Re-read live: both the asset and the player's own volume can move while
            // the theme is sounding.
            float peak = (settings != null ? Mathf.Clamp01(settings.worldMusicVolume) : 0.3f) *
                AudioVolumeSettings.Music;
            source.volume = level * peak;

            if (level <= 0.001f)
            {
                if (hasPending)
                {
                    // The wait starts once the previous theme is really silent, the login
                    // theme of another player included, so the two never overlap.
                    if (pendingClip != null && LoginMusicPlayer.IsSounding)
                    {
                        enterTimer = 0f;
                        return;
                    }

                    enterTimer += delta;
                    float enterDelay = settings != null
                        ? Mathf.Max(0f, settings.worldMusicEnterDelaySeconds)
                        : 2f;
                    if (enterTimer >= enterDelay)
                    {
                        ApplyPending();
                    }
                }
                else if (source.isPlaying)
                {
                    source.Stop();
                }

                return;
            }

            if (targetLevel <= 0f || source.clip == null || source.loop)
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

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive)
            {
                return;
            }

            EnterScene(scene.name);
        }

        // The theme always leaves and comes back: the fade out runs first, then the
        // silence of worldMusicEnterDelaySeconds, then the fade in of the new scene.
        private void EnterScene(string sceneName)
        {
            ResolveScene(sceneName, out pendingClip, out pendingLoopStart, out pendingLoopEnd);
            hasPending = true;
            enterTimer = 0f;
            targetLevel = 0f;
        }

        private void ResolveScene(
            string sceneName,
            out AudioClip clip,
            out float start,
            out float end)
        {
            clip = null;
            start = 0f;
            end = 0f;

            if (settings == null ||
                !settings.worldMusicEnabled ||
                LoginMusicPlayer.IsLoginScene())
            {
                return;
            }

            SceneMusicEntry entry = settings.FindSceneMusic(sceneName);
            if (entry != null)
            {
                clip = entry.clip;
                start = entry.loopStartSeconds;
                end = entry.loopEndSeconds;
                return;
            }

            clip = settings.worldMusicClip;
            start = settings.worldMusicLoopStartSeconds;
            end = settings.worldMusicLoopEndSeconds;
        }

        private void ApplyPending()
        {
            hasPending = false;
            enterTimer = 0f;

            AudioClip clip = pendingClip;
            pendingClip = null;

            if (clip == null)
            {
                source.Stop();
                source.clip = null;
                return;
            }

            loopStart = Mathf.Clamp(pendingLoopStart, 0f, Mathf.Max(0f, clip.length - 1f));
            loopEnd = pendingLoopEnd > loopStart + 1f
                ? Mathf.Min(pendingLoopEnd, clip.length)
                : clip.length;

            source.clip = clip;
            // Unity's own loop is sample perfect; the manual one only exists to cut the
            // silence a theme can carry after its fade out.
            source.loop = loopEnd >= clip.length - 0.01f && loopStart <= 0.01f;
            source.volume = 0f;
            Rewind();
            targetLevel = 1f;
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
    }
}
