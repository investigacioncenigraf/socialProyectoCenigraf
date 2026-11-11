using System;
using System.Collections.Generic;
using SocialProyectoCenigraf.World.NPC;
using SocialProyectoCenigraf.World.Rendering;
using TMPro;
using UnityEngine;

namespace SocialProyectoCenigraf.World
{
    /// <summary>
    /// Maze bifurcation: while the local player is inside the interaction zone, the question
    /// (at Sign/Reference), its subtitle (at Sign/Subtitle) and each answer (at WayX/Reference)
    /// are shown in the scene, and they fade out when the player leaves.
    /// A short time after entering, the direction arrows (WayX/Arrow) appear and blink to draw attention.
    /// Choosing a path is done by walking into it: each way's ShadowArea reveals itself.
    /// Nothing here rewards or punishes the player; the maze layout decides.
    /// Question and answers are hardcoded in the inspector for now (a catalog will fill them later).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Cenigraf/World/Way Reveal Question")]
    public sealed class WayRevealQuestion : MonoBehaviour
    {
        public const int MaxWays = 3;

        [Serializable]
        public struct Way
        {
            [Tooltip("WayA / WayB / WayC root. It is deactivated when it is beyond Active Ways.")]
            public GameObject root;
            [Tooltip("Text placed at this way's Reference object.")]
            public TMP_Text answerText;
            [Tooltip("Arrow (SpriteRenderer) that points to this way.")]
            public SpriteRenderer arrow;
        }

        [Header("Question (hardcoded for now)")]
        [SerializeField] private string question = "¿Cuánto es 2×2?";
        [Tooltip("One entry per way, in order: WayA, WayB, WayC.")]
        [SerializeField] private string[] answers = { "22", "4", "" };

        [Header("Ways")]
        [Tooltip("How many ways this bifurcation uses (the rest are hidden).")]
        [SerializeField, Range(1, MaxWays)] private int activeWays = 2;
        [SerializeField] private Way[] ways = new Way[MaxWays];

        [Header("References")]
        [Tooltip("Interaction area (same NPCInteractionZone used by the NPC prefab).")]
        [SerializeField] private NPCInteractionZone interactionZone;
        [Tooltip("Text placed at Sign/Reference.")]
        [SerializeField] private TMP_Text questionText;
        [Tooltip("Text placed at Sign/Subtitle. Its text is edited on the TMP component itself.")]
        [SerializeField] private TMP_Text subtitleText;

        [Header("Text outline (all texts)")]
        [SerializeField] private Color outlineColor = Color.black;
        [SerializeField, Range(0f, 1f)] private float outlineWidth = 0.25f;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float textFadeSeconds = 0.2f;

        [Header("Arrows")]
        [Tooltip("Seconds after entering the area before the arrows appear.")]
        [SerializeField, Min(0f)] private float arrowAppearDelaySeconds = 0.5f;
        [Tooltip("Seconds for one full blink cycle (bright -> dim -> bright). Smaller = faster.")]
        [SerializeField, Min(0.05f)] private float arrowBlinkPeriodSeconds = 0.8f;
        [Tooltip("Opacity at the dimmest point of the blink. 0 = fully off, 1 = no blinking.")]
        [SerializeField, Range(0f, 1f)] private float arrowBlinkMinAlpha = 0.15f;

        // Each text/arrow keeps the opacity it has in the prefab (e.g. a translucent subtitle).
        private readonly Dictionary<TMP_Text, float> baseAlphas = new();
        private readonly Dictionary<SpriteRenderer, float> arrowBaseAlphas = new();
        private float textAlpha;
        private float timeInside;      // seconds the player has been inside the area
        private float arrowEnvelope;   // 0..1 appear/disappear fade of the arrows
        private readonly ShadowArea[] wayShadows = new ShadowArea[MaxWays]; // one per way, found at runtime

        public int ActiveWays => activeWays;

        /// <summary>For the future question catalog.</summary>
        public void Configure(string newQuestion, string[] newAnswers, int newActiveWays)
        {
            question = newQuestion;
            answers = newAnswers;
            activeWays = Mathf.Clamp(newActiveWays, 1, MaxWays);
            Refresh(Application.isPlaying ? textAlpha : 1f, Application.isPlaying ? 0f : 1f);
        }

        private void Awake()
        {
            FindReferences();
        }

