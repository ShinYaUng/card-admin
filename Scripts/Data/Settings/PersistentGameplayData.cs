using System;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Data.Collection;
using UnityEngine;
using NueGames.NueDeck.Scripts.Enums;

namespace NueGames.NueDeck.Scripts.Data.Settings
{
    [Serializable]
    public class PersistentGameplayData
    {
        private readonly GameplayData _gameplayData;
        [SerializeField] private List<RelicType> ownedRelics = new List<RelicType>();
        [SerializeField] private bool firstCardFreeUsedThisCombat = false;

        [SerializeField] private int currentGold;
        [SerializeField] private int drawCount;
        [SerializeField] private int maxMana;
        [SerializeField] private int currentMana;
        [SerializeField] private bool canUseCards;
        [SerializeField] private bool canSelectCards;
        [SerializeField] private bool isRandomHand;
        [SerializeField] private List<AllyBase> allyList;
        [SerializeField] private int currentStageId;
        [SerializeField] private int currentEncounterId;
        [SerializeField] private bool isFinalEncounter;
        [SerializeField] private List<CardData> currentCardsList;
        [SerializeField] private List<AllyHealthData> allyHealthDataDataList;
        // === RUN PROGRESS FIELDS START ===
        [SerializeField] private List<RunProgress> runProgressList = new List<RunProgress>();
        // === RUN PROGRESS FIELDS END ===
        // === COMBAT EVENT FIELDS START ===
        [SerializeField] private CombatEventType currentCombatEvent = CombatEventType.None;
        [SerializeField] private int currentCombatEventValue = 0;  // dmg ต่อเทิร์น หรือ เปอร์เซ็นต์โบนัส
        [SerializeField] private string currentCombatEventName = "";
        [SerializeField] private string currentCombatEventDesc = "";

        public CombatEventType CurrentCombatEventType => currentCombatEvent;
        public int CurrentCombatEventValue => currentCombatEventValue;
        public string CurrentCombatEventSummary => string.IsNullOrEmpty(currentCombatEventName)
            ? "Event: -"
            : $"Event: {currentCombatEventName} — {currentCombatEventDesc}";
        // === COMBAT EVENT FIELDS END ===
        // === RUN PROGRESS METHODS START ===
        private RunProgress GetOrCreateProgress(string characterId)
        {
            var rp = runProgressList.Find(x => x.CharacterId == characterId);
            if (rp == null)
            {
                rp = new RunProgress { CharacterId = characterId };
                runProgressList.Add(rp);
            }
            return rp;
        }

        public RunProgress GetProgress(string characterId) => GetOrCreateProgress(characterId);

        public void AddExpTo(string characterId, int amount)
        {
            var rp = GetOrCreateProgress(characterId);
            rp.CurrentExp += amount;

            while (rp.CurrentExp >= rp.ExpToNextLevel)
            {
                rp.CurrentExp -= rp.ExpToNextLevel;
                rp.Level++;
                rp.AvailablePoints += 1;     // ได้ 1 แต้มต่อเลเวล
                rp.ExpToNextLevel += 50;     // ปรับความชันตามต้องการ
            }
        }

        public bool SpendPoint(string characterId, RunStatType stat)
        {
            var rp = GetOrCreateProgress(characterId);
            if (rp.AvailablePoints <= 0) return false;

            switch (stat)
            {
                case RunStatType.Health: rp.AddedHealth += 10; break;
                case RunStatType.Armor: rp.AddedArmor += 2; break;
                case RunStatType.Energy: rp.AddedEnergy += 1; break;
                case RunStatType.Attack: rp.AddedAttack += 2; break;
            }
            rp.AvailablePoints--;
            return true;
        }

        private void ResetRunProgress()
        {
            runProgressList = new List<RunProgress>();
        }
        // === RUN PROGRESS METHODS END ===

        // === RELIC METHODS START ===
        public void ResetRelics() => ownedRelics = new List<RelicType>();
        public bool HasRelic(RelicType t) => ownedRelics != null && ownedRelics.Contains(t);
        public void AddRelic(RelicType t)
        {
            if (t == RelicType.None) return;
            if (!ownedRelics.Contains(t)) ownedRelics.Add(t);
        }

        public void ResetCombatFlags() => firstCardFreeUsedThisCombat = false;
        public bool FirstCardFreeUsedThisCombat
        {
            get => firstCardFreeUsedThisCombat;
            set => firstCardFreeUsedThisCombat = value;
        }

        // ผลของ relic แบบรวมศูนย์
        public float GetBlockGainMultiplier() => HasRelic(RelicType.DoubleBlock) ? 2f : 1f;
        public float GetPriceMultiplier() => HasRelic(RelicType.PriceDiscount20) ? 0.8f : 1f;
        public int GetEffectiveCost(int baseCost)
        {
            var m = GetPriceMultiplier();
            return Mathf.CeilToInt(baseCost * m);
        }
        // === RELIC METHODS END ===

