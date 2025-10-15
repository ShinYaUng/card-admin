using NueGames.NueDeck.Scripts.Managers;
using TMPro;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.UI
{
    public class InformationCanvas : CanvasBase
    {
        [Header("Settings")]
        [SerializeField] private GameObject randomizedDeckObject;
        [SerializeField] private TextMeshProUGUI roomTextField;
        [SerializeField] private TextMeshProUGUI goldTextField;
        [SerializeField] private TextMeshProUGUI nameTextField;
        [SerializeField] private TextMeshProUGUI healthTextField;

        public GameObject RandomizedDeckObject => randomizedDeckObject;
        public TextMeshProUGUI RoomTextField => roomTextField;
        public TextMeshProUGUI GoldTextField => goldTextField;
        public TextMeshProUGUI NameTextField => nameTextField;
        public TextMeshProUGUI HealthTextField => healthTextField;


        #region Setup
        private void Awake()
        {
            ResetCanvas();
        }
        #endregion

        #region Public Methods
        public void SetRoomText(int roomNumber, bool useStage = false, int stageNumber = -1) =>
            RoomTextField.text = useStage ? $"Room {stageNumber}/{roomNumber}" : $"Room {roomNumber}";

        public void SetGoldText(int value) => GoldTextField.text = $"{value}";

        public void SetNameText(string name) => NameTextField.text = $"{name}";

        public void SetHealthText(int currentHealth, int maxHealth) => HealthTextField.text = $"{currentHealth}/{maxHealth}";

        private (int cur, int max) GetHealthForDisplay()
        {
            var pgd = GameManager.PersistentGameplayData;
            var ally = pgd.AllyList[0]; // ผู้เล่นหลัก
            var cd = ally.AllyCharacterData;
            var rp = pgd.GetProgress(cd.CharacterID);              // run-only progress
            int desiredMax = cd.MaxHealth + rp.AddedHealth;        // Max จาก SO + แต้มอัป

            // ถ้ามีค่าที่เซฟไว้ ก็ใช้ current/max จากนั้น (แต่ clamp ไม่เกิน desiredMax)
            var saved = pgd.AllyHealthDataList.Find(x => x.CharacterId == cd.CharacterID);
            int cur = saved != null ? Mathf.Min(saved.CurrentHealth, desiredMax) : desiredMax;

            return (cur, desiredMax);
        }

        public void RefreshHealthFromProgress()
        {
            var (cur, max) = GetHealthForDisplay();
            SetHealthText(cur, max);
        }

        public override void ResetCanvas()
        {
            RandomizedDeckObject.SetActive(GameManager.PersistentGameplayData.IsRandomHand);
            // ❌ เดิม: ใช้ SO.MaxHealth ทั้งคู่
            // SetHealthText(GameManager.PersistentGameplayData.AllyList[0].AllyCharacterData.MaxHealth, ...);

            // ✅ ใหม่: ดึงจาก run progress + ค่าที่เซฟไว้
            var (cur, max) = GetHealthForDisplay();
            SetHealthText(cur, max);

            SetNameText(GameManager.GameplayData.DefaultName);
            SetRoomText(GameManager.PersistentGameplayData.CurrentEncounterId + 1,
                        GameManager.GameplayData.UseStageSystem,
                        GameManager.PersistentGameplayData.CurrentStageId + 1);
            UIManager.InformationCanvas.SetGoldText(GameManager.PersistentGameplayData.CurrentGold);
        }
        #endregion

    }
}