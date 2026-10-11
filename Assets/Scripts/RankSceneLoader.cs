using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// เมื่อเก็บเหรียญในแมพครบแล้ว รอสักครู่ จากนั้นพาไปฉาก RankScene
/// - ไม่ต้องวางเองก็ได้ ระบบสร้างให้ในฉากที่มี CoinPickup (ใช้ชื่อฉากเริ่มต้น "RankScene")
/// - ถ้าอยากเปลี่ยนชื่อฉากหรือเวลารอ ให้สร้าง GameObject ว่างแล้วใส่สคริปต์นี้เอง
/// - ฉาก RankScene ต้องอยู่ใน Build Profiles > Scene List ไม่งั้นโหลดไม่ได้
/// </summary>
public class RankSceneLoader : MonoBehaviour
{
    [Tooltip("ชื่อฉากปลายทาง ต้องตรงกับชื่อไฟล์ฉากทุกตัวอักษร")]
    public string sceneName = "RankScene";

    [Tooltip("เก็บเหรียญสุดท้ายแล้วรอกี่วินาทีก่อนเปลี่ยนฉาก (กันกรณีเหรียญใหม่กำลังเกิดต่อ)")]
    public float delayAfterLastCoin = 1.5f;

    [Tooltip("ต้องเคยเก็บเหรียญอย่างน้อยหนึ่งเหรียญ ไม่งั้นฉากที่ไม่มีเหรียญเลยจะเด้งไป RankScene ทันที")]
    public bool requireAtLeastOneCollected = true;

    float emptyTimer;
    bool loading;

    // สร้างตัวโหลดให้เองทุกครั้งที่โหลดฉากที่มีเหรียญ (เกมที่ Build มักเริ่มจากฉากเมนู)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureExists();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureExists();
    }

    static void EnsureExists()
    {
        if (FindFirstObjectByType<RankSceneLoader>() != null) return;
        if (FindFirstObjectByType<CoinPickup>() == null) return;
        new GameObject("RankSceneLoader").AddComponent<RankSceneLoader>();
    }

    void Start()
    {
        bool inBuild = Application.CanStreamedLevelBeLoaded(sceneName);
        Debug.Log("[RankSceneLoader] พร้อมทำงาน: เก็บเหรียญครบแล้วจะไปฉาก \"" + sceneName + "\" (ฉากนี้อยู่ใน Build: " + (inBuild ? "ใช่" : "ไม่ใช่") + ")");
        if (!inBuild) ShowError(MissingSceneMessage());
    }

    string MissingSceneMessage()
    {
        return "ไม่พบฉาก \"" + sceneName + "\" ใน Build\nเปิดฉากนั้นแล้วเลือก File > Build Profiles > Add Open Scenes";
    }

    void Update()
    {
        if (loading) return;

        bool collectedAny = ScoreManager.GetOrCreate().CoinsCollected > 0;
        bool done = CoinPickup.Remaining == 0 && (collectedAny || !requireAtLeastOneCollected);

        if (!done)
        {
            emptyTimer = 0f;
            return;
        }

        emptyTimer += Time.unscaledDeltaTime;
        if (emptyTimer >= delayAfterLastCoin) GoToRank();
    }

    void GoToRank()
    {
        loading = true;              // กันทำซ้ำทุกเฟรม

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[RankSceneLoader] " + MissingSceneMessage());
            ShowError(MissingSceneMessage());      // เกมที่ Build ไม่มี Console ให้ดู จึงแสดงบนจอด้วย
            return;
        }

        Debug.Log("[RankSceneLoader] เก็บเหรียญครบแล้ว กำลังโหลดฉาก \"" + sceneName + "\"");
        ScoreManager.GetOrCreate().SaveResult(Time.timeSinceLevelLoad);
        SceneManager.LoadScene(sceneName);
    }

    // ข้อความสีแดงกลางล่างจอ แสดง 15 วินาที
    static void ShowError(string message)
    {
        var cgo = new GameObject("RankLoaderError");
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var tgo = new GameObject("Text", typeof(RectTransform));
        tgo.transform.SetParent(cgo.transform, false);
        var t = tgo.AddComponent<Text>();
        t.font = Font.CreateDynamicFontFromOSFont(
            new[] { "Leelawadee UI", "Tahoma", "Noto Sans Thai", "Sarabun", "Arial" }, 24);
        t.fontSize = 26;
        t.alignment = TextAnchor.LowerCenter;
        t.color = new Color(1f, 0.3f, 0.3f, 1f);
        t.text = message;
        t.raycastTarget = false;
        var outline = tgo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var r = t.rectTransform;
        r.anchorMin = new Vector2(0f, 0f);
        r.anchorMax = new Vector2(1f, 0f);
        r.pivot = new Vector2(0.5f, 0f);
        r.anchoredPosition = new Vector2(0f, 40f);
        r.sizeDelta = new Vector2(-40f, 90f);

        Destroy(cgo, 15f);
    }
}
