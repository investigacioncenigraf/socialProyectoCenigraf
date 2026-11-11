using System.Collections.Generic;

namespace SocialProyectoCenigraf.Items
{
    /// <summary>
    /// The game is counted in gold nuggets. The emerald is not a coin: it is the stone the peoples
    /// of the Sierra valued above everything, and here it stands for the highest amount.
    /// </summary>
    public static class CurrencyExchange
    {
        /// <summary>What an emerald is worth in nuggets, when the catalog does not say otherwise.</summary>
        public const int EmeraldInNuggets = 125;

        private static int ValueOf(ItemDefinition coin)
        {
            if (coin == null)
            {
                return 0;
            }

            if (coin.nuggetValue > 0)
            {
                return coin.nuggetValue;
            }

            return coin.tier <= 1 ? 1 : EmeraldInNuggets;
        }

        /// <summary>Everything the player carries, counted in nuggets.</summary>
        public static int TotalNuggets(ItemCatalog catalog)
        {
            if (catalog == null)
            {
                return 0;
            }

            int total = 0;

            foreach (ItemDefinition coin in catalog.OfCategory(ItemCategory.Coin))
            {
                total += PlayerItemInventory.GetQuantity(coin.id) * ValueOf(coin);
            }

            return total;
        }

        /// <summary>
        /// Splits an amount of nuggets into the fewest pieces, emeralds first. The key is the item
        /// id and the value how many of them.
        /// </summary>
        public static Dictionary<string, int> Split(ItemCatalog catalog, int nuggets)
        {
            var result = new Dictionary<string, int>();

            if (catalog == null || nuggets <= 0)
            {
                return result;
            }

            List<ItemDefinition> coins = catalog.OfCategory(ItemCategory.Coin);
            int remaining = nuggets;

            for (int i = coins.Count - 1; i >= 0; i--)
            {
                ItemDefinition coin = coins[i];
                int value = ValueOf(coin);

                if (value <= 0 || remaining < value)
                {
                    continue;
                }

                int count = remaining / value;
                remaining -= count * value;
                result[coin.id] = count;
            }

            return result;
        }
    }
}
