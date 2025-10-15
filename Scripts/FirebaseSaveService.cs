using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Data.Settings;
using NueGames.NueDeck.Scripts.Characters; // AllyHealthData
using NueGames.NueDeck.Scripts.Enums;      // RelicType


public class FirebaseSaveService : MonoBehaviour
{
    public static FirebaseSaveService Instance { get; private set; }

    [Header("Firebase RTDB")]
    [SerializeField] private string realtimeDbUrl = "https://game6-4ad69-default-rtdb.asia-southeast1.firebasedatabase.app";

    [Header("Debug")]
    [SerializeField] private bool logDebug = true;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------- รูปแบบข้อมูลเซฟ ----------
    [Serializable]
    public class SaveSlotData
    {
        public int slotIndex;
        public string sceneContext; // "Map" หรือ "Combat"
        public long savedAt;
        public string appVersion;

        public int currentStageId;
        public int currentEncounterId;
        public bool isFinalEncounter;

        public int gold;
        public int maxMana;

        public string[] deckCardNames;      // ใช้ชื่อ asset (cardData.name)
        public string[] allyIds;            // CharacterID ของพันธมิตร
        public List<AllyHealthData> allyHealth; // ใช้ struct ที่เกมคุณมีอยู่แล้ว

        public string[] relics;             // ชื่อ enum RelicType
        public RunProgress[] runProgress;   // คัดลอกค่ารันจริง ๆ

        // ถ้าเซฟจาก Combat → ให้ Map กลับมาเริ่มไฟต์นี้ใหม่ (เราเก็บค่า currentEncounter ไว้เหมือนเดิม)
        // ไม่ต้องใส่ flag เพิ่มก็ทำงานตามต้องการอยู่แล้ว
    }

    [Serializable]
    public class SlotMeta
    {
        public int slotIndex;
        public long savedAt;
        public string sceneContext;
        public int stage;
        public int enc;
    }

    // ---------- API หลัก: Save / Load ----------
    public async Task<bool> SaveSlotAsync(int slot, string sceneContext)
    {
        var (uid, token) = AuthOrNull();
        if (uid == null) return false;

        var gm = GameManager.Instance;
        var pgd = gm.PersistentGameplayData; // มีอยู่แล้วใน GameManager :contentReference[oaicite:3]{index=3}

        var data = new SaveSlotData
        {
            slotIndex = slot,
            sceneContext = sceneContext,
            savedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            appVersion = Application.version,

            currentStageId = pgd.CurrentStageId,
            currentEncounterId = pgd.CurrentEncounterId,
            isFinalEncounter = pgd.IsFinalEncounter,

            gold = pgd.CurrentGold,
            maxMana = pgd.MaxMana,

            deckCardNames = pgd.CurrentCardsList.Where(c => c != null).Select(c => c.name).ToArray(), // ใช้ชื่อ asset เป็น ID
            allyIds = pgd.AllyList.Where(a => a != null && a.AllyCharacterData != null).Select(a => a.AllyCharacterData.CharacterID).ToArray(), // ใช้ CharacterID :contentReference[oaicite:4]{index=4}
            allyHealth = new List<AllyHealthData>(pgd.AllyHealthDataList),

            relics = pgd.ExportOwnedRelics().Select(r => r.ToString()).ToArray(),
            runProgress = pgd.ExportRunProgressList().ToArray()
        };

        var json = JsonUtility.ToJson(data);
        string path = $"{realtimeDbUrl}/saves/{uid}/{slot}.json?auth={token}";
        bool ok = await HttpPut(path, json);
        if (ok && logDebug) Debug.Log($"[Save] slot {slot} OK");

        // meta
        var meta = new SlotMeta { slotIndex = slot, savedAt = data.savedAt, sceneContext = data.sceneContext, stage = data.currentStageId, enc = data.currentEncounterId };
        await HttpPut($"{realtimeDbUrl}/saves/{uid}/_meta/{slot}.json?auth={token}", JsonUtility.ToJson(meta));
        return ok;
    }

