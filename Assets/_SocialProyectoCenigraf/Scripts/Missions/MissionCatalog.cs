using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Missions
{
    [Serializable]
    public sealed class MissionDefinition
    {
        [Tooltip("ID único de la misión durante la partida, incluso entre catálogos.")]
        public string id;
        public string title;
        [TextArea(2, 6)] public string description;
        [TextArea(1, 4)] public string objective;
    }

    [CreateAssetMenu(menuName = "Cenigraf/Mission Catalog", fileName = "MissionCatalog")]
    public sealed class MissionCatalog : ScriptableObject
    {
        [SerializeField] private MissionDefinition[] missions;
        public MissionDefinition[] Missions => missions;
        public bool TryGet(string id, out MissionDefinition result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(id) || missions == null) return false;
            foreach (var entry in missions)
            {
                if (entry == null || entry.id != id) continue;
                if (result != null) { result = null; return false; }
                result = entry;
            }
            return result != null;
        }
    }
}
