using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class AuthCanvas : CanvasBase
{
    [SerializeField] private CanvasGroup rootGroup; // <- ลาก CanvasGroup ของแผงนี้มาไว้
    // Header โชว์ชื่อผู้ใช้ในหน้าเปลี่ยนรหัส
    [SerializeField] private TMP_Text changePwUserLabel;

    // ออปชัน: ให้ผู้ใช้กรอกรหัสเดิมเพื่อยืนยันก่อนเปลี่ยน
    [SerializeField] private TMP_InputField currentPasswordInput;

    [Header("Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject signupPanel;
    [SerializeField] private GameObject changePwPanel;

    [Header("Login")]
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private TMP_Text loginMessage;

    [Header("Login → Account view")]
    [SerializeField] private GameObject loginInputsRoot;   // กลุ่ม Username/Password + ปุ่ม Login/Sign up
    [SerializeField] private GameObject accountInfoRoot;   // กลุ่มแสดงสถานะบัญชี + ปุ่ม Change/Sign out
    [SerializeField] private TMP_Text accountInfoText;     // ข้อความ "Signed in as ..."

    [Header("Signup")]
    [SerializeField] private TMP_InputField signupUsernameInput;
    [SerializeField] private TMP_InputField signupPasswordInput;
    [SerializeField] private TMP_InputField signupPasswordConfirmInput;
    [SerializeField] private TMP_Text signupMessage;

    [Header("Change Password")]
    [SerializeField] private TMP_InputField newPasswordInput;
    [SerializeField] private TMP_InputField newPasswordConfirmInput;
    [SerializeField] private TMP_Text changePwMessage;

    [Header("Main Menu Header")]
    [SerializeField] private TMP_Text signedInLabel; // ข้อความบอกว่าใครกำลังล็อกอิน
    public void OpenSelf()
    {
        // ถ้า UIManager พร้อม ใช้ flow เดิม
        if (UIManager.Instance != null) UIManager.Instance.SetCanvas(this, true, true);

        // Fallback กันพลาด: บังคับให้คลิก/พิมพ์ได้แน่ๆ
        if (rootGroup == null) rootGroup = GetComponent<CanvasGroup>();
        gameObject.SetActive(true);
        if (rootGroup != null) { rootGroup.alpha = 1f; rootGroup.interactable = true; rootGroup.blocksRaycasts = true; }

        ShowLogin();                    // เปิดหน้า Login
        RefreshLoginPanelState();       // สลับ Login ↔ Account ตามสถานะ
        loginUsernameInput?.ActivateInputField(); // โฟกัสช่องแรก
    }
    public void CloseSelf()
    {
        if (UIManager.Instance != null) UIManager.Instance.SetCanvas(this, false, false);

        if (rootGroup == null) rootGroup = GetComponent<CanvasGroup>();
        if (rootGroup != null) { rootGroup.alpha = 0f; rootGroup.interactable = false; rootGroup.blocksRaycasts = false; }
        gameObject.SetActive(false);
    }
    public override void ResetCanvas()
    {
        base.ResetCanvas();
        ShowLogin();
        loginUsernameInput.text = loginPasswordInput.text = "";
        signupUsernameInput.text = signupPasswordInput.text = signupPasswordConfirmInput.text = "";
        newPasswordInput.text = newPasswordConfirmInput.text = "";
        loginMessage.text = signupMessage.text = changePwMessage.text = "";
        RefreshSignedInLabel();
        RefreshLoginPanelState(); // <-- เพิ่มบรรทัดนี้
    }

    void ShowLogin()
    {
        loginPanel.SetActive(true);    // เหลือส่วนอื่นไว้ตามเดิม
        signupPanel.SetActive(false);
        changePwPanel.SetActive(false);

        // กันล๊อคไว้เผื่อไปตั้งค่าผิด
        loginUsernameInput.interactable = true;
        loginPasswordInput.interactable = true;
        loginUsernameInput.readOnly = false;
        loginPasswordInput.readOnly = false;

        // โฟกัสให้ทันที
        EventSystem.current?.SetSelectedGameObject(loginUsernameInput.gameObject);
    }
    void ShowSignup() { loginPanel.SetActive(false); signupPanel.SetActive(true); changePwPanel.SetActive(false); }
    void ShowChangePw() { loginPanel.SetActive(false); signupPanel.SetActive(false); changePwPanel.SetActive(true); }
    private void RefreshLoginPanelState()
    {
        bool loggedIn = GameManager.Instance.IsLoggedIn;

        // ถ้าล็อกอินแล้ว → ซ่อนช่องกรอก login, โชว์มุมมองบัญชี
        if (loginInputsRoot) loginInputsRoot.SetActive(!loggedIn);
        if (accountInfoRoot) accountInfoRoot.SetActive(loggedIn);
        if (accountInfoText) accountInfoText.text = loggedIn
            ? $"Signed in as: {GameManager.Instance.CurrentUserDisplayName}"
            : "";
    }
    public void OnClickGoSignup() => ShowSignup();
    public void OnClickGoLogin()
    {
        ShowLogin();
        RefreshLoginPanelState(); // <-- เพิ่มบรรทัดนี้
    }
    public void OnClickGoChangePw()
    {
        if (!GameManager.Instance.IsLoggedIn)
        {
            // ยังไม่ล็อกอิน -> ให้กลับไปหน้า Login
            loginMessage.text = "Login first";
            ShowLogin();
            return;
        }
        // โชว์ชื่อผู้ใช้ปัจจุบันบนหัวข้อ
        if (changePwUserLabel != null)
            changePwUserLabel.text = $"Change password for: {GameManager.Instance.CurrentUserDisplayName}";
        ShowChangePw();
    }

    public async void OnClickLogin()
    {
        loginMessage.text = "Signing in...";
        var ok = await FirebaseAuthService.Instance.SignInWithUsernamePassword(loginUsernameInput.text, loginPasswordInput.text);
        loginMessage.text = ok ? "Login success" : "User or password are wrong";
        if (ok)
        {
            GameManager.Instance.SetCurrentUser(FirebaseAuthService.Instance.UserId,
                                                FirebaseAuthService.Instance.Username,
                                                FirebaseAuthService.Instance.Username);
            RefreshSignedInLabel();
            UIManager.Instance.SetCanvas(this, false);
        }
    }

    public async void OnClickSignup()
    {
        if (signupPasswordInput.text != signupPasswordConfirmInput.text)
        { signupMessage.text = "Password not match"; return; }

        signupMessage.text = "Signing up";
        var ok = await FirebaseAuthService.Instance.SignUpWithUsernamePassword(signupUsernameInput.text, signupPasswordInput.text);
        signupMessage.text = ok ? "Sign-up success" : "Sign-up unsuccess";
    }

    public async void OnClickChangePassword()
    {
        if (!GameManager.Instance.IsLoggedIn)
        {
            changePwMessage.text = "Login first";
            ShowLogin();
            return;
        }

        if (newPasswordInput.text != newPasswordConfirmInput.text)
        {
            changePwMessage.text = "Password not match";
            return;
        }

        changePwMessage.text = "Changing";
        var ok = await FirebaseAuthService.Instance.ChangePassword(newPasswordInput.text);
        changePwMessage.text = ok ? "Changecomplete" : "can not change";
    }

    public void OnClickSignOut()
    {
        // 1) เคลียร์สถานะการล็อกอิน
        FirebaseAuthService.Instance.SignOutLocal();
        GameManager.Instance.ClearCurrentUser();

        // 2) อัปเดตหัวเมนู (signedInLabel) และสลับหน้า Login/Account
        RefreshSignedInLabel();
        RefreshLoginPanelState();             // <-- สำคัญ: จะซ่อน accountInfoRoot และเคลียร์ข้อความ

        // (ออปชัน) เคลียร์ข้อความสถานะต่าง ๆ
        if (loginMessage) loginMessage.text = "";
        if (signupMessage) signupMessage.text = "";
        if (changePwMessage) changePwMessage.text = "";

        // 3) ปิดหน้าต่าง AuthCanvas ทันที
        CloseSelf();                          // ใช้เมธอดที่มี fallback ให้แล้ว
    }

    private void RefreshSignedInLabel()
    {
        var gm = GameManager.Instance;
        if (signedInLabel != null)
            signedInLabel.text = string.IsNullOrEmpty(gm.CurrentUserDisplayName)
                ? "Not signed in"
                : $"Signed in as: {gm.CurrentUserDisplayName}";
    }

    // ทำให้เรียกอัปเดตหัวข้อผู้ใช้จากภายนอกได้ (เช่น ตอนกด SignOut จากปุ่มเมนู)
    public void RefreshHeaderPublic()
    {
        // ถ้า method เดิมชื่อ RefreshSignedInLabel เป็น private ให้เรียกที่นี่
        // หรือย้ายให้เป็น public ได้เลยก็ได้
        var gm = GameManager.Instance;
        if (signedInLabel != null)
            signedInLabel.text = string.IsNullOrEmpty(gm.CurrentUserDisplayName)
                ? "Not signed in"
                : $"Signed in as: {gm.CurrentUserDisplayName}";
    }

    public void OpenChangePwDirect()
    {
        OpenSelf();       // เปิดแคนวาสก่อน
        OnClickGoChangePw(); // แล้วเด้งเข้าแท็บเปลี่ยนรหัส (จะเช็คล็อกอินให้อัตโนมัติ)
    }
}
