using System;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.NueExtentions;
using UnityEngine;
using UnityEngine.UI; // สำหรับ LayoutRebuilder


namespace NueGames.NueDeck.Scripts.UI.Reward
{
    public class RewardCanvas : CanvasBase
    {
        [Header("References")]
        [SerializeField] private RewardContainerData rewardContainerData;
        [SerializeField] private Transform rewardRoot;
        [SerializeField] private RewardContainer rewardContainerPrefab;
        [SerializeField] private Transform rewardPanelRoot;
        [Header("Choice")]
        [SerializeField] private Transform choice2DCardSpawnRoot;
        [SerializeField] private ChoiceCard choiceCardUIPrefab;
        [SerializeField] private ChoicePanel choicePanel;
        // ===== Add below existing Choice fields =====
        [Header("Relics")]
        [SerializeField] private RelicDatabase relicDatabase;     // SO รวมรายการ Relic
        [SerializeField] private Transform relicChoiceRoot;        // ที่วางปุ่มตัวเลือก 3 อัน
        [SerializeField] private RelicChoiceItem relicChoicePrefab;// พรีแฟบไอเทมให้กดเลือก

        private readonly List<RewardContainer> _currentRewardsList = new List<RewardContainer>();
        private readonly List<ChoiceCard> _spawnedChoiceList = new List<ChoiceCard>();
        private readonly List<CardData> _cardRewardList = new List<CardData>();

        public ChoicePanel ChoicePanel => choicePanel;

        #region Public Methods

        public void PrepareCanvas()
        {
            rewardPanelRoot.gameObject.SetActive(true);
        }
        public void BuildReward(RewardType rewardType)
        {
            var rewardClone = Instantiate(rewardContainerPrefab, rewardRoot);
            _currentRewardsList.Add(rewardClone);

            switch (rewardType)
            {
                case RewardType.Gold:
                    var rewardGold = rewardContainerData.GetRandomGoldReward(out var goldRewardData);
                    var finalGold = GameManager.PersistentGameplayData.ApplyGoldBonusIfAny(rewardGold); // << ใส่บรรทัดนี้

                    rewardClone.BuildReward(goldRewardData.RewardSprite, goldRewardData.RewardDescription);
                    rewardClone.RewardButton.onClick.AddListener(() => GetGoldReward(rewardClone, finalGold)); // ใช้ finalGold
                    break;
                case RewardType.Card:
                    var rewardCardList = rewardContainerData.GetRandomCardRewardList(out var cardRewardData);
                    _cardRewardList.Clear();
                    foreach (var cardData in rewardCardList)
                        _cardRewardList.Add(cardData);
                    rewardClone.BuildReward(cardRewardData.RewardSprite, cardRewardData.RewardDescription);
                    rewardClone.RewardButton.onClick.AddListener(() => GetCardReward(rewardClone, 3));
                    break;
                case RewardType.Relic:
                    {
                        // เปิด Overlay เลือก Relic และเอาขึ้นบนสุด
                        ChoicePanel.gameObject.SetActive(true);
                        choicePanel.transform.SetAsLastSibling();

                        // ซ่อน RewardPanel ไว้ก่อนเพื่อกันผู้เล่นกด "Next" ทับ (จะเปิดคืนตอน ResetChoice)
                        if (rewardPanelRoot) rewardPanelRoot.gameObject.SetActive(false);

                        // กันพลาด: ล้างลูกเก่าในรากวางของ Relic
                        if (relicChoiceRoot != null)
                        {
                            foreach (Transform t in relicChoiceRoot) Destroy(t.gameObject);
                            relicChoiceRoot.gameObject.SetActive(true);
                        }

                        // สุ่ม 3 ชิ้น
                        var relicList = relicDatabase.GetRandomRelicList(3);
                        System.Action onPicked = () =>
                        {
                            ResetChoice(); // ปิดแผง/ล้างของ แล้วเปิด RewardPanel คืน (ทำใน ResetChoice ด้านล่าง)
                        };

                        foreach (var r in relicList)
                        {
                            var item = Instantiate(relicChoicePrefab, relicChoiceRoot);
                            item.transform.localScale = Vector3.one; // กันสเกล 0 จากพรีแฟบ
                            item.Build(r, onPicked);
                        }

                        // ⬇️ วาง 2–3 บรรทัดนี้ “หลัง foreach” และ “ก่อน” ลบ rewardClone
                        Canvas.ForceUpdateCanvases();
                        var rt = relicChoiceRoot as RectTransform;
                        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                        // (ถ้ายังไม่ขึ้น ลองเสริมรีเฟรชที่ parent ด้วย)
                        var cp = choicePanel.transform as RectTransform;
                        if (cp) LayoutRebuilder.ForceRebuildLayoutImmediate(cp);

                     
                        // กล่องรางวัลสำหรับ Relic ไม่ต้องใช้แล้ว ลบได้
                        _currentRewardsList.Remove(rewardClone);
                        Destroy(rewardClone.gameObject);
                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(rewardType), rewardType, null);
            }
        }

        public override void ResetCanvas()
        {
            ResetRewards();

            ResetChoice();
        }

        private void ResetRewards()
        {
            foreach (var rewardContainer in _currentRewardsList)
                Destroy(rewardContainer.gameObject);

            _currentRewardsList?.Clear();
        }

        private void ResetChoice()
        {
            foreach (var choice in _spawnedChoiceList)
                Destroy(choice.gameObject);
            _spawnedChoiceList?.Clear();

            if (relicChoiceRoot != null)
            {
                foreach (Transform t in relicChoiceRoot)
                    Destroy(t.gameObject);
            }

            // ✅ เปิด RewardPanel คืน เพื่อให้รับ Gold/Card ต่อได้
            if (rewardPanelRoot) rewardPanelRoot.gameObject.SetActive(true);

            ChoicePanel.DisablePanel();
        }



        #endregion

        #region Private Methods
        private void GetGoldReward(RewardContainer rewardContainer, int amount)
        {
            GameManager.PersistentGameplayData.CurrentGold += amount;
            _currentRewardsList.Remove(rewardContainer);
            UIManager.InformationCanvas.SetGoldText(GameManager.PersistentGameplayData.CurrentGold);
            Destroy(rewardContainer.gameObject);
        }

        private void GetCardReward(RewardContainer rewardContainer, int amount = 3)
        {
            ChoicePanel.gameObject.SetActive(true);

            for (int i = 0; i < amount; i++)
            {
                Transform spawnTransform = choice2DCardSpawnRoot;

                var choice = Instantiate(choiceCardUIPrefab, spawnTransform);

                var reward = _cardRewardList.RandomItem();
                choice.BuildReward(reward);
                choice.OnCardChose += ResetChoice;

                _cardRewardList.Remove(reward);
                _spawnedChoiceList.Add(choice);
                _currentRewardsList.Remove(rewardContainer);

            }

            Destroy(rewardContainer.gameObject);
        }
        #endregion

    }
}