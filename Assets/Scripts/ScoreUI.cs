using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// แสดงคะแนนที่มุมขวาบนของจอ สร้าง Canvas และข้อความเองทั้งหมด
/// ไม่ต้องวางเองก็ได้ ระบบสร้างให้ตอนเริ่มฉาก แต่ถ้าวางเองก็ปรับตำแหน่ง/สีได้ที่ Inspector
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Header("ตำแหน่ง (มุมขวาบน)")]
    public Canvas targetCanvas;                          // เว้นว่างได้ ระบบสร้างให้
    public Vector2 margin = new Vector2(16f, 16f);

    [Header("ข้อความ")]
    public string scoreLabel = "คะแนน";
    public string remainingLabel = "เหรียญที่ต้องเก็บ";
    public string allCollectedText = "เก็บเหรียญครบแล้ว!";

    [Header("สี")]
    public Color panelColor = new Color(0.04f, 0.035f, 0.06f, 0.7f);
    public Color scoreColor = new Color(1f, 0.84f, 0.1f, 1f);
    public Color textColor = new Color(0.91f, 0.89f, 0.83f, 1f);
    public Color dimColor = new Color(0.6f, 0.58f, 0.69f, 1f);

    Font font;
    ScoreManager sm;
    Text scoreText, labelText, subText;
    RectTransform scoreRt;
    float punch;

    // ทำงานครั้งเดียวตอนเปิดเกม แล้วคอยฟังทุกครั้งที่โหลดฉาก
    // (เกมที่ Build มักเริ่มจากฉากเมนู ถ้าสร้างแค่ครั้งแรก UI จะหายไปตอนเปลี่ยนเข้าฉากเล่น)
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

    // สร้างเฉพาะฉากที่มีเหรียญ จะได้ไม่โผล่ในหน้าเมนู
    static void EnsureExists()
    {
        if (FindFirstObjectByType<ScoreUI>() != null) return;
        if (FindFirstObjectByType<CoinPickup>() == null) return;
        new GameObject("ScoreUI").AddComponent<ScoreUI>();
    }

    void Start()
    {
        font = Font.CreateDynamicFontFromOSFont(
            new[] { "Leelawadee UI", "Tahoma", "Noto Sans Thai", "Sarabun", "Arial" }, 24);
        Build();
        sm = ScoreManager.GetOrCreate();                 // ให้แน่ใจว่ามีตัวเก็บคะแนน
        ScoreManager.Changed += OnScoreChanged;
        CoinPickup.CountChanged += OnCountChanged;
        Refresh(false);
    }

    void OnDestroy()
    {
        ScoreManager.Changed -= OnScoreChanged;
        CoinPickup.CountChanged -= OnCountChanged;
    }

    void OnScoreChanged()
    {
        Refresh(true);
    }

    void OnCountChanged()
    {
        Refresh(false);
    }

    void Update()
    {
        // ตัวเลขคะแนนเด้งใหญ่ขึ้นเล็กน้อยแล้วค่อยกลับ
        if (punch > 0f)
        {
            punch = Mathf.Max(0f, punch - Time.unscaledDeltaTime * 4f);
            float s = 1f + 0.3f * punch;
            scoreRt.localScale = new Vector3(s, s, 1f);
        }
    }

    void Refresh(bool animate)
    {
        // กันกรณีฉากกำลังถูกปิด แล้วข้อความถูกทำลายไปก่อน
        if (sm == null || scoreText == null || subText == null) return;

        scoreText.text = sm.Score.ToString("N0");

        int remaining = CoinPickup.Remaining;            // นับจากเหรียญที่มีอยู่ในเกมตอนนี้
        if (remaining == 0 && sm.CoinsCollected > 0)
        {
            subText.text = allCollectedText;
        }
        else
        {
            string hex = ColorUtility.ToHtmlStringRGB(scoreColor);
            subText.text = remainingLabel + "  <b><color=#" + hex + ">" + remaining + "</color></b>";
        }

        if (animate) punch = 1f;
    }

    /* ---------- UI ---------- */

    void Build()
    {
        Canvas canvas = targetCanvas;
        if (canvas == null)
        {
            var cgo = new GameObject("ScoreCanvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 21;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // แผ่นพื้นหลัง
        var panelGo = new GameObject("ScorePanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvas.transform, false);
        var prt = panelGo.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(1f, 1f);          // มุมขวาบน
        prt.pivot = new Vector2(1f, 1f);
        prt.anchoredPosition = new Vector2(-margin.x, -margin.y);
        prt.sizeDelta = new Vector2(240f, 88f);
        var pimg = panelGo.GetComponent<Image>();
        pimg.color = panelColor;
        pimg.raycastTarget = false;

        // ป้าย "คะแนน"
        labelText = MakeText(prt, "Label", 18, TextAnchor.UpperLeft, dimColor);
        SetRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -8f), new Vector2(100f, 24f));
        labelText.text = scoreLabel;

        // ตัวเลขคะแนน
        scoreText = MakeText(prt, "Score", 40, TextAnchor.MiddleRight, scoreColor);
        scoreRt = scoreText.rectTransform;
        SetRect(scoreRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -4f), new Vector2(180f, 50f));
        scoreRt.pivot = new Vector2(1f, 1f);                         // เด้งจากมุมขวา

        // บรรทัดล่าง: เหรียญ · สูงสุด
        subText = MakeText(prt, "Sub", 17, TextAnchor.LowerRight, textColor);
        SetRect(subText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 8f), new Vector2(212f, 24f));
    }

    Text MakeText(Transform parent, string name, int size, TextAnchor align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void SetRect(RectTransform r, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.pivot = anchorMin;                 // pivot ตามมุมที่ยึด
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }
}
