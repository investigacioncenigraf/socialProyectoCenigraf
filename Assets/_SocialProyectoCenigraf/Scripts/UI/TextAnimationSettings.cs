using System;
using UnityEngine;

namespace SocialProyectoCenigraf.UI
{
    /// <summary>How the game animates dialogue text, as the player chooses it.</summary>
    public enum TextAnimationStyle
    {
        Default = 0,
        Retro = 1
    }

    /// <summary>
    /// The text animation the player picked in the interface page of the game menu. It is
    /// saved with the player and it rules every scene without exception: the dialogue
    /// mode of DebugStore is kept in step with this value, nothing else decides it.
    /// </summary>
    public static class TextAnimationSettings
    {
        private const string StyleKey = "ui.textAnimation";
        private const TextAnimationStyle DefaultStyle = TextAnimationStyle.Default;

        private static TextAnimationStyle style = DefaultStyle;
        private static bool loaded;

        /// <summary>Raised when the style changes, for the UI and for whoever applies it.</summary>
        public static event Action Changed;

        public static TextAnimationStyle Style
        {
            get
            {
                Load();
                return style;
            }
            set
            {
                Load();

                TextAnimationStyle next = value == TextAnimationStyle.Retro
                    ? TextAnimationStyle.Retro
                    : TextAnimationStyle.Default;
                if (next == style)
                {
                    return;
                }

                style = next;
                PlayerPrefs.SetInt(StyleKey, (int)next);
                Changed?.Invoke();
            }
        }

        /// <summary>Label shown in the menu for a style.</summary>
        public static string LabelOf(TextAnimationStyle value) =>
            value == TextAnimationStyle.Retro ? "Retro" : "Default";

        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // The saved choice stays; only the cache and the listeners of the past run go.
            loaded = false;
            style = DefaultStyle;
            Changed = null;
        }

        private static void Load()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            style = PlayerPrefs.GetInt(StyleKey, (int)DefaultStyle) == (int)TextAnimationStyle.Retro
                ? TextAnimationStyle.Retro
                : TextAnimationStyle.Default;
        }
    }
}
