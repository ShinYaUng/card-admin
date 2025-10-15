using UnityEngine;
using NueGames.NueDeck.Scripts.Enums;

namespace NueGames.NueDeck.Scripts.Data.Collection
{
    [CreateAssetMenu(fileName = "Relic", menuName = "NueDeck/Collection/Relic", order = 0)]
    public class RelicData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string relicName;
        [SerializeField] private Sprite icon;
        [TextArea][SerializeField] private string description;
        [SerializeField] private RelicType relicType;

        public string Id => id;
        public string RelicName => relicName;
        public Sprite Icon => icon;
        public string Description => description;
        public RelicType RelicType => relicType;
    }
}
