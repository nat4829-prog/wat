using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// อยู่ใน RankScene: แสดงผลรอบที่เพิ่งเล่นจบ และตารางอันดับ (จดไว้ใน PlayerPrefs ข้ามการเปิดเกม)
/// - ไม่ต้องวางเองก็ได้ ระบบสร้างให้ในฉากที่ชื่อ "RankScene"
/// - สร้าง UI เองทั้งหมด ไม่ยุ่งกับ Canvas และปุ่มเดิมของคุณ (ปุ่มเดิมยังกดได้ตามปกติ)
/// - เรียงอันดับตามคะแนนมากไปน้อย ถ้าเท่ากันดูที่เวลาน้อยกว่า
/// </summary>
public class RankBoard : MonoBehaviour
{
    public const string SaveKey = "dd-rank-v1";
    const string RankSceneName = "RankScene";
    const int MaxSaved = 10;

    struct Entry
    {
        public int score;
        public int coins;
        public float time;
    }

    [Header("ตำแหน่งและขนาด")]
    public Canvas targetCanvas;                          // เว้นว่างได้ ระบบสร้างให้
    public Vector2 offset = Vector2.zero;                // ขยับแผงจากกลางจอ (เลื่อนหนีปุ่มเดิมได้)
    public Vector2 panelSize = new Vector2(560f, 610f);
    [Range(1, MaxSaved)]
    public int visibleRows = 8;

    [Header("ข้อความ")]
    public string titleText = "อันดับคะแนน";
    public string thisRunLabel = "รอบนี้";
    public string noResultText = "ยังไม่มีผลรอบใหม่";
    public string emptyText = "ยังไม่มีสถิติ เล่นให้จบแล้วคะแนนจะมาอยู่ที่นี่";

    [Header("สี")]
    public Color panelColor = new Color(0.05f, 0.04f, 0.07f, 0.82f);
    public Color textColor = new Color(0.93f, 0.91f, 0.86f, 1f);
    public Color dimColor = new Color(0.62f, 0.6f, 0.7f, 1f);
    public Color highlightColor = new Color(1f, 0.84f, 0.1f, 1f);

    /// <summary>ตัวแสดงอันดับที่กำลังทำงานอยู่ในฉาก (RankReset ใช้สั่งให้รีเฟรชหลังล้าง)</summary>
    public static RankBoard Instance { get; private set; }

