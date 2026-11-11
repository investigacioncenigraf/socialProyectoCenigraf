using System;
using System.Collections.Generic;
using UnityEngine;

namespace SocialProyectoCenigraf.Missions
{
    /// <summary>Misiones activadas durante la partida. Conserva su estado al cambiar de escena.</summary>
    public static class MissionJournal
    {
        private static readonly Dictionary<string, MissionDefinition> active = new Dictionary<string, MissionDefinition>();
        private static readonly HashSet<string> completed = new HashSet<string>(StringComparer.Ordinal);
        public static IReadOnlyCollection<MissionDefinition> ActiveMissions => active.Values;
        public static event Action<MissionDefinition> MissionActivated;
        public static event Action<MissionDefinition> MissionFinished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            active.Clear();
            completed.Clear();
            MissionActivated = null;
            MissionFinished = null;
        }
        public static bool IsActive(string id) => !string.IsNullOrWhiteSpace(id) && active.ContainsKey(id.Trim());
        public static bool IsCompleted(string id) => !string.IsNullOrEmpty(id) && completed.Contains(id.Trim());

        public static bool Complete(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !active.TryGetValue(id.Trim(), out MissionDefinition mission))
                return false;

            active.Remove(mission.id.Trim());
            completed.Add(mission.id.Trim());
            MissionFinished?.Invoke(mission);
            Debug.Log($"Misión completada: {mission.title} ({mission.id})");
            return true;
        }

        public static bool TryActivate(MissionCatalog catalog, string id)
        {
            if (catalog == null || !catalog.TryGet(id, out var mission))
            {
                Debug.LogWarning($"Misión '{id}' no encontrada o duplicada en el catálogo.");
                return false;
            }
            if (completed.Contains(mission.id.Trim())) return false;
            if (active.ContainsKey(mission.id.Trim())) return false;
            active.Add(mission.id.Trim(), mission);
            Debug.Log($"Misión activada: {mission.title} ({mission.id}) — {mission.objective}");
            MissionActivated?.Invoke(mission);
            return true;
        }
    }
}