        private void OnEnable()
        {
            FindReferences();
            textAlpha = Application.isPlaying ? 0f : 1f;
            timeInside = 0f;
            arrowEnvelope = Application.isPlaying ? 0f : 1f;
            Refresh(textAlpha, arrowEnvelope);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            bool show = interactionZone != null && interactionZone.HasPlayerInside;
            float target = show ? 1f : 0f;
            textAlpha = textFadeSeconds <= Mathf.Epsilon
                ? target
                : Mathf.MoveTowards(textAlpha, target, Time.deltaTime / textFadeSeconds);
            ApplyAlpha(textAlpha);

            // Arrows: appear after the configured delay, then blink while the player stays inside.
            timeInside = show ? timeInside + Time.deltaTime : 0f;
            bool arrowsOn = show && timeInside >= arrowAppearDelaySeconds;
            float envTarget = arrowsOn ? 1f : 0f;
            arrowEnvelope = textFadeSeconds <= Mathf.Epsilon
                ? envTarget
                : Mathf.MoveTowards(arrowEnvelope, envTarget, Time.deltaTime / textFadeSeconds);

            float blink = 1f;
            if (arrowsOn)
            {
                float t = timeInside - arrowAppearDelaySeconds;
                float wave = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * t / arrowBlinkPeriodSeconds); // starts bright
                blink = Mathf.Lerp(arrowBlinkMinAlpha, 1f, wave);
            }
            ApplyArrowAlpha(arrowEnvelope * blink);
        }

        private void FindReferences()
        {
            if (interactionZone == null)
                interactionZone = GetComponentInChildren<NPCInteractionZone>(true);

            for (int i = 0; i < wayShadows.Length; i++)
            {
                GameObject root = ways != null && i < ways.Length ? ways[i].root : null;
                wayShadows[i] = root != null ? root.GetComponentInChildren<ShadowArea>(true) : null;
            }
        }

        /// <summary>Applies texts, outline, how many ways are active, and the given opacities.</summary>
        private void Refresh(float alpha, float arrowAlpha)
        {
            SetText(questionText, question);

            for (int i = 0; i < ways.Length; i++)
            {
                bool active = i < activeWays;
                if (ways[i].root != null && ways[i].root.activeSelf != active)
                    ways[i].root.SetActive(active);

                string answer = answers != null && i < answers.Length ? answers[i] : string.Empty;
                SetText(ways[i].answerText, answer);
            }

            ApplyOutline(questionText);
            ApplyOutline(subtitleText);
            for (int i = 0; i < ways.Length; i++)
                ApplyOutline(ways[i].answerText);

            ApplyAlpha(alpha);
            ApplyArrowAlpha(arrowAlpha);
        }

        private void ApplyAlpha(float alpha)
        {
            ApplyAlpha(questionText, alpha);
            ApplyAlpha(subtitleText, alpha);
            for (int i = 0; i < ways.Length; i++)
                ApplyAlpha(ways[i].answerText, alpha);
        }

        private void ApplyAlpha(TMP_Text target, float alpha)
        {
            if (target == null) return;

            if (!baseAlphas.TryGetValue(target, out float baseAlpha))
            {
                baseAlpha = target.color.a;
                baseAlphas[target] = baseAlpha;
            }

            bool visible = alpha > 0.001f;
            if (target.enabled != visible) target.enabled = visible;

            float finalAlpha = baseAlpha * alpha;
            if (visible && !Mathf.Approximately(target.alpha, finalAlpha)) target.alpha = finalAlpha;
        }

        private void ApplyArrowAlpha(float alpha)
        {
            for (int i = 0; i < ways.Length; i++)
            {
                SpriteRenderer arrow = ways[i].arrow;
                if (arrow == null) continue;

                if (!arrowBaseAlphas.TryGetValue(arrow, out float baseAlpha))
                {
                    baseAlpha = arrow.color.a;
                    arrowBaseAlphas[arrow] = baseAlpha;
                }

                // A way the player already walked into no longer needs its arrow.
                bool wayVisited = Application.isPlaying && i < wayShadows.Length &&
                                  wayShadows[i] != null && wayShadows[i].HasBeenEntered;
                float arrowAlpha = wayVisited ? 0f : alpha;

                bool visible = arrowAlpha > 0.001f;
                if (arrow.enabled != visible) arrow.enabled = visible;

                Color color = arrow.color;
                color.a = baseAlpha * arrowAlpha;
                if (visible && !Mathf.Approximately(arrow.color.a, color.a)) arrow.color = color;
            }
        }

        private void ApplyOutline(TMP_Text target)
        {
            if (target == null) return;

            // Setting the outline makes TMP reach Renderer.material, which Unity forbids
            // on a prefab asset. An asset has no scene; an open prefab stage does, and a
            // scene instance does, so both of those keep their outline.
            if (!target.gameObject.scene.IsValid()) return;

            Color32 color = outlineColor;
            if (!Mathf.Approximately(target.outlineWidth, outlineWidth)) target.outlineWidth = outlineWidth;
            if (!target.outlineColor.Equals(color)) target.outlineColor = color;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null && target.text != value) target.text = value;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            activeWays = Mathf.Clamp(activeWays, 1, MaxWays);
            if (ways == null || ways.Length != MaxWays) Array.Resize(ref ways, MaxWays);

            // Scene objects cannot be changed safely inside OnValidate: refresh on the next editor tick.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;

                // Inspecting the prefab asset itself: refreshing it there changes nothing
                // visible and only dirties the asset.
                if (!gameObject.scene.IsValid()) return;

                Refresh(Application.isPlaying ? textAlpha : 1f, Application.isPlaying ? arrowEnvelope : 1f);
            };
        }
#endif
    }
}
