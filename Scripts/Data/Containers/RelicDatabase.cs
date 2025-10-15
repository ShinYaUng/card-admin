using System.Collections.Generic;
using UnityEngine;
using NueGames.NueDeck.Scripts.Data.Collection;

namespace NueGames.NueDeck.Scripts.Data.Containers
{
    [CreateAssetMenu(fileName = "Relic Database", menuName = "NueDeck/Settings/RelicDatabase", order = 0)]
    public class RelicDatabase : ScriptableObject
    {
        [SerializeField] private List<RelicData> allRelics = new List<RelicData>();
        public List<RelicData> AllRelics => allRelics;

        public List<RelicData> GetRandomRelicList(int count)
        {
            var pool = new List<RelicData>(allRelics);
            var result = new List<RelicData>();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
