using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.UI;
using UnityEngine;
using NueGames.NueDeck.Scripts.Managers;

namespace NueGames.NueDeck.Scripts.Managers
{
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private List<EncounterButton> encounterButtonList;

        public List<EncounterButton> EncounterButtonList => encounterButtonList;

        private GameManager GameManager => GameManager.Instance;

        private void Start()
        {
            PrepareEncounters();
        }

        public void OpenShopFromMap()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.OpenShop();   // เปิด ShopCanvas ที่อยู่ใน NueCore
        }

        public void OpenDeckManagerFromMap()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.OpenDeckManager();
        }

        private void PrepareEncounters()
        {
            for (int i = 0; i < EncounterButtonList.Count; i++)
            {
                var btn = EncounterButtonList[i];
                if (GameManager.PersistentGameplayData.CurrentEncounterId == i)
                    btn.SetStatus(EncounterButtonStatus.Active);
                else if (GameManager.PersistentGameplayData.CurrentEncounterId > i)
                    btn.SetStatus(EncounterButtonStatus.Completed);
                else
                    btn.SetStatus(EncounterButtonStatus.Passive);
            }
        }
    }
}
