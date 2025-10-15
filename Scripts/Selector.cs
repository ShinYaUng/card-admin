using UnityEngine;
using UnityEngine.SceneManagement;
using NueGames.NueDeck.Scripts.Data.Settings;
using NueGames.NueDeck.Scripts.Managers;

public class GameplayDataSelector : MonoBehaviour
{
    [SerializeField] private GameplayData[] gameplayDataOptions;


    public void SelectGameplayData(int index)
    {
        if (index < 0 || index >= gameplayDataOptions.Length) return;

        GameManager.Instance.SetGameplayData(gameplayDataOptions[index]);

    }
}
