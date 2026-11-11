using System;
using System.Collections.Generic;
using UnityEngine;

namespace SocialProyectoCenigraf.Items
{
    /// <summary>Inventario de la partida actual, conservado entre escenas.</summary>
    public static class PlayerItemInventory
    {
        private static readonly Dictionary<string, int> quantities = new Dictionary<string, int>(StringComparer.Ordinal);

        public static event Action<ItemDefinition, int> ItemAdded;
        public static event Action<ItemDefinition, int> ItemRemoved;
        public static IReadOnlyDictionary<string, int> Items => quantities;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            quantities.Clear();
            ItemAdded = null;
            ItemRemoved = null;
        }

        public static bool HasItem(string id) => GetQuantity(id) > 0;

        public static int GetQuantity(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && quantities.TryGetValue(id.Trim(), out int quantity)
                ? quantity
                : 0;
        }

        public static bool TryGrant(ItemCatalog catalog, string id, int amount = 1)
        {
            if (catalog == null || !catalog.TryGet(id, out ItemDefinition item))
            {
                Debug.LogWarning($"Ítem '{id}' no encontrado o duplicado en el catálogo.");
                return false;
            }
            if (amount < 1) return false;

            int current = GetQuantity(item.id);
            int limit = item.stackable ? Mathf.Max(1, item.maxStack) : 1;
            int next = Mathf.Min(limit, current + amount);
            if (next == current) return false;

            quantities[item.id.Trim()] = next;
            ItemAdded?.Invoke(item, next);
            return true;
        }

        /// <summary>Gasta o consume unidades de un ítem. Falla si no hay suficientes.</summary>
        public static bool TryConsume(ItemCatalog catalog, string id, int amount = 1)
        {
            if (catalog == null || !catalog.TryGet(id, out ItemDefinition item) || amount < 1)
            {
                return false;
            }

            string key = item.id.Trim();
            int current = GetQuantity(key);

            if (current < amount)
            {
                return false;
            }

            int next = current - amount;

            if (next > 0)
            {
                quantities[key] = next;
            }
            else
            {
                quantities.Remove(key);
            }

            ItemRemoved?.Invoke(item, next);
            return true;
        }
    }
}
