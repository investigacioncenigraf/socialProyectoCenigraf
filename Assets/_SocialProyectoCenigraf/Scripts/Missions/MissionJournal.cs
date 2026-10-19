using System;
using System.Collections.Generic;
using UnityEngine;

namespace SocialProyectoCenigraf.Missions
{
    /// <summary>Misiones activadas durante la partida. Conserva su estado al cambiar de escena.</summary>
    public static class MissionJournal
    {
        private static readonly Dictionary<string, MissionDefinition> active = new Dictionary<string, MissionDefinition>();
        public static IReadOnlyCollection<MissionDefinition> ActiveMissions => active.Values;
        public static event Action<MissionDefinition> MissionActivated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            active.Clear();
            MissionActivated = null;
        }
        public static bool IsActive(string id) => !string.IsNullOrEmpty(id) && active.ContainsKey(id);
        public static bool TryActivate(MissionCatalog catalog, string id)
        {
            if (catalog == null || !catalog.TryGet(id, out var mission))
            {
                Debug.LogWarning($"Misión '{id}' no encontrada o duplicada en el catálogo.");
                return false;
            }
            if (active.ContainsKey(mission.id)) return false;
            active.Add(mission.id, mission);
            Debug.Log($"Misión activada: {mission.title} ({mission.id}) — {mission.objective}");
            MissionActivated?.Invoke(mission);
            return true;
        }
    }
}
