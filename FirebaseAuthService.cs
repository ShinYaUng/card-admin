using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseAuthService : MonoBehaviour
{
    public static FirebaseAuthService Instance { get; private set; }

    [Header("Firebase")]
    [SerializeField] private string webApiKey = "AIzaSyB08qFZomwnW3ODycCNBx75TJzifaUXses"; // Project settings → Web API key
    [SerializeField] private string realtimeDbUrl = "https://game6-4ad69-default-rtdb.asia-southeast1.firebasedatabase.app"; // RTDB URL
    [SerializeField] private string pseudoEmailDomain = "user.local"; // อีเมลหลอก

    [System.NonSerialized] public string IdToken;
    [System.NonSerialized] public string UserId;
    [System.NonSerialized] public string Username; // ใช้แสดงผลแทน email

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------- Helpers ----------
    private static async Task<string> PostJson(string url, string json)
    {
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        var op = req.SendWebRequest(); while (!op.isDone) await Task.Yield();
#if UNITY_2020_1_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success) { Debug.LogError(req.error + " " + req.downloadHandler.text); return null; }
#else
        if (req.isNetworkError || req.isHttpError) { Debug.LogError(req.error + " " + req.downloadHandler.text); return null; }
#endif
        return req.downloadHandler.text;
    }

    private string ToPseudoEmail(string uname) => $"{uname.Trim().ToLower()}@{pseudoEmailDomain}";

    [System.Serializable] private class AuthRes { public string idToken, localId, displayName; }

    // ---------- SIGN UP ----------
    public async Task<bool> SignUpWithUsernamePassword(string username, string password)
    {
        var uname = username.Trim().ToLower();
        // 1) สมัครด้วย Email/Password (อีเมลหลอก)
        string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={webApiKey}";
        string body = $"{{\"email\":\"{ToPseudoEmail(uname)}\",\"password\":\"{password}\",\"returnSecureToken\":true}}";
        var resText = await PostJson(url, body);
        if (string.IsNullOrEmpty(resText)) return false;

        var res = JsonUtility.FromJson<AuthRes>(resText);
        IdToken = res.idToken; UserId = res.localId; Username = uname;

        // 2) ตั้ง displayName = username (ออปชัน)
        string urlUpdate = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={webApiKey}";
        await PostJson(urlUpdate, $"{{\"idToken\":\"{IdToken}\",\"displayName\":\"{uname}\",\"returnSecureToken\":true}}");

        // 3) จองชื่อใน RTDB: /usernames/{uname} -> { uid }
        var mapUrl = $"{realtimeDbUrl}/usernames/{uname}.json?auth={IdToken}";
        var put = new UnityWebRequest(mapUrl, "PUT");
        put.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes($"{{\"uid\":\"{UserId}\"}}"));
        put.downloadHandler = new DownloadHandlerBuffer();
        put.SetRequestHeader("Content-Type", "application/json");
        var op = put.SendWebRequest(); while (!op.isDone) await Task.Yield();

#if UNITY_2020_1_OR_NEWER
        if (put.result != UnityWebRequest.Result.Success)
#else
        if (put.isNetworkError || put.isHttpError)
#endif
        {
            Debug.LogWarning("Username already taken by rule.");
            // (ออปชัน) ยกเลิกบัญชีที่เพิ่งสมัคร หากชื่อซ้ำ
            await PostJson($"https://identitytoolkit.googleapis.com/v1/accounts:delete?key={webApiKey}", $"{{\"idToken\":\"{IdToken}\"}}");
            IdToken = null; UserId = null; Username = null;
            return false;
        }

        // 4) โปรไฟล์สั้น ๆ
        var profUrl = $"{realtimeDbUrl}/users/{UserId}.json?auth={IdToken}";
        var profReq = new UnityWebRequest(profUrl, "PUT");
        profReq.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(
            $"{{\"username\":\"{uname}\",\"createdAt\":{System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()} }}"));
        profReq.downloadHandler = new DownloadHandlerBuffer();
        profReq.SetRequestHeader("Content-Type", "application/json");
        var op2 = profReq.SendWebRequest(); while (!op2.isDone) await Task.Yield();

        return true;
    }

    // ---------- SIGN IN ----------
    public async Task<bool> SignInWithUsernamePassword(string username, string password)
    {
        var uname = username.Trim().ToLower();
        string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={webApiKey}";
        string body = $"{{\"email\":\"{ToPseudoEmail(uname)}\",\"password\":\"{password}\",\"returnSecureToken\":true}}";
        var resText = await PostJson(url, body);
        if (string.IsNullOrEmpty(resText)) return false;

        var res = JsonUtility.FromJson<AuthRes>(resText);
        IdToken = res.idToken; UserId = res.localId; Username = uname;
        return true;
    }

    // ---------- CHANGE PASSWORD (เฉพาะตอนล็อกอินอยู่) ----------
    public async Task<bool> ChangePassword(string newPassword)
    {
        if (string.IsNullOrEmpty(IdToken)) return false;
        string url = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={webApiKey}";
        string body = $"{{\"idToken\":\"{IdToken}\",\"password\":\"{newPassword}\",\"returnSecureToken\":true}}";
        var res = await PostJson(url, body);
        return !string.IsNullOrEmpty(res);
    }

    public void SignOutLocal() { IdToken = null; UserId = null; Username = null; }
}
