using UnityEngine;
using UnityEngine.UI;
using NueGames.NueDeck.Scripts.Data.Characters;
using NueGames.NueDeck.Scripts.Data.Settings;
using NueGames.NueDeck.Scripts.Managers;

public class LevelUpUI : MonoBehaviour
{
    [SerializeField] private AllyCharacterData allyData;

    [Header("Buttons")]
    [SerializeField] private Button healthButton;
    [SerializeField] private Button armorButton;
    [SerializeField] private Button energyButton;
    [SerializeField] private Button attackButton;

    [Header("UI Texts")]
    [SerializeField] private Text levelText;
    [SerializeField] private Text expText;
    [SerializeField] private Text pointsText;

    private PersistentGameplayData PGD => GameManager.Instance.PersistentGameplayData;

    private void Start()
    {
        healthButton.onClick.AddListener(() => Upgrade(RunStatType.Health));
        armorButton.onClick.AddListener(() => Upgrade(RunStatType.Armor));
        energyButton.onClick.AddListener(() => Upgrade(RunStatType.Energy));
        attackButton.onClick.AddListener(() => Upgrade(RunStatType.Attack));

        RefreshUI();
    }

    private void Upgrade(RunStatType stat)
    {
        if (PGD.SpendPoint(allyData.CharacterID, stat))
        {
            var rp = PGD.GetProgress(allyData.CharacterID);

            // อัปเดต MaxMana (เดิม)
            PGD.MaxMana = GameManager.Instance.GameplayData.MaxMana + rp.AddedEnergy;

            // 🟢 อัปเดตเลขเลือดที่เก็บไว้ทันที (ให้เห็นผลไฟต์ถัดไปโดยไม่ดีเลย์)
            int desiredMax = allyData.MaxHealth + rp.AddedHealth;

            // ถ้ามีค่า current ที่บันทึกไว้ ให้ “บวกตาม delta” เล็กน้อย (เช่น +10 เมื่ออัปเลือด)
            // หรือจะไม่ heal ก็ได้ ตามดีไซน์ของเกม — ตัวอย่างนี้ heal เท่าที่เพิ่ม
            var saved = PGD.AllyHealthDataList.Find(x => x.CharacterId == allyData.CharacterID);
            int cur = saved != null ? Mathf.Min(saved.CurrentHealth + 10, desiredMax) : desiredMax;

            PGD.SetAllyHealthData(allyData.CharacterID, cur, desiredMax);  // ← เซฟลง PGD

            // รีเฟรชข้อความบนมุมซ้ายทันที
            if (UIManager.Instance != null && UIManager.Instance.InformationCanvas != null)
                UIManager.Instance.InformationCanvas.SetHealthText(cur, desiredMax);
            // ⬇️ เพิ่มหลังจากอัปเดตค่า PGD/HP/UI เสร็จ (ก่อนออกจาก if)

            var handController = CollectionManager.Instance?.HandController;
            if (handController != null && handController.hand != null)
            {
                foreach (var c in handController.hand)
                    c.UpdateCardText();   // รีเฟรช description ให้โชว์ดาเมจใหม่ (รวมแต้มโจมตีที่อัป)
            }
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        var rp = PGD.GetProgress(allyData.CharacterID);
        levelText.text = $"Level: {rp.Level}";
        expText.text = $"EXP: {rp.CurrentExp}/{rp.ExpToNextLevel}";
        pointsText.text = $"Points: {rp.AvailablePoints}";
    }
}
