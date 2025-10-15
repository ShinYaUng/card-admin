using System.Collections;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.UI;
using NueGames.NueDeck.Scripts.UI.Reward;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NueGames.NueDeck.Scripts.Managers
{
    [DefaultExecutionOrder(-4)]
    public class UIManager : MonoBehaviour
    {
        public UIManager() { }
        public static UIManager Instance { get; private set; }

        [SerializeField] private ShopCanvas shopCanvas;
        [SerializeField] private DeckTrimCanvas deckTrimCanvas;

        [Header("Canvases")]
        [SerializeField] private CombatCanvas combatCanvas;
        [SerializeField] private InformationCanvas informationCanvas;
        [SerializeField] private RewardCanvas rewardCanvas;
        [SerializeField] private InventoryCanvas inventoryCanvas;


        [Header("Fader")]
        [SerializeField] private CanvasGroup fader;
        [SerializeField] private float fadeSpeed = 1f;

        [SerializeField] private DeckManagerCanvas deckManagerCanvas;
        [SerializeField] private AuthCanvas authCanvas;

        #region Cache
        public DeckManagerCanvas DeckManagerCanvas => deckManagerCanvas;
        public CombatCanvas CombatCanvas => combatCanvas;
        public InformationCanvas InformationCanvas => informationCanvas;
        public RewardCanvas RewardCanvas => rewardCanvas;
        public InventoryCanvas InventoryCanvas => inventoryCanvas;
        public ShopCanvas ShopCanvas => shopCanvas;
        public DeckTrimCanvas DeckTrimCanvas => deckTrimCanvas;
        public void OpenAuth() => SetCanvas(authCanvas, true, true);
        public void CloseAuth() => SetCanvas(authCanvas, false, false);
        public void SignOut() => authCanvas?.OnClickSignOut();
        #endregion
        public void OpenShop() => SetCanvas(ShopCanvas, true, true);
        #region Setup
        private void Awake()
        {
            if (Instance == null)
            {
                transform.parent = null;
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

        }
        #endregion

        #region Public Methods

        public void OpenInventory(List<CardData> cardList, string title)
        {
            SetCanvas(InventoryCanvas, true, true);
            InventoryCanvas.ChangeTitle(title);
            InventoryCanvas.SetCards(cardList);
        }

        public void SetCanvas(CanvasBase targetCanvas, bool open, bool reset = false)
        {
            if (reset)
                targetCanvas.ResetCanvas();

            if (open)
                targetCanvas.OpenCanvas();
            else
                targetCanvas.CloseCanvas();
        }
        public void ChangeScene(int index)
        {
            StartCoroutine(ChangeSceneRoutine(index));
        }
        #endregion

        #region Routines
        private IEnumerator ChangeSceneRoutine(int index)
        {
            SceneManager.LoadScene(index);
            yield return StartCoroutine(Fade(false));
        }

        public IEnumerator Fade(bool isIn)
        {
            var waitFrame = new WaitForEndOfFrame();
            var timer = isIn ? 0f : 1f;

            while (true)
            {
                timer += Time.deltaTime * (isIn ? fadeSpeed : -fadeSpeed);

                fader.alpha = timer;

                if (timer >= 1f) break;

                yield return waitFrame;
            }
        }

        #endregion
        public void OpenDeckTrimIfOverLimit(int max = 14)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.PersistentGameplayData == null) return; // กัน null เผื่อเวลาทดสอบใน Editor
            var count = gm.PersistentGameplayData.CurrentCardsList.Count; // ✅
            if (count > max)
                SetCanvas(DeckTrimCanvas, true, true); // อ้างอิง DeckTrimCanvas ที่คุณลากเข้าช่องไว้
        }

        public void OpenDeckManager()
        {
            SetCanvas(deckManagerCanvas, true, true);
            deckManagerCanvas.Open();
        }
        public void CloseDeckManager()
        {
            deckManagerCanvas.Close();
            SetCanvas(deckManagerCanvas, false, true);
        }
    }
}
