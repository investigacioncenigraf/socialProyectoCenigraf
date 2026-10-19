using UnityEngine;

namespace SocialProyectoCenigraf.Audio
{
    [CreateAssetMenu(menuName = "Social Proyecto Cenigraf/Audio/Retro Audio Settings")]
    public sealed class RetroAudioSettings : ScriptableObject
    {
        [Header("Volumen de efectos retro")]
        [Range(0f, 1f)] public float hoverVolume = 0.07f;
        [Range(0f, 1f)] public float letterVolume = 0.08f;
        [Range(0f, 1f)] public float interactionVolume = 0.14f;
        [Range(0f, 1f)] public float rewardVolume = 0.16f;
    }
}
