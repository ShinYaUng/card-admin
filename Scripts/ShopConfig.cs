using UnityEngine;
using NueGames.NueDeck.Scripts.Enums;

namespace NueGames.NueDeck.Scripts.UI.Shop
{
    [CreateAssetMenu(fileName = "Shop Config", menuName = "NueDeck/Shop/Config")]
    public class ShopConfig : ScriptableObject
    {
        [Header("Offers")]
        public int offerCount = 3;
        public int rerollCost = 15;

        [Header("Prices by Rarity")]
        public int commonPrice = 25;
        public int rarePrice = 60;
        public int legendaryPrice = 120;

        public int GetPrice(RarityType r)
        {
            switch (r)
            {
                case RarityType.Common: return commonPrice;
                case RarityType.Rare: return rarePrice;
                case RarityType.Legendary: return legendaryPrice;
                default: return commonPrice;
            }
        }
    }
}
