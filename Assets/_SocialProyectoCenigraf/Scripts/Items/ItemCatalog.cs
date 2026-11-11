using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Items
{
    /// <summary>Familia a la que pertenece un ítem.</summary>
    public enum ItemCategory
    {
        Misc = 0,
        Coin = 1,
        Potion = 2,
        Mana = 3,
        Quest = 4,
        /// <summary>Se recoge y se vende o se usa para fabricar. No se consume.</summary>
        Material = 5
    }

    [Serializable]
    public sealed class ItemDefinition
    {
        public string id;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public Sprite icon;
        public bool stackable;
        [Min(1)] public int maxStack = 99;

        [Header("Clasificación")]
        public ItemCategory category = ItemCategory.Misc;
        [Tooltip("Orden dentro de su categoría: 1 la más baja (bronce, básica, soplo).")]
        [Min(1)] public int tier = 1;

        [Header("Valor")]
        [Tooltip("Cuánto vale en pepitas de oro, la unidad del juego. La esmeralda vale 125.")]
        [Min(0)] public int nuggetValue;

        [Header("Consumible")]
        public bool consumable;
        [Tooltip("Cuánto repone al usarse. Sin efecto todavía: el juego no tiene barras de salud ni de maná.")]
        [Min(0)] public int restoreAmount;
    }

    [CreateAssetMenu(menuName = "Cenigraf/Items/Item Catalog", fileName = "ItemCatalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] items;
        public ItemDefinition[] Items => items;

        /// <summary>Ítems de una categoría, de menor a mayor rango.</summary>
        public System.Collections.Generic.List<ItemDefinition> OfCategory(ItemCategory category)
        {
            var result = new System.Collections.Generic.List<ItemDefinition>();

            if (items == null)
            {
                return result;
            }

            foreach (ItemDefinition item in items)
            {
                if (item != null && item.category == category)
                {
                    result.Add(item);
                }
            }

            result.Sort((a, b) => a.tier.CompareTo(b.tier));
            return result;
        }

        public bool TryGet(string id, out ItemDefinition result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(id) || items == null) return false;

            foreach (ItemDefinition item in items)
            {
                if (item == null || !string.Equals(item.id?.Trim(), id.Trim(), StringComparison.Ordinal))
                    continue;
                if (result != null)
                {
                    result = null;
                    return false;
                }
                result = item;
            }

            return result != null;
        }
    }
}
