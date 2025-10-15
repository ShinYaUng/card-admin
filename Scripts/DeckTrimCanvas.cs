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
        [SerializeField] private int minDeckSize = 14;   // �礢�鹵��
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

            // ��ԡ����ź�͡�ҡ��
            var btn = card.GetComponent<Button>();
            if (btn == null) btn = card.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                // ����礶֧/��ӡ��Ң�鹵������ ����ź
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

            // ��ͤ������
            titleText.text = over > 0
                ? $"Your deck has {count} cards.\nRemove {over} card(s) to continue."
                : $"Your deck has {count} cards.\nMinimum is {minDeckSize}. You can't remove more.";

            // ���� Done �������������Թ��鹵�� (��� trim ��������)
            doneButton.interactable = over == 0;
            doneButton.onClick.RemoveAllListeners();
            doneButton.onClick.AddListener(() => { if (over == 0) Close(); });

            // �Դ��������ö㹡�� �ź���촔 ����Ͷ֧��鹵��
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
