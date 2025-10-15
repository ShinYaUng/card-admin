using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;

namespace NueGames.NueDeck.Scripts.UI.Reward
{
    public class RelicChoiceItem : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI desc;
        [SerializeField] private Button pickButton;

        private RelicData data;

        public void Build(RelicData relic, System.Action onPicked)
        {
            data = relic;
            icon.sprite = relic.Icon;
            title.text = relic.RelicName;
            desc.text = relic.Description;
            pickButton.onClick.RemoveAllListeners();
            pickButton.onClick.AddListener(() =>
            {
                GameManager.Instance.PersistentGameplayData.AddRelic(relic.RelicType);
                onPicked?.Invoke();
            });
        }
    }
}