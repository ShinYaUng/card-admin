using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.UI.Shop;
using System.Collections;

namespace NueGames.NueDeck.Scripts.UI
{
    public class ShopCanvas : CanvasBase
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private Transform offersRoot;
        [SerializeField] private ShopOfferSlot offerSlotPrefab;
        [SerializeField] private Button rerollButton;
        [SerializeField] private Button closeButton;

        [Header("Config")]
        [SerializeField] private ShopConfig config;

        private GameManager GM => GameManager.Instance;
        private UIManager UI => UIManager.Instance;

        private readonly List<ShopOfferSlot> _slots = new();

        public override void OpenCanvas()
        {
            base.OpenCanvas();
            StartCoroutine(OpenAndBuildRoutine()); // เปลี่ยนจากเรียก BuildShop() ตรงๆ
            BindButtons();
            UpdateGoldUI();
        }

        private IEnumerator OpenAndBuildRoutine()
        {
            // 1) รอรีเฟรชรีโมต (ถ้ามีบริการ)
            yield return EnsureRemoteRefreshed();

            // 2) รวมพูล: base + remote (ไม่ซ้ำ Id)
            var baseAll = GM.GameplayData.AllCardsList; // เดิม
            _mergedPool.Clear();
            _mergedPool.AddRange(baseAll);

            if (CardCatalogService.Instance != null)
                _mergedPool = _mergedPool
                    .Concat(CardCatalogService.Instance.GetAllRuntimeCards())
                    .GroupBy(c => c.Id)
                    .Select(g => g.First())
                    .ToList();

            // 3) สร้างร้านจากพูลรวม
            BuildShop();
        }

        private void BindButtons()
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(Reroll);
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseShop);
        }

        private IEnumerable<CardData> PickOffers()
        {
            var source = (_mergedPool != null && _mergedPool.Count > 0)
                ? _mergedPool
                : GM.GameplayData.AllCardsList; // fallback เดิม

            return source.OrderBy(_ => Random.value).Take(Mathf.Max(1, config.offerCount));
        }

        private void ClearOffers()
        {
            foreach (Transform c in offersRoot) Destroy(c.gameObject);
            _slots.Clear();
        }

        private void BuildShop()
        {
            ClearOffers();
            var pgd = GM.PersistentGameplayData;

            foreach (var card in PickOffers())
            {
                var basePrice = config.GetPrice(card.Rarity);
                var finalPrice = pgd.GetEffectiveCost(basePrice); // ราคาแสดง/ซื้อจริง

                var slot = Instantiate(offerSlotPrefab, offersRoot);
                slot.Setup(card, finalPrice, this);  // ส่ง “ราคาสุทธิ” เข้าไป
                _slots.Add(slot);
            }
        }


        public void TryPurchase(CardData card, int price, ShopOfferSlot slot)
        {
            var pgd = GM.PersistentGameplayData;
            if (pgd.CurrentGold < price) { /* แจ้งเตือนทองไม่พอ */ return; }

            pgd.CurrentGold -= price;
            pgd.CurrentCardsList.Add(card);
            Destroy(slot.gameObject);
            _slots.Remove(slot);

            UpdateGoldUI();
            UI.OpenDeckTrimIfOverLimit(14);
        }


        private void Reroll()
        {
            var pgd = GM.PersistentGameplayData;
            int baseCost = config.rerollCost;
            int finalCost = pgd.GetEffectiveCost(baseCost);

            if (pgd.CurrentGold < finalCost) return;

            pgd.CurrentGold -= finalCost;
            UpdateGoldUI();
            BuildShop();
        }


        private void CloseShop()
        {
            UI.SetCanvas(this, false);
        }

        private void UpdateGoldUI()
        {
            goldText.text = GM.PersistentGameplayData.CurrentGold.ToString();
        }

        private List<CardData> _mergedPool = new();
        private IEnumerator EnsureRemoteRefreshed()
        {
            if (CardCatalogService.Instance == null) yield break;
            var task = CardCatalogService.Instance.Refresh();
            while (!task.IsCompleted) yield return null; // รอ async โดยไม่ block
        }

    }
}
