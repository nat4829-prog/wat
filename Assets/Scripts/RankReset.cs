using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// รีเซ็ตอันดับ: ลบตารางอันดับที่จดไว้ (และคะแนนสูงสุด ถ้าเลือก) แล้วให้หน้า RankScene รีเฟรชทันที
/// ใช้ได้ 3 แบบ
///   1) ปล่อยไว้เฉย ๆ ระบบสร้างปุ่ม "ล้างอันดับ" มุมขวาล่างของ RankScene ให้เอง (กดสองครั้งเพื่อยืนยัน)
///   2) ผูกปุ่มของคุณเอง: สร้าง GameObject ใส่สคริปต์นี้ แล้วลากมาใส่ OnClick ของปุ่ม เลือก RankReset > ResetRanking
///      (ปิด Create Button ไว้ถ้าไม่อยากได้ปุ่มที่ระบบสร้าง)
///   3) เรียกจากโค้ดที่ไหนก็ได้: RankReset.ResetAll(true);
/// </summary>
public class RankReset : MonoBehaviour
{
    const string RankSceneName = "RankScene";

    [Header("สิ่งที่จะล้าง")]
    [Tooltip("ล้างคะแนนสูงสุด (dd-high-score) ด้วย")]
    public bool alsoResetHighScore = true;

    [Header("ปุ่มที่ระบบสร้างให้")]
    public bool createButton = true;
    public Vector2 buttonMargin = new Vector2(24f, 24f);      // ระยะจากมุมขวาล่าง
    public Vector2 buttonSize = new Vector2(230f, 56f);
    public string buttonLabel = "ล้างอันดับ";
    public string confirmLabel = "กดอีกครั้งเพื่อยืนยัน";
    public string doneLabel = "ล้างอันดับแล้ว";
    [Tooltip("หลังกดครั้งแรก มีเวลากี่วินาทีให้กดซ้ำเพื่อยืนยัน")]
    public float confirmSeconds = 3f;

    static readonly Color NormalColor = new Color(0.50f, 0.10f, 0.14f, 0.92f);
    static readonly Color ConfirmColor = new Color(0.85f, 0.45f, 0.05f, 0.95f);
    static readonly Color DoneColor = new Color(0.15f, 0.55f, 0.30f, 0.95f);

    Image buttonImage;
    Text buttonText;
    bool confirming;
    float revertTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureExists(SceneManager.GetActiveScene().name);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureExists(scene.name);
    }

    static void EnsureExists(string sceneName)
    {
        if (sceneName != RankSceneName) return;
        if (FindFirstObjectByType<RankReset>() != null) return;
        new GameObject("RankReset").AddComponent<RankReset>();
    }

    /* ---------- การล้าง ---------- */

    /// <summary>ล้างอันดับที่จดไว้ทั้งหมด เรียกจากที่ไหนก็ได้</summary>
    public static void ResetAll(bool alsoHighScore)
    {
        PlayerPrefs.DeleteKey(RankBoard.SaveKey);
        if (alsoHighScore) ScoreManager.ClearSavedHighScore();
        ScoreManager.ClearLastResult();
        PlayerPrefs.Save();

        if (RankBoard.Instance != null) RankBoard.Instance.ClearRanking();    // ให้ตารางบนจอว่างทันที
        Debug.Log("[RankReset] ล้างอันดับแล้ว" + (alsoHighScore ? " (รวมคะแนนสูงสุด)" : ""));
    }

    /// <summary>ล้างทันที ไม่ถามยืนยัน ผูกกับปุ่มของคุณเองใน OnClick ได้</summary>
    public void ResetRanking()
    {
        ResetAll(alsoResetHighScore);
        confirming = false;
        ShowState(doneLabel, DoneColor, 1.5f);
    }

    /* ---------- ปุ่ม ---------- */

    void Start()
    {
        if (createButton) BuildButton();
    }

    void Update()
    {
        if (revertTimer <= 0f) return;
        revertTimer -= Time.unscaledDeltaTime;
        if (revertTimer <= 0f)
        {
            confirming = false;
            ShowState(buttonLabel, NormalColor, 0f);
        }
    }

    void OnButtonClicked()
    {
        if (!confirming)
        {
            confirming = true;
            ShowState(confirmLabel, ConfirmColor, confirmSeconds);    // รอกดซ้ำ ไม่กดก็กลับเป็นปกติเอง
        }
        else
        {
            ResetRanking();
        }
    }

    void ShowState(string label, Color color, float seconds)
    {
        revertTimer = seconds;
        if (buttonText != null) buttonText.text = label;
        if (buttonImage != null) buttonImage.color = color;
    }

    void BuildButton()
    {
        EnsureEventSystem();

        var cgo = new GameObject("RankResetCanvas");
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();                       // ต้องมี ปุ่มถึงจะรับการคลิก

        var bgo = new GameObject("ResetButton", typeof(RectTransform), typeof(Image), typeof(Button));
        bgo.transform.SetParent(cgo.transform, false);
        var rt = bgo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);          // มุมขวาล่าง
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-buttonMargin.x, buttonMargin.y);
        rt.sizeDelta = buttonSize;

        buttonImage = bgo.GetComponent<Image>();
        buttonImage.color = NormalColor;
        var btn = bgo.GetComponent<Button>();
        btn.targetGraphic = buttonImage;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(OnButtonClicked);

        var tgo = new GameObject("Label", typeof(RectTransform));
        tgo.transform.SetParent(bgo.transform, false);
        buttonText = tgo.AddComponent<Text>();
        buttonText.font = Font.CreateDynamicFontFromOSFont(
            new[] { "Leelawadee UI", "Tahoma", "Noto Sans Thai", "Sarabun", "Arial" }, 24);
        buttonText.fontSize = 24;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = new Color(0.97f, 0.95f, 0.9f, 1f);
        buttonText.text = buttonLabel;
        buttonText.raycastTarget = false;
        var lr = buttonText.rectTransform;
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
    }

    // ฉากส่วนใหญ่มี EventSystem อยู่แล้ว สร้างให้เฉพาะตอนไม่มีเท่านั้น
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
