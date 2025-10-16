// Scripts/Net/CardCatalogService.cs
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Enums;

namespace NueGames.NueDeck.Scripts.Net
{
    public class CardCatalogService : MonoBehaviour
    {
        public static CardCatalogService Instance { get; private set; }

        [SerializeField] private string catalogUrl = "https://card-admin-api.onrender.com/cards"; // GET -> RemoteCardList (JSON)

        private readonly Dictionary<string, CardData> _runtime = new();
        private bool _refreshing;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public IEnumerable<CardData> GetAllRuntimeCards() => _runtime.Values;

        public async Task Refresh()
        {
            if (_refreshing || string.IsNullOrEmpty(catalogUrl)) return;
            _refreshing = true;
            try
            {
                using var req = UnityWebRequest.Get(catalogUrl);
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

#if UNITY_2020_3_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success) return;
#else
                if (req.isNetworkError || req.isHttpError) return;
#endif
                var json = req.downloadHandler.text;
                var list = JsonUtility.FromJson<RemoteCardList>(json) ?? new RemoteCardList();

                foreach (var dto in list.cards)
                {
                    if (_runtime.ContainsKey(dto.id)) continue;

                    var cd = CreateCardDataFromDto(dto);
                    if (cd != null) _runtime[dto.id] = cd;
                }
            }
            finally { _refreshing = false; }
        }

        // ======= Factory: DTO -> CardData (runtime) =======
        private static void SetPrivateField<T>(CardData cd, string field, T val)
        {
            typeof(CardData).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(cd, val);
        }

        private static RarityType ParseRarity(string s) =>
            System.Enum.TryParse<RarityType>(s, true, out var r) ? r : RarityType.Common;

        private static CardActionType ParseActionType(string s) =>
            System.Enum.TryParse<CardActionType>(s, true, out var t) ? t : CardActionType.Attack;

        private static ActionTargetType ParseTarget(string s) =>
            System.Enum.TryParse<ActionTargetType>(s, true, out var t) ? t : ActionTargetType.Enemy;

        private static StatusType ParseStatus(string s) =>
            System.Enum.TryParse<StatusType>(s, true, out var st) ? st : StatusType.Strength;

        private static CardData CreateCardDataFromDto(RemoteCardDto dto)
        {
            var cd = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(cd, "id", dto.id);
            SetPrivateField(cd, "cardName", dto.cardName);
            SetPrivateField(cd, "manaCost", dto.manaCost);
            SetPrivateField(cd, "rarity", ParseRarity(dto.rarity));
            SetPrivateField(cd, "usableWithoutTarget", dto.usableWithoutTarget);
            SetPrivateField(cd, "exhaustAfterPlay", dto.exhaustAfterPlay);

            // spriteUrl: ถ้ายังไม่ต้องโหลดภาพ runtime ให้ปล่อยว่างไปก่อนได้
            SetPrivateField<Sprite>(cd, "cardSprite", null);

            // Actions
            var actions = new List<CardActionData>();
            foreach (var a in dto.actions)
            {
                var ad = new CardActionData();
#if UNITY_EDITOR
                ad.EditActionType(ParseActionType(a.type));
                ad.EditActionTarget(ParseTarget(a.target));
                ad.EditActionValue(a.value);
                ad.EditActionDelay(a.delay);
#else
                typeof(CardActionData).GetField("cardActionType", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ad, ParseActionType(a.type));
                typeof(CardActionData).GetField("actionTargetType", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ad, ParseTarget(a.target));
                typeof(CardActionData).GetField("actionValue", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ad, a.value);
                typeof(CardActionData).GetField("actionDelay", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ad, a.delay);
#endif
                actions.Add(ad);
            }
            SetPrivateField(cd, "cardActionDataList", actions);

            // Descriptions
            var descs = new List<CardDescriptionData>();
            foreach (var d in dto.desc)
            {
                var dd = new CardDescriptionData();
                typeof(CardDescriptionData).GetField("descriptionText", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dd, d.text);
                typeof(CardDescriptionData).GetField("useModifier", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dd, d.useModifier);
                typeof(CardDescriptionData).GetField("modifiedActionValueIndex", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dd, d.actionIndex);
                typeof(CardDescriptionData).GetField("modiferStats", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dd, ParseStatus(d.modStat));
                descs.Add(dd);
            }
            SetPrivateField(cd, "cardDescriptionDataList", descs);

            // เติมคำอธิบายที่ประกอบค่าจาก Action/Status (+Strength/+RunAttack) ตามระบบเดิมในโปรเจกต์
            cd.UpdateDescription(); // ใช้เมธอดของ CardData เดิมในโปรเจกต์

            return cd;
        }
    }
}
