using System.Threading;
using NueGames.NueDeck.Scripts.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveLoadPanel : MonoBehaviour
{
    [SerializeField] CanvasGroup cg;
    [SerializeField] TMP_Text title;
    [SerializeField] Button[] slotButtons;   // size 3
    [SerializeField] TMP_Text[] slotLabels;  // size 3
    private string mode = "Save";            // "Save" | "Load"

    private SynchronizationContext unityCtx;

    void Awake()
    {
        if (!cg) cg = GetComponent<CanvasGroup>();
        unityCtx = SynchronizationContext.Current;   // <— จับเมนเธรดไว้
        Hide();
    }

    public async void Open(string mode)
    {
        this.mode = mode;

        // ส่วนเปิดหน้าต่าง ทำบนเมนเธรด (เรียกตรง ๆ ได้เพราะยังไม่ await)
        title.text = (mode == "Save") ? "Save Game" : "Load Game";
        gameObject.SetActive(true);
        if (cg) { cg.alpha = 1; cg.interactable = true; cg.blocksRaycasts = true; }

        // โหลดข้อมูลเซฟ (I/O) จะวิ่งเธรดไหนก็ได้
        var metas = await FirebaseSaveService.Instance.ListMetaAsync().ConfigureAwait(false);

        // กลับเมนเธรดก่อนอัพเดต UI/TMP/Button
        unityCtx.Post(_ =>
        {
            for (int i = 0; i < 3; i++)
            {
                var m = (metas != null && i < metas.Length) ? metas[i] : null;
                slotLabels[i].text = (m == null)
                    ? "EMPTY"
                    : $"{UnixToLocal(m.savedAt):yyyy-MM-dd HH:mm}\n{m.sceneContext}\nStage:{m.stage} Enc:{m.enc}";

                var idx = i;
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => OnClickSlot(idx));
                slotButtons[i].interactable = (mode == "Save") || (m != null);
            }
        }, null);
    }

    public void Hide()
    {
        if (cg) { cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false; }
        gameObject.SetActive(false);
    }

    private async void OnClickSlot(int slot)
    {
        if (mode == "Save")
        {
            var ctx = IsCombat() ? "Combat" : "Map";
            await FirebaseSaveService.Instance.SaveSlotAsync(slot, ctx).ConfigureAwait(false);

            // กลับเมนเธรดก่อนปิด UI
            unityCtx.Post(_ => Hide(), null);
        }
        else
        {
            var data = await FirebaseSaveService.Instance.LoadSlotAsync(slot).ConfigureAwait(false);

            // กลับเมนเธรดก่อนเปลี่ยนฉาก/ปิด UI
            unityCtx.Post(_ =>
            {
                if (data != null)
                {
                    var gm = GameManager.Instance;
                    SceneManager.LoadScene(gm.SceneData.mapSceneIndex);
                }
                Hide();
            }, null);
        }
    }

    private static System.DateTime UnixToLocal(long s)
        => System.DateTimeOffset.FromUnixTimeSeconds(s).ToLocalTime().DateTime;

    private bool IsCombat()
    {
        var s = SceneManager.GetActiveScene().buildIndex;
        return s == GameManager.Instance.SceneData.combatSceneIndex;
    }
}
