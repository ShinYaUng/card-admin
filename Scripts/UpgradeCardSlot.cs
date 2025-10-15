using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.UI;

namespace NueGames.NueDeck.Scripts.UI
{
    public class UpgradeCardSlot : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform cardHolder;             // �ش�ҧ CardPrefab
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;        // �ʴ� �Lv 1/3� 
        [SerializeField] private TextMeshProUGUI priceText;        // �ʴ��Ҥ��ѻ�Ѵ�
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Vector2 cardPreviewSize = new Vector2(260, 360);
        private DeckManagerCanvas _owner;
        private int _index;
        private CardData _data;
        private CardBase _cardView;

        private GameManager GM => GameManager.Instance;

        public void Build(DeckManagerCanvas owner, CardData data, int index)
        {
            _owner = owner;
            _data = data;
            _index = index;

            // ���ҧ card view ���� CardPrefab �ҵðҹ
            if (_cardView != null) Destroy(_cardView.gameObject);
            _cardView = owner.BuildCardView(_data, cardHolder);  // �� set ����/�ٻ/desc/mana ����ͧ
                                                                 // ��ѧ _cardView = GM.BuildAndGetCard(_data, cardHolder);
            _cardView.UpdateCardText();

            // >>> �ѧ�Ѻ��Ҵ/����������㹡�ͺ preview (�ͧ�Ѻ��� CardUI ��� Card3D)
            var rt = _cardView.GetComponent<RectTransform>()
                     ?? _cardView.GetComponentInChildren<RectTransform>(true);

            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = cardPreviewSize; // �� 260x360
            }
            else
            {
                // �ó��� 3D ��ǹ � ����� RectTransform ��� � ���´��� localScale �ç �
                _cardView.transform.localScale = Vector3.one * 100f;
            }

            if (nameText) nameText.text = _data.CardName;
            if (levelText) levelText.text = $"Lv {_data.UpgradeStep}/3";

            // �Ҥ��ѻ�ͧ ��дѺ�Ѵ仔
            if (_data.CanUpgrade)
            {
                int baseCost = GM.GameplayData.GetUpgradeCostForStep(_data.NextUpgrade.UpgradeStep);
                int finalCost = GM.PersistentGameplayData.GetEffectiveCost(baseCost);

                if (priceText) priceText.text = finalCost.ToString();
                if (upgradeButton)
                {
                    upgradeButton.interactable = true;
                    upgradeButton.onClick.RemoveAllListeners();
                    upgradeButton.onClick.AddListener(() => _owner.TryUpgradeAtIndex(_index));
                }
            }
            else
            {
                if (priceText) priceText.text = "-";
                if (upgradeButton) upgradeButton.interactable = false;
            }


            Debug.Log($"[DeckPreview] {_data.CardName} size={(rt ? rt.sizeDelta.ToString() : "no-rt")}, scale={_cardView.transform.localScale}");
        }
    }
}