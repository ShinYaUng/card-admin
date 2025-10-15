using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Data.Collection;

namespace NueGames.NueDeck.Scripts.UI
{
    public class DeckTrimCanvas : CanvasBase
    {
        [Header("UI")]
        [SerializeField] private Transform gridRoot;
        [SerializeField] private CardBase cardPrefab;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button doneButton;
        [Header("Rules")]
        [SerializeField] private int minDeckSize = 14;   // เด็คขั้นต่ำ
        private GameManager GM => GameManager.Instance;
        private List<CardData> _working;

        public override void OpenCanvas()
        {
            base.OpenCanvas();
            Build();
        }

        private void Build()
        {
            foreach (Transform c in gridRoot) Destroy(c.gameObject);
            _working = new List<CardData>(GM.PersistentGameplayData.CurrentCardsList);

            foreach (var cd in _working)
                AddCardItem(cd);

            UpdateHeaderAndButton();
        }

        private void AddCardItem(CardData cd)
        {
            var card = Instantiate(cardPrefab, gridRoot);
            card.SetCard(cd, isPlayable: false);
            card.UpdateCardText();

            // คลิกแล้วลบออกจากเด็ค
            var btn = card.GetComponent<Button>();
            if (btn == null) btn = card.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                // ถ้าเด็คถึง/ต่ำกว่าขั้นต่ำแล้ว ห้ามลบ
                if (GM.PersistentGameplayData.CurrentCardsList.Count <= minDeckSize)
                    return;

                GM.PersistentGameplayData.CurrentCardsList.Remove(cd);
                Destroy(card.gameObject);
                UpdateHeaderAndButton();
            });
        }

        private void UpdateHeaderAndButton()
        {
            int count = GM.PersistentGameplayData.CurrentCardsList.Count;
            int over = Mathf.Max(0, count - minDeckSize);

            // ข้อความหัว
            titleText.text = over > 0
                ? $"Your deck has {count} cards.\nRemove {over} card(s) to continue."
                : $"Your deck has {count} cards.\nMinimum is {minDeckSize}. You can't remove more.";

            // ปุ่ม Done กดได้เมื่อไม่เกินขั้นต่ำ (คือ trim เสร็จแล้ว)
            doneButton.interactable = over == 0;
            doneButton.onClick.RemoveAllListeners();
            doneButton.onClick.AddListener(() => { if (over == 0) Close(); });

            // ปิดความสามารถในการ “ลบการ์ด” เมื่อถึงขั้นต่ำ
            bool canRemove = count > minDeckSize;
            foreach (Transform child in gridRoot)
            {
                var b = child.GetComponent<Button>();
                if (b) b.interactable = canRemove;
            }
        }


        private void Close()
        {
            UIManager.Instance.SetCanvas(this, false);
        }
    }
}
