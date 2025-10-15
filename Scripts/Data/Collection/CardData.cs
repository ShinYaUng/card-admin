using System;
using System.Collections.Generic;
using System.Text;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.NueExtentions;
using UnityEngine;
using NueGames.NueDeck.Scripts.Characters;

namespace NueGames.NueDeck.Scripts.Data.Collection
{
    [CreateAssetMenu(fileName = "Card Data", menuName = "NueDeck/Collection/Card", order = 0)]
    public class CardData : ScriptableObject
    {
        [Header("Card Profile")]
        [SerializeField] private string id;
        [SerializeField] private string cardName;
        [SerializeField] private int manaCost;
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private RarityType rarity;

        [Header("Action Settings")]
        [SerializeField] private bool usableWithoutTarget;
        [SerializeField] private bool exhaustAfterPlay;
        [SerializeField] private List<CardActionData> cardActionDataList;

        [Header("Description")]
        [SerializeField] private List<CardDescriptionData> cardDescriptionDataList;
        [SerializeField] private List<SpecialKeywords> specialKeywordsList;

        [Header("Fx")]
        [SerializeField] private AudioActionType audioType;

        [Header("Upgrade")]
        [SerializeField, Range(1, 3)] private int upgradeStep = 1;   // 1..3
        [SerializeField] private CardData nextUpgrade;              // null = ระดับสุดท้าย

        #region Cache
        public string Id => id;
        public bool UsableWithoutTarget => usableWithoutTarget;
        public int ManaCost => manaCost;
        public string CardName => cardName;
        public Sprite CardSprite => cardSprite;
        public List<CardActionData> CardActionDataList => cardActionDataList;
        public List<CardDescriptionData> CardDescriptionDataList => cardDescriptionDataList;
        public List<SpecialKeywords> KeywordsList => specialKeywordsList;
        public AudioActionType AudioType => audioType;
        public string MyDescription { get; set; }
        public RarityType Rarity => rarity;
        public int UpgradeStep => upgradeStep;
        public CardData NextUpgrade => nextUpgrade;
        public bool CanUpgrade => nextUpgrade != null;


        public bool ExhaustAfterPlay => exhaustAfterPlay;

        #endregion

        #region Methods
        public void UpdateDescription()
        {
            var str = new StringBuilder();

            foreach (var descriptionData in cardDescriptionDataList)
            {
                str.Append(descriptionData.UseModifier
                    ? descriptionData.GetModifiedValue(this)
                    : descriptionData.GetDescription());
            }

            MyDescription = str.ToString();
        }
        #endregion

        #region Editor Methods
#if UNITY_EDITOR
        public void EditCardName(string newName) => cardName = newName;
        public void EditId(string newId) => id = newId;
        public void EditManaCost(int newCost) => manaCost = newCost;
        public void EditRarity(RarityType targetRarity) => rarity = targetRarity;
        public void EditCardSprite(Sprite newSprite) => cardSprite = newSprite;
        public void EditUsableWithoutTarget(bool newStatus) => usableWithoutTarget = newStatus;
        public void EditExhaustAfterPlay(bool newStatus) => exhaustAfterPlay = newStatus;
        public void EditCardActionDataList(List<CardActionData> newCardActionDataList) =>
            cardActionDataList = newCardActionDataList;
        public void EditCardDescriptionDataList(List<CardDescriptionData> newCardDescriptionDataList) =>
            cardDescriptionDataList = newCardDescriptionDataList;
        public void EditSpecialKeywordsList(List<SpecialKeywords> newSpecialKeywordsList) =>
            specialKeywordsList = newSpecialKeywordsList;
        public void EditAudioType(AudioActionType newAudioActionType) => audioType = newAudioActionType;
        public void EditUpgradeStep(int step) => upgradeStep = Mathf.Clamp(step, 1, 3);
        public void EditNextUpgrade(CardData next) => nextUpgrade = next;
#endif

        #endregion

    }

    [Serializable]
    public class CardActionData
    {
        [SerializeField] private CardActionType cardActionType;
        [SerializeField] private ActionTargetType actionTargetType;
        [SerializeField] private float actionValue;
        [SerializeField] private float actionDelay;

        public ActionTargetType ActionTargetType => actionTargetType;
        public CardActionType CardActionType => cardActionType;
        public float ActionValue => actionValue;
        public float ActionDelay => actionDelay;

        #region Editor

#if UNITY_EDITOR
        public void EditActionType(CardActionType newType) => cardActionType = newType;
        public void EditActionTarget(ActionTargetType newTargetType) => actionTargetType = newTargetType;
        public void EditActionValue(float newValue) => actionValue = newValue;
        public void EditActionDelay(float newValue) => actionDelay = newValue;

#endif


        #endregion
    }

    [Serializable]
    public class CardDescriptionData
    {
        [Header("Text")]
        [SerializeField] private string descriptionText;
        [SerializeField] private bool enableOverrideColor;
        [SerializeField] private Color overrideColor = Color.black;

        [Header("Modifer")]
        [SerializeField] private bool useModifier;
        [SerializeField] private int modifiedActionValueIndex;
        [SerializeField] private StatusType modiferStats;
        [SerializeField] private bool usePrefixOnModifiedValue;
        [SerializeField] private string modifiedValuePrefix = "*";
        [SerializeField] private bool overrideColorOnValueScaled;

