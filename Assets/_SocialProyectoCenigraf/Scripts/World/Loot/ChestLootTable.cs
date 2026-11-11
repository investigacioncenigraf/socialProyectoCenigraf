using System;
using SocialProyectoCenigraf.Items;
using UnityEngine;

namespace SocialProyectoCenigraf.World.Loot
{
    /// <summary>What a chest of a given level holds.</summary>
    [Serializable]
    public struct ChestLevelLoot
    {
        [Tooltip("Cuántos materiales entrega. 0 deja el cofre vacío de objetos.")]
        [Min(0)] public int minItems;
        [Min(0)] public int maxItems;

        [Header("Peso de cada rareza")]
        [Min(0)] public int commonWeight;
        [Min(0)] public int uncommonWeight;
        [Min(0)] public int rareWeight;
        [Min(0)] public int epicWeight;

        [Header("Riqueza, contada en pepitas de oro")]
        [Tooltip("Se entrega en las menos piezas posibles: 125 pepitas se pagan con una esmeralda.")]
        [Min(0)] public int minNuggets;
        [Min(0)] public int maxNuggets;
    }

    [CreateAssetMenu(
        fileName = "ChestLootTable",
        menuName = "Cenigraf/Loot/Chest Loot Table",
        order = 0)]
    public sealed class ChestLootTable : ScriptableObject
    {
        [SerializeField] private ItemCatalog catalog;
        [Tooltip("Un nivel por índice, del 0 al 9.")]
        [SerializeField] private ChestLevelLoot[] levels = new ChestLevelLoot[10];

        public ItemCatalog Catalog => catalog;
        public int LevelCount => levels != null ? levels.Length : 0;

        public ChestLevelLoot GetLevel(int level)
        {
            if (levels == null || levels.Length == 0)
            {
                return default;
            }

            return levels[Mathf.Clamp(level, 0, levels.Length - 1)];
        }
    }
}
