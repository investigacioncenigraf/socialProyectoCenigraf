using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Audio
{
    /// <summary>
    /// Player volume for music and for sound effects, as a multiplier over the mix the
    /// project already defines in RetroAudioSettings. Saved with the player, so it
    /// survives scenes and sessions, and read live by every voice.
    /// </summary>
    public static class AudioVolumeSettings
    {
        private const string MusicKey = "audio.music";
        private const string EffectsKey = "audio.effects";

        // Where the sliders of the game menu sit before the player touches them.
        private const float DefaultMusic = 0.5f;
        private const float DefaultEffects = 0.75f;

        private static float music = DefaultMusic;
        private static float effects = DefaultEffects;
        private static bool loaded;

        /// <summary>Raised whenever either volume changes, for UI that shows the value.</summary>
        public static event Action Changed;

        public static float Music
        {
            get
            {
                Load();
                return music;
            }
            set => Apply(ref music, MusicKey, value);
        }

        public static float Effects
        {
            get
            {
                Load();
                return effects;
            }
            set => Apply(ref effects, EffectsKey, value);
        }

        /// <summary>Writes the values to disk. Called when the audio page is left, so a
        /// crash or a hard stop cannot lose what the player just set.</summary>
        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // The saved values stay; only the cache and the listeners of the past run go.
            loaded = false;
            music = DefaultMusic;
            effects = DefaultEffects;
            Changed = null;
        }

        private static void Load()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, DefaultMusic));
            effects = Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey, DefaultEffects));
        }

        private static void Apply(ref float field, string key, float value)
        {
            Load();

            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(field, clamped))
            {
                return;
            }

            field = clamped;
            PlayerPrefs.SetFloat(key, clamped);
            Changed?.Invoke();
        }
    }
}