        public string DescriptionText => descriptionText;
        public bool EnableOverrideColor => enableOverrideColor;
        public Color OverrideColor => overrideColor;
        public bool UseModifier => useModifier;
        public int ModifiedActionValueIndex => modifiedActionValueIndex;
        public StatusType ModiferStats => modiferStats;
        public bool UsePrefixOnModifiedValue => usePrefixOnModifiedValue;
        public string ModifiedValuePrefix => modifiedValuePrefix;
        public bool OverrideColorOnValueScaled => overrideColorOnValueScaled;

        private CombatManager CombatManager => CombatManager.Instance;

        public string GetDescription()
        {
            var str = new StringBuilder();

            str.Append(DescriptionText);

            if (EnableOverrideColor && !string.IsNullOrEmpty(str.ToString()))
                str.Replace(str.ToString(), ColorExtentions.ColorString(str.ToString(), OverrideColor));

            return str.ToString();
        }

        public string GetModifiedValue(CardData cardData)
        {
            if (cardData.CardActionDataList.Count <= 0) return "";

            if (ModifiedActionValueIndex >= cardData.CardActionDataList.Count)
                modifiedActionValueIndex = cardData.CardActionDataList.Count - 1;

            if (ModifiedActionValueIndex < 0)
                modifiedActionValueIndex = 0;

            var str = new StringBuilder();
            var value = cardData.CardActionDataList[ModifiedActionValueIndex].ActionValue;
            var modifer = 0;
            if (CombatManager)
            {
                var player = CombatManager.CurrentMainAlly;
                if (player)
                {
                    // +Strength (เดิม)
                    modifer = player.CharacterStats.StatusDict[ModiferStats].StatusValue;
                    value += modifer;

                    // ✅ ใช้ "ค่าโจมตีรอบเล่น" (RunBaseAttackBonus) เพื่อให้ตรงกับดาเมจจริง
                    int runAtk = 0;
                    if (player is AllyBase ally) runAtk = ally.RunBaseAttackBonus;
                    else if (player.CharacterData != null) runAtk = player.CharacterData.BaseAttackPower;

                    // เพิ่มเฉพาะเมื่อช่องนี้ตั้ง ModiferStats = Strength (แปลว่าเป็นเลขดาเมจ)
                    if (ModiferStats == NueGames.NueDeck.Scripts.Enums.StatusType.Strength)
                        value += runAtk;

                    if (modifer != 0 && usePrefixOnModifiedValue)
                        str.Append(modifiedValuePrefix);
                }
            }

            str.Append(value);

            if (EnableOverrideColor)
            {
                if (OverrideColorOnValueScaled)
                {
                    if (modifer != 0)
                        str.Replace(str.ToString(), ColorExtentions.ColorString(str.ToString(), OverrideColor));
                }
                else
                {
                    str.Replace(str.ToString(), ColorExtentions.ColorString(str.ToString(), OverrideColor));
                }

            }

            return str.ToString();
        }

        #region Editor
#if UNITY_EDITOR

        public string GetDescriptionEditor()
        {
            var str = new StringBuilder();

            str.Append(DescriptionText);

            return str.ToString();
        }

        public string GetModifiedValueEditor(CardData cardData)
        {
            if (cardData.CardActionDataList.Count <= 0) return "";

            if (ModifiedActionValueIndex >= cardData.CardActionDataList.Count)
                modifiedActionValueIndex = cardData.CardActionDataList.Count - 1;

            if (ModifiedActionValueIndex < 0)
                modifiedActionValueIndex = 0;

            var str = new StringBuilder();
            var value = cardData.CardActionDataList[ModifiedActionValueIndex].ActionValue;
            if (CombatManager)
            {
                var player = CombatManager.CurrentMainAlly;
                if (player)
                {
                    // +Strength (เดิม)
                    var modifer = player.CharacterStats.StatusDict[ModiferStats].StatusValue;
                    value += modifer;

                    // ✅ ใช้ RunBaseAttackBonus เช่นกันในโหมด Editor/Play
                    int runAtk = 0;
                    if (player is AllyBase ally) runAtk = ally.RunBaseAttackBonus;
                    else if (player.CharacterData != null) runAtk = player.CharacterData.BaseAttackPower;

                    if (ModiferStats == NueGames.NueDeck.Scripts.Enums.StatusType.Strength)
                        value += runAtk;

                    if (modifer != 0) str.Append("*");
                }
            }

            str.Append(value);

            return str.ToString();
        }

        public void EditDescriptionText(string newText) => descriptionText = newText;
        public void EditEnableOverrideColor(bool newStatus) => enableOverrideColor = newStatus;
        public void EditOverrideColor(Color newColor) => overrideColor = newColor;
        public void EditUseModifier(bool newStatus) => useModifier = newStatus;
        public void EditModifiedActionValueIndex(int newIndex) => modifiedActionValueIndex = newIndex;
        public void EditModiferStats(StatusType newStatusType) => modiferStats = newStatusType;
        public void EditUsePrefixOnModifiedValues(bool newStatus) => usePrefixOnModifiedValue = newStatus;
        public void EditPrefixOnModifiedValues(string newText) => modifiedValuePrefix = newText;
        public void EditOverrideColorOnValueScaled(bool newStatus) => overrideColorOnValueScaled = newStatus;

#endif
        #endregion
    }
}