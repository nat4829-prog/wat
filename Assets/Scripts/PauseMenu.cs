using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// กด Esc: เกมหยุด และขึ้นปุ่ม 3 ปุ่มกลางจอ
///   1) เล่นต่อ
///   2) จบเกมและดูคะแนน  (จดคะแนนรอบนี้ แล้วไปหน้า RankScene)
///   3) ออกจากเกม
/// - ไม่ต้องวางเองก็ได้ ระบบสร้างให้ในฉากที่มีเหรียญ (CoinPickup)
/// - หยุดเกมด้วย Time.timeScale = 0 และหยุดเสียง สคริปต์ที่ใช้เวลาแบบ Time.deltaTime จะหยุดเอง
///   สคริปต์ที่ไม่ผูกกับเวลา (เช่นหมุนกล้องตามเมาส์) ให้เช็ก PauseMenu.IsPaused ก่อนทำงาน
/// - ใช้จอยได้ด้วย: ปุ่ม Start เปิด/ปิดเมนู
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>เกมกำลังหยุดอยู่หรือไม่ ให้สคริปต์อื่นเช็กได้</summary>
    public static bool IsPaused { get; private set; }

    [Header("ปลายทาง")]
    public string rankSceneName = "RankScene";

    [Header("ข้อความ")]
    public string titleText = "พักเกม";
    public string resumeLabel = "เล่นต่อ";
    public string endLabel = "จบเกมและดูคะแนน";
    public string quitLabel = "ออกจากเกม";

    [Header("ขนาดและสี")]
    public Vector2 buttonSize = new Vector2(380f, 66f);
    public float buttonSpacing = 18f;
    public Color dimColor = new Color(0.03f, 0.025f, 0.05f, 0.80f);
    public Color resumeColor = new Color(0.16f, 0.44f, 0.29f, 0.96f);
    public Color endColor = new Color(0.62f, 0.46f, 0.08f, 0.96f);
    public Color quitColor = new Color(0.52f, 0.11f, 0.15f, 0.96f);
    public Color textColor = new Color(0.97f, 0.95f, 0.90f, 1f);

    Font font;
    GameObject root;
    Button resumeButton;
    Text errorText;

    float prevTimeScale = 1f;
    CursorLockMode prevLock;
    bool prevCursorVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        IsPaused = false;
    }

    // สร้างเมนูให้เองทุกครั้งที่โหลดฉากที่มีเหรียญ (ไม่โผล่ในหน้า Title / Rank)
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
        if (FindFirstObjectByType<PauseMenu>() != null) return;
        if (FindFirstObjectByType<CoinPickup>() == null) return;
        new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    /* ---------- เริ่มต้น / ปิด ---------- */

    void Start()
    {
        font = Font.CreateDynamicFontFromOSFont(
            new[] { "Leelawadee UI", "Tahoma", "Noto Sans Thai", "Sarabun", "Arial" }, 24);
        EnsureEventSystem();
        BuildUI();
        root.SetActive(false);
    }

    void OnDestroy()
    {
        // เปลี่ยนฉากตอนกำลังหยุดอยู่ ต้องคืนเวลา/เสียงให้ปกติ ไม่งั้นฉากต่อไปจะค้าง
        if (IsPaused)
        {
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }

    void Update()
    {
        if (!PausePressed()) return;
        if (IsPaused) Resume();
        else Pause();
    }

    static bool PausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
        var gp = Gamepad.current;
        return gp != null && gp.startButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    /* ---------- หยุด / เล่นต่อ ---------- */

    public void Pause()
    {
        if (IsPaused || root == null) return;
        IsPaused = true;

        prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        prevLock = Cursor.lockState;
        prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;          // ปลดเมาส์ให้คลิกปุ่มได้
        Cursor.visible = true;

        errorText.text = "";
        root.SetActive(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);     // ให้ใช้ลูกศร/จอยเลือกปุ่มได้
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        Time.timeScale = prevTimeScale;
        AudioListener.pause = false;
        Cursor.lockState = prevLock;
        Cursor.visible = prevCursorVisible;

        if (root != null) root.SetActive(false);
    }

    /// <summary>จบเกมตอนนี้ จดคะแนนที่ได้ แล้วไปหน้า RankScene</summary>
    public void EndGameAndShowScore()
    {
        if (!Application.CanStreamedLevelBeLoaded(rankSceneName))
        {
            string msg = "ไม่พบฉาก \"" + rankSceneName + "\" ใน Build: เปิดฉากนั้นแล้วเลือก File > Build Profiles > Add Open Scenes";
            Debug.LogError("[PauseMenu] " + msg);
            errorText.text = msg;                        // เมนูยังอยู่ ให้เห็นข้อความ ไม่ใช่กดแล้วเงียบ
            return;
        }

        // คืนเวลาและเสียงก่อนเปลี่ยนฉาก ไม่งั้นหน้า Rank จะค้าง
        ScoreManager.GetOrCreate().SaveResult(Time.timeSinceLevelLoad);

        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;          // หน้า Rank ต้องใช้เมาส์กดปุ่ม
        Cursor.visible = true;

        SceneManager.LoadScene(rankSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;     // Application.Quit ไม่ทำอะไรใน Editor
#else
        Application.Quit();
#endif
    }

    /* ---------- UI ---------- */

    void BuildUI()
    {
        var cgo = new GameObject("PauseCanvas");
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        // พื้นมืดเต็มจอ (รับคลิก กันไม่ให้ทะลุไปโดน UI ด้านหลัง)
        root = new GameObject("PauseRoot", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(cgo.transform, false);
        var rrt = root.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero;
        rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero;
        rrt.offsetMax = Vector2.zero;
        var dim = root.GetComponent<Image>();
        dim.color = dimColor;
        dim.raycastTarget = true;

        MakeText(rrt, titleText, 52, TextAnchor.MiddleCenter, textColor, new Vector2(0f, 190f), new Vector2(700f, 80f));

        float pitch = buttonSize.y + buttonSpacing;
        resumeButton = MakeButton(rrt, resumeLabel, resumeColor, new Vector2(0f, pitch), Resume);
        MakeButton(rrt, endLabel, endColor, Vector2.zero, EndGameAndShowScore);
        MakeButton(rrt, quitLabel, quitColor, new Vector2(0f, -pitch), QuitGame);

        errorText = MakeText(rrt, "", 22, TextAnchor.UpperCenter, new Color(1f, 0.35f, 0.35f, 1f),
                             new Vector2(0f, -pitch - buttonSize.y * 0.5f - 24f), new Vector2(900f, 70f));
    }

    Button MakeButton(Transform parent, string label, Color color, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = buttonSize;

        var img = go.GetComponent<Image>();
        img.color = color;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        cb.selectedColor = new Color(1.25f, 1.25f, 1.25f, 1f);      // ปุ่มที่เลือกด้วยคีย์บอร์ด/จอยสว่างขึ้น
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        cb.fadeDuration = 0.05f;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        var t = MakeText(rt, label, 28, TextAnchor.MiddleCenter, textColor, Vector2.zero, Vector2.zero);
        var tr = t.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        return btn;
    }

    Text MakeText(Transform parent, string text, int size, TextAnchor align, Color color, Vector2 pos, Vector2 boxSize)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.text = text;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var r = t.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = boxSize;
        return t;
    }

    // ฉากเล่นมักไม่มี EventSystem ถ้าไม่มี ปุ่มจะกดไม่ได้เลย จึงสร้างให้
    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem").AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.gameObject.AddComponent<StandaloneInputModule>();
#endif
    }
}
