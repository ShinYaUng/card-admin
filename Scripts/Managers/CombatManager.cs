using System;
using System.Collections;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Characters.Enemies;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Utils.Background;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Managers
{
    public class CombatManager : MonoBehaviour
    {
        private CombatManager() { }
        private bool _startBlockAppliedThisCombat = false;
        public static CombatManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private BackgroundContainer backgroundContainer;
        [SerializeField] private List<Transform> enemyPosList;
        [SerializeField] private List<Transform> allyPosList;


        #region Cache
        public List<EnemyBase> CurrentEnemiesList { get; private set; } = new List<EnemyBase>();
        public List<AllyBase> CurrentAlliesList { get; private set; } = new List<AllyBase>();

        public Action OnAllyTurnStarted;
        public Action OnEnemyTurnStarted;
        public List<Transform> EnemyPosList => enemyPosList;

        public List<Transform> AllyPosList => allyPosList;

        public AllyBase CurrentMainAlly => CurrentAlliesList.Count > 0 ? CurrentAlliesList[0] : null;

        public EnemyEncounter CurrentEncounter { get; private set; }

        public CombatStateType CurrentCombatStateType
        {
            get => _currentCombatStateType;
            private set
            {
                ExecuteCombatState(value);
                _currentCombatStateType = value;
            }
        }

        private CombatStateType _currentCombatStateType;
        protected FxManager FxManager => FxManager.Instance;
        protected AudioManager AudioManager => AudioManager.Instance;
        protected GameManager GameManager => GameManager.Instance;
        protected UIManager UIManager => UIManager.Instance;

        protected CollectionManager CollectionManager => CollectionManager.Instance;

        #endregion


        #region Setup
        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                Instance = this;
                CurrentCombatStateType = CombatStateType.PrepareCombat;
            }
        }

        private void Start()
        {
            StartCombat();
        }

        public void StartCombat()
        {
            BuildEnemies();
            BuildAllies();
            ApplyRunUpgradesToAllies(); // MaxHP/Energy/Attack per run

            backgroundContainer.OpenSelectedBackground();
            CollectionManager.SetGameDeck();

            UIManager.CombatCanvas.gameObject.SetActive(true);
            UIManager.InformationCanvas.gameObject.SetActive(true);

            // NEW: roll event + show on UI
            var pgd = GameManager.Instance.PersistentGameplayData;
            pgd.RollRandomCombatEventForThisEncounter();
            UIManager.CombatCanvas.SetEventText(pgd.CurrentCombatEventSummary);

            _startBlockAppliedThisCombat = false;
            GameManager.Instance.PersistentGameplayData.ResetCombatFlags();
            CurrentCombatStateType = CombatStateType.AllyTurn;
        }



        private void ExecuteCombatState(CombatStateType targetStateType)
        {
            switch (targetStateType)
            {
                case CombatStateType.PrepareCombat:
                    break;
                case CombatStateType.AllyTurn:
                    OnAllyTurnStarted?.Invoke();

                    if (CurrentMainAlly.CharacterStats.IsStunned)
                    {
                        EndTurn();
                        return;
                    }

                    GameManager.PersistentGameplayData.CurrentMana = GameManager.PersistentGameplayData.MaxMana;
                    CollectionManager.DrawCards(GameManager.PersistentGameplayData.DrawCount);
                    GameManager.PersistentGameplayData.CanSelectCards = true;

                    if (!_startBlockAppliedThisCombat)
                    {
                        ApplyStartingBlockTotal();
                        _startBlockAppliedThisCombat = true;
                    }

                    // NEW: Event tick at start of player's turn
                    ApplyCombatEventAtAllyTurnStart();

                    break;

                case CombatStateType.EnemyTurn:

                    OnEnemyTurnStarted?.Invoke();

                    CollectionManager.DiscardHand();

                    StartCoroutine(nameof(EnemyTurnRoutine));

                    GameManager.PersistentGameplayData.CanSelectCards = false;

                    break;
                case CombatStateType.EndCombat:

                    GameManager.PersistentGameplayData.CanSelectCards = false;

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetStateType), targetStateType, null);
            }
        }
        #endregion

        #region Public Methods
        public void EndTurn()
        {
            CurrentCombatStateType = CombatStateType.EnemyTurn;
        }
        public void OnAllyDeath(AllyBase targetAlly)
        {
            var targetAllyData = GameManager.PersistentGameplayData.AllyList.Find(x =>
                x.AllyCharacterData.CharacterID == targetAlly.AllyCharacterData.CharacterID);
            if (GameManager.PersistentGameplayData.AllyList.Count > 1)
                GameManager.PersistentGameplayData.AllyList.Remove(targetAllyData);
            CurrentAlliesList.Remove(targetAlly);
            UIManager.InformationCanvas.ResetCanvas();
            if (CurrentAlliesList.Count <= 0)
                LoseCombat();
        }
        public void OnEnemyDeath(EnemyBase targetEnemy)
        {
            CurrentEnemiesList.Remove(targetEnemy);
            if (CurrentEnemiesList.Count <= 0)
                WinCombat();
        }
        public void DeactivateCardHighlights()
        {
            foreach (var currentEnemy in CurrentEnemiesList)
                currentEnemy.EnemyCanvas.SetHighlight(false);

            foreach (var currentAlly in CurrentAlliesList)
                currentAlly.AllyCanvas.SetHighlight(false);
        }
        public void IncreaseMana(int target)
        {
            GameManager.PersistentGameplayData.CurrentMana += target;
            UIManager.CombatCanvas.SetPileTexts();
        }
        public void HighlightCardTarget(ActionTargetType targetTypeTargetType)
        {
            switch (targetTypeTargetType)
            {
                case ActionTargetType.Enemy:
                    foreach (var currentEnemy in CurrentEnemiesList)
                        currentEnemy.EnemyCanvas.SetHighlight(true);
                    break;
                case ActionTargetType.Ally:
                    foreach (var currentAlly in CurrentAlliesList)
                        currentAlly.AllyCanvas.SetHighlight(true);
                    break;
                case ActionTargetType.AllEnemies:
                    foreach (var currentEnemy in CurrentEnemiesList)
                        currentEnemy.EnemyCanvas.SetHighlight(true);
                    break;
                case ActionTargetType.AllAllies:
                    foreach (var currentAlly in CurrentAlliesList)
                        currentAlly.AllyCanvas.SetHighlight(true);
                    break;
                case ActionTargetType.RandomEnemy:
                    foreach (var currentEnemy in CurrentEnemiesList)
                        currentEnemy.EnemyCanvas.SetHighlight(true);
                    break;
                case ActionTargetType.RandomAlly:
                    foreach (var currentAlly in CurrentAlliesList)
                        currentAlly.AllyCanvas.SetHighlight(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetTypeTargetType), targetTypeTargetType, null);
            }
        }
        #endregion

        #region Private Methods
        private void ApplyCombatEventAtAllyTurnStart()
        {
            var pgd = GameManager.PersistentGameplayData;
            if (pgd.CurrentCombatEventType == CombatEventType.DamageEachAllyTurn)
            {
                int dmg = Mathf.Max(1, pgd.CurrentCombatEventValue);
                var ally = CurrentMainAlly;
                if (ally != null)
                {
                    ally.CharacterStats.Damage(dmg);
                    if (FxManager != null)
                        FxManager.SpawnFloatingText(ally.TextSpawnRoot, $"-{dmg} (Event)");
                }
            }
        }

        private void BuildEnemies()
        {
            CurrentEncounter = GameManager.EncounterData.GetEnemyEncounter(
                GameManager.PersistentGameplayData.CurrentStageId,
                GameManager.PersistentGameplayData.CurrentEncounterId,
                GameManager.PersistentGameplayData.IsFinalEncounter);

            var enemyList = CurrentEncounter.EnemyList;
            for (var i = 0; i < enemyList.Count; i++)
            {
                var clone = Instantiate(enemyList[i].EnemyPrefab, EnemyPosList.Count >= i ? EnemyPosList[i] : EnemyPosList[0]);
                clone.BuildCharacter();
                CurrentEnemiesList.Add(clone);
            }
        }
        private void BuildAllies()
        {
            for (var i = 0; i < GameManager.PersistentGameplayData.AllyList.Count; i++)
            {
                var clone = Instantiate(GameManager.PersistentGameplayData.AllyList[i], AllyPosList.Count >= i ? AllyPosList[i] : AllyPosList[0]);
                clone.BuildCharacter();
                CurrentAlliesList.Add(clone);
            }
        }
        // เรียกหลัง BuildAllies()
        private void ApplyRunUpgradesToAllies()
        {
            // ตั้งค่า Energy/เทิร์นจากตัวหลัก
            if (CurrentAlliesList.Count > 0)
            {
                var main = CurrentMainAlly != null ? CurrentMainAlly : CurrentAlliesList[0];
                var mainRp = GameManager.Instance.PersistentGameplayData.GetProgress(main.AllyCharacterData.CharacterID);
                GameManager.Instance.PersistentGameplayData.MaxMana =
                    GameManager.Instance.GameplayData.MaxMana + mainRp.AddedEnergy;
            }

            foreach (var ally in CurrentAlliesList)
            {
                var cd = ally.AllyCharacterData;
                var rp = GameManager.Instance.PersistentGameplayData.GetProgress(cd.CharacterID);

                // ✅ MaxHP แบบ absolute = base(SO) + แต้มอัป
                int desiredMax = cd.MaxHealth + rp.AddedHealth; // จาก SO: CharacterDataBase.MaxHealth :contentReference[oaicite:3]{index=3}
                int prevMax = ally.CharacterStats.MaxHealth;

                if (desiredMax != prevMax)
                {
                    // ถ้า Max เพิ่ม ให้ +HP ปัจจุบันเท่าที่เพิ่ม (หรือจะ heal to max ก็ได้)
                    int delta = desiredMax - prevMax;
                    ally.CharacterStats.MaxHealth = desiredMax;
                    if (delta > 0)
                        ally.CharacterStats.CurrentHealth = Mathf.Min(ally.CharacterStats.CurrentHealth + delta, desiredMax);
                    else
                        ally.CharacterStats.CurrentHealth = Mathf.Min(ally.CharacterStats.CurrentHealth, desiredMax);
                }

                // ดาเมจฐานเฉพาะรอบ (ไปใช้ใน AttackAction)
                ally.RunBaseAttackBonus = cd.BaseAttackPower + rp.AddedAttack;
            }
        }


        private void ApplyStartingBlockTotal()
        {
            var pgd = GameManager.Instance.PersistentGameplayData;
            foreach (var ally in CurrentAlliesList)
            {
                var cd = ally.AllyCharacterData;
                var rp = pgd.GetProgress(cd.CharacterID); // run-only progress
                var startBlock = cd.BaseArmor + rp.AddedArmor; // ฐาน + แต้มอัป “เริ่มไฟต์”

                if (startBlock > 0)
                {
                    // ✅ คูณผลจาก Relic (DoubleBlock = x2) ก่อนใส่ Block
                    var mul = pgd.GetBlockGainMultiplier();          // 1f หรือ 2f
                    var finalBlock = Mathf.RoundToInt(startBlock * mul);

                    ally.CharacterStats.ApplyStatus(StatusType.Block, finalBlock);
                    Debug.Log($"[StartBlock] {cd.CharacterName} base:{cd.BaseArmor} + added:{rp.AddedArmor} = {startBlock}  x{mul} => {finalBlock}");
                }
            }
        }



        private void LoseCombat()
        {
            if (CurrentCombatStateType == CombatStateType.EndCombat) return;

            CurrentCombatStateType = CombatStateType.EndCombat;

            CollectionManager.DiscardHand();
            CollectionManager.DiscardPile.Clear();
            CollectionManager.DrawPile.Clear();
            CollectionManager.HandPile.Clear();
            CollectionManager.HandController.hand.Clear();
            UIManager.CombatCanvas.gameObject.SetActive(true);
            UIManager.CombatCanvas.CombatLosePanel.SetActive(true);
        }
        private void WinCombat()
        {
            if (CurrentCombatStateType == CombatStateType.EndCombat) return;

            CurrentCombatStateType = CombatStateType.EndCombat;

            foreach (var allyBase in CurrentAlliesList)
            {
                GameManager.PersistentGameplayData.SetAllyHealthData(
                    allyBase.AllyCharacterData.CharacterID,
                    allyBase.CharacterStats.CurrentHealth,
                    allyBase.CharacterStats.MaxHealth
                );
            }

            // 🟢 แจก EXP ตาม EnemyCharacterData
            GrantExpRewardFromEnemies();
            GrantGoldFromEnemies();
            CollectionManager.ClearPiles();

            if (GameManager.PersistentGameplayData.IsFinalEncounter)
            {
                UIManager.CombatCanvas.CombatWinPanel.SetActive(true);
            }
            else
            {
                CurrentMainAlly.CharacterStats.ClearAllStatus();
                // ⬇️ คำนวนด่านที่เพิ่งผ่าน (ใช้เลขคนเล่น 1-based)
                int justClearedEncounterNumber = GameManager.PersistentGameplayData.CurrentEncounterId + 1;

                GameManager.PersistentGameplayData.CurrentEncounterId++;
                UIManager.CombatCanvas.gameObject.SetActive(false);
                UIManager.RewardCanvas.gameObject.SetActive(true);
                UIManager.RewardCanvas.PrepareCanvas();
                UIManager.RewardCanvas.BuildReward(RewardType.Gold);
                UIManager.RewardCanvas.BuildReward(RewardType.Card);
                // ⬇️ เงื่อนไขด่าน 5 และ 10 แจก Relic
                if (justClearedEncounterNumber == 5 || justClearedEncounterNumber == 10)
                {
                    UIManager.RewardCanvas.BuildReward(RewardType.Relic);
                }
            }
        }

        #endregion

        private void GrantExpRewardFromEnemies()
        {
            // 1) รวม EXP ดิบจากศัตรูทั้งหมด
            int baseTotalExp = 0;
            foreach (var enemyData in CurrentEncounter.EnemyList)
                baseTotalExp += enemyData.ExpReward;

            // 2) ให้ PersistentGameplayData คูณโบนัสอีเว้นท์ (ถ้ามี)
            var pgd = GameManager.Instance.PersistentGameplayData;
            int finalTotalExp = pgd.ApplyExpBonusIfAny(baseTotalExp);

            // 3) แจก EXP ให้พันธมิตรทุกตัวด้วยค่าที่คูณแล้ว
            foreach (var allyBase in CurrentAlliesList)
            {
                var id = allyBase.AllyCharacterData.CharacterID;
                pgd.AddExpTo(id, finalTotalExp);
                Debug.Log($"[EXP] {id} +{finalTotalExp} (base:{baseTotalExp})");
            }
        }

        private void GrantGoldFromEnemies()
        {
            int baseGold = 0;
            foreach (var enemyData in CurrentEncounter.EnemyList)
                baseGold += enemyData.GoldReward;

            var pgd = GameManager.PersistentGameplayData;
            // ใช้โบนัส +Gold จากอีเวนต์ถ้ามี (เช่น +50%) — มีเมธอดช่วยใน PGD แล้ว
            int finalGold = pgd.ApplyGoldBonusIfAny(baseGold);

            pgd.CurrentGold += finalGold; // เก็บลงกระเป๋า run ปัจจุบัน (มีพร็อพ CurrentGold อยู่แล้ว)
            if (UIManager != null && UIManager.InformationCanvas != null)
                UIManager.InformationCanvas.SetGoldText(pgd.CurrentGold);

            Debug.Log($"[Gold] +{finalGold} (base:{baseGold}) from enemies");
        }




        #region Routines
        private IEnumerator EnemyTurnRoutine()
        {
            var waitDelay = new WaitForSeconds(0.1f);

            foreach (var currentEnemy in CurrentEnemiesList)
            {
                yield return currentEnemy.StartCoroutine(nameof(EnemyExample.ActionRoutine));
                yield return waitDelay;
            }

            if (CurrentCombatStateType != CombatStateType.EndCombat)
                CurrentCombatStateType = CombatStateType.AllyTurn;
        }
        #endregion


    }
}