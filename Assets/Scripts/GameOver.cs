using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// จบเกมเพราะผู้เล่นตาย: หยุดเกมสั้น ๆ แสดงข้อความ "คุณตายแล้ว" จดคะแนนรอบนี้ แล้วไปหน้า RankScene
/// เรียกใช้จากที่ไหนก็ได้ด้วย  GameOver.ShowRank();  (PlayerHealth.Die เรียกให้แล้ว)
/// ไม่ต้องวางสคริปต์นี้ในฉากเอง
/// </summary>
public class GameOver : MonoBehaviour
{
    /// <summary>กำลังอยู่ในขั้นตอนจบเกมหรือไม่ (กันเรียกซ้ำ)</summary>
    public static bool IsOver { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        IsOver = false;
    }

    /// <summary>เริ่มขั้นตอนจบเกม แล้วไปหน้าอันดับหลังหน่วงเวลา (วินาที)</summary>
    public static void ShowRank(float delaySeconds = 1.5f, string rankSceneName = "RankScene")
    {
        if (IsOver) return;
        IsOver = true;
        var go = new GameObject("GameOver");
        go.AddComponent<GameOver>().Begin(delaySeconds, rankSceneName);
    }

    string title = "คุณตายแล้ว";
    string subtitle = "กำลังสรุปคะแนน...";
    Text subtitleText;
    Font font;

    void Begin(float delay, string rankScene)
    {
        StartCoroutine(Sequence(delay, rankScene));
    }

    void OnDestroy()
    {
        IsOver = false;          // ฉากถูกปิดแล้ว (เปลี่ยนฉากสำเร็จ) รอบต่อไปเริ่มได้ใหม่
    }

    IEnumerator Sequence(float delay, string rankScene)
    {
        float playedSeconds = Time.timeSinceLevelLoad;      // เวลาที่เล่นจริงจนถึงตอนตาย

        // แช่เกมไว้ ไม่ให้ศัตรูหรืออย่างอื่นเดินต่อระหว่างรอ
        Time.timeScale = 0f;
        AudioListener.pause = true;

        font = Font.CreateDynamicFontFromOSFont(
            new[] { "Leelawadee UI", "Tahoma", "Noto Sans Thai", "Sarabun", "Arial" }, 24);
        BuildUI();

        yield return new WaitForSecondsRealtime(delay);

        if (!Application.CanStreamedLevelBeLoaded(rankScene))
        {
            string msg = "ไม่พบฉาก \"" + rankScene + "\" ใน Build: เปิดฉากนั้นแล้วเลือก File > Build Profiles > Add Open Scenes";
            Debug.LogError("[GameOver] " + msg);
            subtitleText.text = msg;
            subtitleText.color = new Color(1f, 0.35f, 0.35f, 1f);
            Time.timeScale = 1f;                            // ไม่ให้เกมค้างถาวร
            AudioListener.pause = false;
            IsOver = false;
            Destroy(gameObject, 15f);
            yield break;
        }

        ScoreManager.GetOrCreate().SaveResult(playedSeconds);

        Time.timeScale = 1f;                                // คืนค่าก่อนเปลี่ยนฉาก ไม่งั้นหน้า Rank จะค้าง
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;             // หน้า Rank ต้องใช้เมาส์กดปุ่ม
        Cursor.visible = true;

        SceneManager.LoadScene(rankScene);
    }

    void BuildUI()
    {
        var cgo = new GameObject("GameOverCanvas");
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dimGo.transform.SetParent(cgo.transform, false);
        var drt = dimGo.GetComponent<RectTransform>();
        drt.anchorMin = Vector2.zero;
        drt.anchorMax = Vector2.one;
        drt.offsetMin = Vector2.zero;
        drt.offsetMax = Vector2.zero;
        var dim = dimGo.GetComponent<Image>();
        dim.color = new Color(0.1f, 0.0f, 0.02f, 0.72f);
        dim.raycastTarget = false;

        MakeText(drt, title, 84, new Color(0.92f, 0.2f, 0.25f, 1f), new Vector2(0f, 30f), new Vector2(1000f, 120f));
        subtitleText = MakeText(drt, subtitle, 30, new Color(0.95f, 0.93f, 0.88f, 1f), new Vector2(0f, -50f), new Vector2(1000f, 90f));
    }

    Text MakeText(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 box)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.text = text;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var r = t.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = box;
        return t;
    }
}
