using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Player.Visual
{
    [CreateAssetMenu(
        fileName = "PlayerSkinCatalog",
        menuName = "Cenigraf/Player/Skin Catalog")]
    public sealed class PlayerSkinCatalog : ScriptableObject
    {
        [SerializeField] private PlayerSkinDefinition[] skins =
            Array.Empty<PlayerSkinDefinition>();

        public bool TryGetSkin(
            string skinId,
            out PlayerSkinDefinition definition)
        {
            string normalizedId = string.IsNullOrWhiteSpace(skinId)
                ? State.PlayerStateData.DefaultSkinId
                : skinId.Trim();

            foreach (PlayerSkinDefinition candidate in skins)
            {
                if (candidate != null &&
                    string.Equals(
                        candidate.SkinId,
                        normalizedId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }
    }
}
