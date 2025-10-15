using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.UI;  // << ��ͧ�� ������� CanvasBase

namespace NueGames.NueDeck.Scripts.UI   // �й�������� namespace ���ǡѺ UI ����
{
    public class DeckManagerCanvas : CanvasBase
    {
        [Header("Refs")]
        [SerializeField] private Transform contentRoot;      // Grid/Content
        [SerializeField] private UpgradeCardSlot slotPrefab; // Prefab �ͧ��ͧ
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI goldText;

        [SerializeField] private CardBase uiCardPrefab; // �����Ὼ CardUI �ç���

        private GameManager GM => GameManager.Instance;

        private void Awake()
        {
            if (closeButton) closeButton.onClick.AddListener(CloseCanvas);
        }

        // === �Ӥѭ: �ͧ�Ѻ UIManager.SetCanvas ===
        public override void ResetCanvas()
        {
            base.ResetCanvas();
            // ��ҧ��¡��
            if (contentRoot != null)
            {
                for (int i = contentRoot.childCount - 1; i >= 0; i--)
                    Destroy(contentRoot.GetChild(i).gameObject);
            }
            // �ѻവ�ͧ
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

        // === ��ҹ���� ===
        public void Refresh()
        {
            if (!GM || GM.PersistentGameplayData == null || contentRoot == null || slotPrefab == null) return;

            // ��ҧ���
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

        // ���¡�ҡ���� Upgrade ����Ъ�ͧ
        public void TryUpgradeAtIndex(int cardIndex)
        {
            if (!GM || GM.PersistentGameplayData == null) return;

            var pgd = GM.PersistentGameplayData;
            var list = pgd.CurrentCardsList;
            if (cardIndex < 0 || cardIndex >= list.Count) return;

            var current = list[cardIndex];
            if (current == null || !current.CanUpgrade) return;

            var next = current.NextUpgrade;

            // �Ҥ� base �ҡ GameplayData
            int baseCost = GM.GameplayData.GetUpgradeCostForStep(next.UpgradeStep);
            // �Ҥ��ط����ѧ��ǹŴ (�� Relic Ŵ 20%)
            int finalCost = pgd.GetEffectiveCost(baseCost);

            if (pgd.CurrentGold < finalCost)
            {
                // TODO: ����͹�ͧ����
                return;
            }

            pgd.CurrentGold -= finalCost;
            list[cardIndex] = next;

            Refresh(); // ���ê˹�Ҩ���ѧ�ѻ
        }


        // helper: ���ҧ�����ʴ�㹪�ͧ
        public CardBase BuildCardView(CardData data, Transform parent)
        {
            if (uiCardPrefab != null)
            {
                var uiCard = Instantiate(uiCardPrefab, parent);
                uiCard.SetCard(data, false);
                uiCard.UpdateCardText();
                return uiCard;
            }
            // fallback ��� (����ѧ������� uiCardPrefab)
            var card = GameManager.Instance.BuildAndGetCard(data, parent);
            card.UpdateCardText();
            return card;
        }
        public void Open() => OpenCanvas();
        public void Close() => CloseCanvas();
    }
}
