using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Data.Collection;

namespace NueGames.NueDeck.Scripts.UI.Shop
{
    public class ShopOfferSlot : MonoBehaviour
    {
        [SerializeField] private CardBase cardPrefab;     // �� CardUI/3D ���á���
        [SerializeField] private Transform cardRoot;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button buyButton;

        private CardData _card;
        private int _price;
        private ShopCanvas _shop;

        public void Setup(CardData card, int price, ShopCanvas shop)
        {
            _shop = shop;
            _card = card;
            _price = price;

            foreach (Transform c in cardRoot) Destroy(c.gameObject);
            var cardView = Instantiate(cardPrefab, cardRoot);
            cardView.SetCard(card, isPlayable: false);
            cardView.UpdateCardText();
            // ����������: �Դ hover/drag �ͧ CardBase (��ͧ�� SetPreviewMode � CardUI.cs)
            if (cardView is CardUI ui)
                ui.SetPreviewMode(true);

            // ��� CanvasGroup �������� ���ǻԴ�Ѻ�Թ�ص�ҡ���� (����������� Buy �Ѻ᷹)
            var root = cardView.gameObject;
            var cg = root.GetComponent<CanvasGroup>();
            if (cg == null) cg = root.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;

            // �ѹ���/���˹����¹�������ҵ� ��кѧ�Ѻ����բ�Ҵ��� CardRoot
            var containerRT = cardRoot as RectTransform;
            var cardRT = cardView.transform as RectTransform;
            if (containerRT != null && cardRT != null)
            {
                if (containerRT.rect.width < 10f || containerRT.rect.height < 10f)
                {
                    containerRT.anchorMin = containerRT.anchorMax = new Vector2(0.5f, 0.5f);
                    containerRT.pivot = new Vector2(0.5f, 0.5f);
                    containerRT.sizeDelta = new Vector2(320f, 500f);
                }
                cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                cardRT.pivot = new Vector2(0.5f, 0.5f);
                cardRT.sizeDelta = containerRT.rect.size;
                cardRT.localScale = Vector3.one;
                cardRT.anchoredPosition = Vector2.zero;
            }

            priceText.text = _price.ToString();
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(Buy);
        }

        private void Buy()
        {
            _shop.TryPurchase(_card, _price, this);
        }
    }
}
