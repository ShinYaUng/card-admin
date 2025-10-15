using NueGames.NueDeck.Scripts.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuAccountBinder : MonoBehaviour
{
    [SerializeField] private TMP_Text signedInText;   // ลาก SignedInText
    [SerializeField] private GameObject logoutButton; // ลาก Btn_Logout

    void Update()
    {
        var gm = GameManager.Instance;
        if (signedInText)
            signedInText.text = gm != null && gm.IsLoggedIn
                ? $"Signed in as: {gm.CurrentUserDisplayName}"
                : "Not signed in";

        if (logoutButton)
            logoutButton.SetActive(gm != null && gm.IsLoggedIn);
    }
}