    Font font;
    GameObject panelGo;
    Canvas canvas;
    readonly List<Entry> entries = new List<Entry>();
    bool hasNew;
    Entry current;
    int newRank = -1;

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
        if (FindFirstObjectByType<RankBoard>() != null) return;
        new GameObject("RankBoard").AddComponent<RankBoard>();
    }

    /* ---------- เก็บ / โหลดอันดับ ---------- */

    static int Compare(Entry a, Entry b)
    {
        int c = b.score.CompareTo(a.score);
        return c != 0 ? c : a.time.CompareTo(b.time);
    }

    static List<Entry> Load()
    {
        var list = new List<Entry>();
        string raw = PlayerPrefs.GetString(SaveKey, "");
        if (raw.Length == 0) return list;

        foreach (string part in raw.Split(';'))
        {
            string[] f = part.Split(',');
            if (f.Length < 3) continue;
            int s, c;
            float t;
            if (int.TryParse(f[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out s) &&
                int.TryParse(f[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out c) &&
                float.TryParse(f[2], NumberStyles.Float, CultureInfo.InvariantCulture, out t))
            {
                list.Add(new Entry { score = s, coins = c, time = t });
            }
        }
        list.Sort(Compare);
        return list;
    }

    static void Save(List<Entry> list)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append(';');
            sb.Append(list[i].score.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(list[i].coins.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(list[i].time.ToString("F1", CultureInfo.InvariantCulture));
        }
        PlayerPrefs.SetString(SaveKey, sb.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>ล้างตารางอันดับทั้งหมด ผูกกับปุ่มใน OnClick ได้</summary>
    public void ClearRanking()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        entries.Clear();
        newRank = -1;
        hasNew = false;                  // ซ่อนผลรอบนี้ด้วย ไม่งั้นจะขึ้น "ยังไม่ติดอันดับ" ทั้งที่ตารางว่าง
        Rebuild();
    }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /* ---------- เริ่มทำงาน ---------- */

    void Start()
    {
        font = Font.CreateDynamicFontFromOSFont(
            new[] { "Leelawadee UI", "Tahoma", "Noto Sans Thai", "Sarabun", "Arial" }, 24);

        entries.AddRange(Load());

        if (ScoreManager.HasLastResult)
        {
            hasNew = true;
            current = new Entry
            {
                score = ScoreManager.LastScore,
                coins = ScoreManager.LastCoins,
                time = ScoreManager.LastTimeSeconds
            };
            ScoreManager.ClearLastResult();              // บันทึกครั้งเดียวเท่านั้น

            int idx = 0;
            while (idx < entries.Count && Compare(entries[idx], current) <= 0) idx++;
            if (idx < MaxSaved)
            {
                entries.Insert(idx, current);
                if (entries.Count > MaxSaved) entries.RemoveRange(MaxSaved, entries.Count - MaxSaved);
                newRank = idx;
                Save(entries);
            }
            else
            {
                newRank = -1;                            // คะแนนไม่ติดอันดับ
            }
        }

        Rebuild();
    }

    /* ---------- UI ---------- */

    static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        return (total / 60) + ":" + (total % 60).ToString("00");
    }

    void Rebuild()
    {
        if (panelGo != null) Destroy(panelGo);

        if (canvas == null)
        {
            canvas = targetCanvas;
            if (canvas == null)
            {
                var cgo = new GameObject("RankBoardCanvas");
                canvas = cgo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5;
                var scaler = cgo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        panelGo = new GameObject("RankPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvas.transform, false);
        var prt = panelGo.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = offset;
        prt.sizeDelta = panelSize;
        var img = panelGo.GetComponent<Image>();
        img.color = panelColor;
        img.raycastTarget = false;                       // ไม่บังปุ่มของเดิม

        float w = panelSize.x;

        // หัวเรื่อง
        Cell(prt, titleText, 40, TextAnchor.UpperCenter, highlightColor, 0f, -18f, w);

        // ผลรอบนี้
        if (hasNew)
        {
            Cell(prt, thisRunLabel, 20, TextAnchor.UpperLeft, dimColor, 32f, -88f, 160f);
            Cell(prt, current.score.ToString("N0"), 56, TextAnchor.UpperRight, textColor, w - 32f - 300f, -70f, 300f);
            Cell(prt, "เหรียญ " + current.coins + "   ·   เวลา " + FormatTime(current.time),
                 20, TextAnchor.UpperLeft, dimColor, 32f, -140f, w - 64f);

            string status;
            Color statusColor = textColor;
            if (newRank == 0) { status = "สถิติสูงสุดใหม่!"; statusColor = highlightColor; }
            else if (newRank > 0) { status = "ติดอันดับที่ " + (newRank + 1); statusColor = highlightColor; }
            else status = "ยังไม่ติดอันดับ";
            Cell(prt, status, 22, TextAnchor.UpperRight, statusColor, w - 32f - 260f, -140f, 260f);
        }
        else
        {
            Cell(prt, noResultText, 20, TextAnchor.UpperCenter, dimColor, 0f, -100f, w);
        }

        // หัวตาราง
        const float headY = -206f;
        Cell(prt, "อันดับ", 18, TextAnchor.UpperLeft, dimColor, 32f, headY, 80f);
        Cell(prt, "คะแนน", 18, TextAnchor.UpperRight, dimColor, 270f - 120f, headY, 120f);
        Cell(prt, "เหรียญ", 18, TextAnchor.UpperRight, dimColor, 400f - 100f, headY, 100f);
        Cell(prt, "เวลา", 18, TextAnchor.UpperRight, dimColor, 528f - 100f, headY, 100f);

        // แถวอันดับ
        if (entries.Count == 0)
        {
            Cell(prt, emptyText, 20, TextAnchor.UpperCenter, dimColor, 0f, -270f, w);
            return;
        }

        int rows = Mathf.Min(visibleRows, entries.Count);
        for (int i = 0; i < rows; i++)
        {
            float y = -240f - i * 40f;
            Color c = i == newRank ? highlightColor : textColor;
            Entry e = entries[i];
            Cell(prt, "#" + (i + 1), 26, TextAnchor.UpperLeft, c, 32f, y, 80f);
            Cell(prt, e.score.ToString("N0"), 26, TextAnchor.UpperRight, c, 270f - 120f, y, 120f);
            Cell(prt, e.coins.ToString(), 26, TextAnchor.UpperRight, c, 400f - 100f, y, 100f);
            Cell(prt, FormatTime(e.time), 26, TextAnchor.UpperRight, c, 528f - 100f, y, 100f);
        }
    }

    // วางข้อความที่มุมซ้ายบนของแผง (x ไปทางขวา, y ติดลบคือลงล่าง)
    Text Cell(Transform parent, string text, int size, TextAnchor align, Color color, float x, float y, float width)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.text = text;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;

        var r = t.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(width, size + 12f);
        return t;
    }
}
