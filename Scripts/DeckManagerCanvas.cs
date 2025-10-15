using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.UI;  // << ต้องมี เพื่อเห็น CanvasBase

namespace NueGames.NueDeck.Scripts.UI   // แนะนำให้อยู่ namespace เดียวกับ UI อื่นๆ
{
    public class DeckManagerCanvas : CanvasBase
    {
        [Header("Refs")]
        [SerializeField] private Transform contentRoot;      // Grid/Content
        [SerializeField] private UpgradeCardSlot slotPrefab; // Prefab ของช่อง
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI goldText;

        [SerializeField] private CardBase uiCardPrefab; // ใส่พรีแฟบ CardUI ตรงนี้

        private GameManager GM => GameManager.Instance;

        private void Awake()
        {
            if (closeButton) closeButton.onClick.AddListener(CloseCanvas);
        }

        // === สำคัญ: รองรับ UIManager.SetCanvas ===
        public override void ResetCanvas()
        {
            base.ResetCanvas();
            // ล้างรายการ
            if (contentRoot != null)
            {
                for (int i = contentRoot.childCount - 1; i >= 0; i--)
                    Destroy(contentRoot.GetChild(i).gameObject);
            }
            // อัปเดตทอง
            if (goldText && GM && GM.PersistentGameplayData != null)
                goldText.text = GM.PersistentGameplayData.CurrentGold.ToString();
        }

        public override void OpenCanvas()
        {
            base.OpenCanvas();
            Refresh();
        }

        public override void CloseCanvas()
        {
            base.CloseCanvas();
        }

        // === ใช้งานภายใน ===
        public void Refresh()
        {
            if (!GM || GM.PersistentGameplayData == null || contentRoot == null || slotPrefab == null) return;

            // ล้างเก่า
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);

            var list = GM.PersistentGameplayData.CurrentCardsList;
            for (int i = 0; i < list.Count; i++)
            {
                var slot = Instantiate(slotPrefab, contentRoot);
                slot.Build(this, list[i], i);
            }

            if (goldText) goldText.text = GM.PersistentGameplayData.CurrentGold.ToString();
        }

        // เรียกจากปุ่ม Upgrade ในแต่ละช่อง
        public void TryUpgradeAtIndex(int cardIndex)
        {
            if (!GM || GM.PersistentGameplayData == null) return;

            var pgd = GM.PersistentGameplayData;
            var list = pgd.CurrentCardsList;
            if (cardIndex < 0 || cardIndex >= list.Count) return;

            var current = list[cardIndex];
            if (current == null || !current.CanUpgrade) return;

            var next = current.NextUpgrade;

            // ราคา base จาก GameplayData
            int baseCost = GM.GameplayData.GetUpgradeCostForStep(next.UpgradeStep);
            // ราคาสุทธิหลังส่วนลด (เช่น Relic ลด 20%)
            int finalCost = pgd.GetEffectiveCost(baseCost);

            if (pgd.CurrentGold < finalCost)
            {
                // TODO: แจ้งเตือนทองไม่พอ
                return;
            }

            pgd.CurrentGold -= finalCost;
            list[cardIndex] = next;

            Refresh(); // รีเฟรชหน้าจอหลังอัป
        }


        // helper: สร้างการ์ดแสดงในช่อง
        public CardBase BuildCardView(CardData data, Transform parent)
        {
            if (uiCardPrefab != null)
            {
                var uiCard = Instantiate(uiCardPrefab, parent);
                uiCard.SetCard(data, false);
                uiCard.UpdateCardText();
                return uiCard;
            }
            // fallback เดิม (ถ้ายังไม่ได้ตั้ง uiCardPrefab)
            var card = GameManager.Instance.BuildAndGetCard(data, parent);
            card.UpdateCardText();
            return card;
        }
        public void Open() => OpenCanvas();
        public void Close() => CloseCanvas();
    }
}