        public void RollRandomCombatEventForThisEncounter()
        {
            // โอกาสตัวอย่าง: None 40% | DamageEachAllyTurn 30% | +EXP 15% | +Gold 15%
            float r = UnityEngine.Random.value;
            if (r < 0.40f)
            {
                currentCombatEvent = CombatEventType.None;
                currentCombatEventValue = 0;
                currentCombatEventName = "No Event";
                currentCombatEventDesc = "-";
                return;
            }
            else if (r < 0.70f)
            {
                currentCombatEvent = CombatEventType.DamageEachAllyTurn;
                currentCombatEventValue = 2; // โดน 2 ดาเมจ ทุกต้นเทิร์นผู้เล่น (ปรับได้)
                currentCombatEventName = "Bleeding Grounds";
                currentCombatEventDesc = $"Take {currentCombatEventValue} damage at the start of your turns.";
            }
            else if (r < 0.85f)
            {
                currentCombatEvent = CombatEventType.BonusExpPercent;
                currentCombatEventValue = 50; // +50% EXP หลังชนะ
                currentCombatEventName = "+EXP Bonus";
                currentCombatEventDesc = $"+{currentCombatEventValue}% EXP after victory.";
            }
            else
            {
                currentCombatEvent = CombatEventType.BonusGoldPercent;
                currentCombatEventValue = 50; // +50% Gold หลังชนะ
                currentCombatEventName = "+Gold Bonus";
                currentCombatEventDesc = $"+{currentCombatEventValue}% Gold after victory.";
            }
        }

        public int ApplyExpBonusIfAny(int baseExp)
        {
            if (currentCombatEvent == CombatEventType.BonusExpPercent)
                return Mathf.CeilToInt(baseExp * (1f + currentCombatEventValue / 100f));
            return baseExp;
        }
        public int ApplyGoldBonusIfAny(int baseGold)
        {
            if (currentCombatEvent == CombatEventType.BonusGoldPercent)
                return Mathf.CeilToInt(baseGold * (1f + currentCombatEventValue / 100f));
            return baseGold;
        }
        public PersistentGameplayData(GameplayData gameplayData)
        {
            _gameplayData = gameplayData;

            InitData();
        }

        public void SetAllyHealthData(string id, int newCurrentHealth, int newMaxHealth)
        {
            var data = allyHealthDataDataList.Find(x => x.CharacterId == id);
            var newData = new AllyHealthData();
            newData.CharacterId = id;
            newData.CurrentHealth = newCurrentHealth;
            newData.MaxHealth = newMaxHealth;
            if (data != null)
            {
                allyHealthDataDataList.Remove(data);
                allyHealthDataDataList.Add(newData);
            }
            else
            {
                allyHealthDataDataList.Add(newData);
            }
        }
        private void InitData()
        {
            DrawCount = _gameplayData.DrawCount;
            MaxMana = _gameplayData.MaxMana;
            CurrentMana = MaxMana;
            CanUseCards = true;
            CanSelectCards = true;
            IsRandomHand = _gameplayData.IsRandomHand;
            AllyList = new List<AllyBase>(_gameplayData.InitalAllyList);
            CurrentEncounterId = 0;
            CurrentStageId = 0;
            CurrentGold = 0;
            CurrentCardsList = new List<CardData>();
            IsFinalEncounter = false;
            allyHealthDataDataList = new List<AllyHealthData>();
            ResetRunProgress();
        }

        #region Encapsulation

        public int DrawCount
        {
            get => drawCount;
            set => drawCount = value;
        }

        public int MaxMana
        {
            get => maxMana;
            set => maxMana = value;
        }

        public int CurrentMana
        {
            get => currentMana;
            set => currentMana = value;
        }

        public bool CanUseCards
        {
            get => canUseCards;
            set => canUseCards = value;
        }

        public bool CanSelectCards
        {
            get => canSelectCards;
            set => canSelectCards = value;
        }

        public bool IsRandomHand
        {
            get => isRandomHand;
            set => isRandomHand = value;
        }

        public List<AllyBase> AllyList
        {
            get => allyList;
            set => allyList = value;
        }

        public int CurrentStageId
        {
            get => currentStageId;
            set => currentStageId = value;
        }

        public int CurrentEncounterId
        {
            get => currentEncounterId;
            set => currentEncounterId = value;
        }

        public bool IsFinalEncounter
        {
            get => isFinalEncounter;
            set => isFinalEncounter = value;
        }

        public List<CardData> CurrentCardsList
        {
            get => currentCardsList;
            set => currentCardsList = value;
        }

        public List<AllyHealthData> AllyHealthDataList
        {
            get => allyHealthDataDataList;
            set => allyHealthDataDataList = value;
        }
        public int CurrentGold
        {
            get => currentGold;
            set => currentGold = value;
        }

        // ===== EXPORT/IMPORT for SaveSystem =====
        public List<RunProgress> ExportRunProgressList()
        {
            return new List<RunProgress>(runProgressList);
        }
        public void ImportRunProgressList(List<RunProgress> list)
        {
            runProgressList = list ?? new List<RunProgress>();
        }

        public List<RelicType> ExportOwnedRelics()
        {
            return new List<RelicType>(ownedRelics);
        }
        public void ImportOwnedRelics(List<RelicType> list)
        {
            ownedRelics = list ?? new List<RelicType>();
        }
        #endregion
    }
    // === RUN PROGRESS (run-only) START ===
    [Serializable]
    public class RunProgress
    {
        public string CharacterId;
        public int Level = 1;
        public int CurrentExp = 0;
        public int ExpToNextLevel = 100;
        public int AvailablePoints = 0;

        // แต้มอัปสเตตเฉพาะรอบนี้
        public int AddedHealth = 0;  // +Max HP
        public int AddedArmor = 0;  // +Block เริ่มไฟท์
        public int AddedEnergy = 0;  // +พลังงานต่อเทิร์น (ไปบวก MaxMana)
        public int AddedAttack = 0;  // +Base Attack
    }

    public enum RunStatType { Health, Armor, Energy, Attack }
    // === RUN PROGRESS (run-only) END ===

    
}