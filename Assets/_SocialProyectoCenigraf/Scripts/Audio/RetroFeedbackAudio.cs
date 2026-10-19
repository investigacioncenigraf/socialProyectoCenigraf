using System;
using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.Audio
{
    // Separate voices prevent text from cutting off the interaction confirmation.
    public sealed class RetroFeedbackAudio : MonoBehaviour
    {
        private static RetroFeedbackAudio instance;
        private AudioSource letterSource, interactionSource, rewardSource, hoverSource;
        private AudioClip letterClip, interactionClip, rewardClip, hoverClip;
        private RetroAudioSettings settings;
        private Selectable[] selectableBuffer = new Selectable[32];
        private float lastHoverTime = float.NegativeInfinity;
        private int lastLetterFrame = -1, lastInteractionFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeButtons() => GetInstance();

        private static RetroFeedbackAudio GetInstance()
        {
            if (instance == null)
                instance = new GameObject("RetroFeedbackAudio").AddComponent<RetroFeedbackAudio>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            settings = Resources.Load<RetroAudioSettings>("Audio/RetroAudioSettings");
            letterSource = CreateSource();
            interactionSource = CreateSource();
            rewardSource = CreateSource();
            hoverSource = CreateSource();
            letterClip = CreateTone("LetterBit", 620f, 540f, 0.022f);
            interactionClip = CreateTone("InteractionBit", 740f, 980f, 0.075f);
            rewardClip = CreateRewardMelody();
            hoverClip = CreateTone("ButtonHoverBit", 460f, 580f, 0.035f);
        }

        private void LateUpdate()
        {
            // Includes buttons created at runtime, without allocating an array every frame.
            int count = Selectable.allSelectableCount;
            if (selectableBuffer.Length < count) Array.Resize(ref selectableBuffer, count + 16);
            count = Selectable.AllSelectablesNoAlloc(selectableBuffer);
            for (int i = 0; i < count; i++)
                if (selectableBuffer[i] is Button button && button.GetComponent<RetroButtonAudio>() == null)
                    button.gameObject.AddComponent<RetroButtonAudio>();
            Array.Clear(selectableBuffer, 0, selectableBuffer.Length);
        }

        public static void PlayButtonHover()
        {
            var audio = GetInstance();
            if (Time.unscaledTime - audio.lastHoverTime < 0.06f) return;
            audio.lastHoverTime = Time.unscaledTime;
            audio.hoverSource.clip = audio.hoverClip;
            audio.hoverSource.volume = audio.settings != null ? audio.settings.hoverVolume : 0.07f;
            audio.hoverSource.Play();
        }

        private AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            return source;
        }

        public static void PlayLetter()
        {
            var audio = GetInstance();
            // A slow frame can reveal several letters; avoid stacking loud bursts.
            if (audio.lastLetterFrame == Time.frameCount) return;
            audio.lastLetterFrame = Time.frameCount;
            audio.letterSource.clip = audio.letterClip;
            audio.letterSource.volume = audio.settings != null ? audio.settings.letterVolume : 0.08f;
            audio.letterSource.Play();
        }

        public static void PlayInteraction()
        {
            var audio = GetInstance();
            if (audio.lastInteractionFrame == Time.frameCount) return;
            audio.lastInteractionFrame = Time.frameCount;
            audio.interactionSource.clip = audio.interactionClip;
            audio.interactionSource.volume = audio.settings != null ? audio.settings.interactionVolume : 0.14f;
            audio.interactionSource.Play();
        }

        public static void PlayReward()
        {
            var audio = GetInstance();
            audio.letterSource.Stop();
            audio.interactionSource.Stop();
            audio.rewardSource.clip = audio.rewardClip;
            audio.rewardSource.volume = audio.settings != null ? audio.settings.rewardVolume : 0.16f;
            audio.rewardSource.Play();
        }

        private static AudioClip CreateRewardMelody()
        {
            // Original rising phrase with a longer final note, using the same soft timbre.
            float[] notes = { 523.25f, 659.25f, 783.99f, 1174.66f, 1046.50f };
            const int sampleRate = 44100;
            const float step = 0.085f;
            var samples = new float[Mathf.CeilToInt(sampleRate * 0.60f)];
            for (int note = 0; note < notes.Length; note++)
            {
                float duration = note == notes.Length - 1 ? 0.25f : 0.075f;
                int offset = Mathf.RoundToInt(note * step * sampleRate);
                var tone = CreateTone("RewardNote", notes[note], notes[note], duration);
                var data = new float[tone.samples];
                tone.GetData(data, 0);
                Array.Copy(data, 0, samples, offset, data.Length);
                Destroy(tone);
            }
            var clip = AudioClip.Create("RewardAcquired", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateTone(string name, float startHz, float endHz, float duration)
        {
            const int sampleRate = 44100;
            var samples = new float[Mathf.CeilToInt(sampleRate * duration)];
            double phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                phase += 2 * Math.PI * Mathf.Lerp(startHz, endHz, progress) / sampleRate;
                // Soft square-like timbre, with attack/release to prevent sharp clicks.
                double tone = Math.Sin(phase) + 0.22 * Math.Sin(3 * phase) + 0.08 * Math.Sin(5 * phase);
                float envelope = Mathf.Min(1f, t / 0.003f) * Mathf.Pow(1f - progress, 1.5f);
                samples[i] = (float)tone * 0.35f * envelope;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
            if (letterClip != null) Destroy(letterClip);
            if (interactionClip != null) Destroy(interactionClip);
            if (rewardClip != null) Destroy(rewardClip);
            if (hoverClip != null) Destroy(hoverClip);
        }
    }
}