    public async Task<SaveSlotData> LoadSlotAsync(int slot)
    {
        var (uid, token) = AuthOrNull();
        if (uid == null) return null;

        string path = $"{realtimeDbUrl}/saves/{uid}/{slot}.json?auth={token}";
        var txt = await HttpGet(path);
        if (string.IsNullOrEmpty(txt) || txt == "null") return null;

        var data = JsonUtility.FromJson<SaveSlotData>(txt);
        ApplyToGame(data);
        return data;
    }

    public async Task<SlotMeta[]> ListMetaAsync()
    {
        var (uid, token) = AuthOrNull();
        if (uid == null) return null;

        var result = new SlotMeta[3];
        for (int i = 0; i < 3; i++)
        {
            var t = await HttpGet($"{realtimeDbUrl}/saves/{uid}/_meta/{i}.json?auth={token}");
            result[i] = string.IsNullOrEmpty(t) || t == "null" ? null : JsonUtility.FromJson<SlotMeta>(t);
        }
        return result;
    }

    // ---------- แปลงข้อมูลเซฟกลับเข้าเกม ----------
    private void ApplyToGame(SaveSlotData d)
    {
        var gm = GameManager.Instance;
        var pgd = gm.PersistentGameplayData; // มี getter แล้ว :contentReference[oaicite:5]{index=5}

        // progression
        pgd.CurrentStageId = d.currentStageId;
        pgd.CurrentEncounterId = d.currentEncounterId;
        pgd.IsFinalEncounter = d.isFinalEncounter;

        // economy/stats
        pgd.CurrentGold = d.gold;
        pgd.MaxMana = d.maxMana;

        // deck: map จากชื่อ asset -> CardData ของจริง
        var all = gm.GameplayData; // ใช้คลังการ์ดจาก GameplayData (คุณใช้อยู่แล้วตอนแจกมือแรก) :contentReference[oaicite:6]{index=6}
        var allCards = all.AllCardsList;   // (อ้างอิงจากที่คุณใช้ set initial deck อยู่แล้ว)
        pgd.CurrentCardsList = new List<CardData>();
        foreach (var name in d.deckCardNames ?? Array.Empty<string>())
        {
            var cd = allCards.FirstOrDefault(c => c != null && c.name == name);
            if (cd != null) pgd.CurrentCardsList.Add(cd);
        }

        // allies: โครงพันธมิตรใช้จาก GameplayData.InitalAllyList อยู่แล้ว, เราอัปเดต HP ตามเซฟ
        // (เกมคุณมี method SetAllyHealthData แล้ว) :contentReference[oaicite:7]{index=7}
        foreach (var a in d.allyHealth ?? new List<AllyHealthData>())
            pgd.SetAllyHealthData(a.CharacterId, a.CurrentHealth, a.MaxHealth);

        // relics & run-progress
        var relicEnums = new List<RelicType>();
        if (d.relics != null)
            foreach (var r in d.relics)
                if (Enum.TryParse<RelicType>(r, out var e)) relicEnums.Add(e);
        pgd.ImportOwnedRelics(relicEnums);
        pgd.ImportRunProgressList(d.runProgress != null ? new List<RunProgress>(d.runProgress) : new List<RunProgress>());

        if (logDebug) Debug.Log("[Save] applied to game");
    }

    // ---------- REST helpers ----------
    private async Task<bool> HttpPut(string url, string json)
    {
        var req = new UnityWebRequest(url, "PUT");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        var op = req.SendWebRequest(); while (!op.isDone) await Task.Yield();
#if UNITY_2020_1_OR_NEWER
        return req.result == UnityWebRequest.Result.Success;
#else
        return !(req.isNetworkError || req.isHttpError);
#endif
    }
    private async Task<string> HttpGet(string url)
    {
        var req = UnityWebRequest.Get(url);
        var op = req.SendWebRequest(); while (!op.isDone) await Task.Yield();
#if UNITY_2020_1_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success) return null;
#else
        if (req.isNetworkError || req.isHttpError) return null;
#endif
        return req.downloadHandler.text;
    }

    private (string uid, string token) AuthOrNull()
    {
        var a = FirebaseAuthService.Instance;
        if (a == null || string.IsNullOrEmpty(a.UserId) || string.IsNullOrEmpty(a.IdToken))
        { Debug.LogWarning("[Save] not logged in"); return default; }
        return (a.UserId, a.IdToken);
    }
}
