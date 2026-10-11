using System;
using UnityEngine;

/// <summary>
/// เก็บคะแนนและจำนวนเหรียญที่ผู้เล่นเก็บได้ในฉากนี้
/// ไม่ต้องวางเองก็ได้ เหรียญตัวแรกที่ถูกเก็บจะสร้างให้อัตโนมัติ (ดู GetOrCreate)
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public const string HighScoreKey = "dd-high-score";

    static ScoreManager instance;

    /// <summary>ถูกเรียกทุกครั้งที่คะแนนเปลี่ยน (ScoreUI ฟังเหตุการณ์นี้)</summary>
    public static event Action Changed;

    [Tooltip("จดคะแนนสูงสุดข้ามการเล่นด้วย PlayerPrefs")]
    public bool saveHighScore = true;

    public int Score { get; private set; }
    public int CoinsCollected { get; private set; }
    public int HighScore { get; private set; }

    // ผลของรอบล่าสุด อยู่รอดข้ามฉาก (ScoreManager ถูกทำลายตอนเปลี่ยนฉาก) ให้ RankScene อ่านไปแสดงได้
    public static bool HasLastResult { get; private set; }
    public static int LastScore { get; private set; }
    public static int LastCoins { get; private set; }
    public static float LastTimeSeconds { get; private set; }

    /// <summary>ลบคะแนนสูงสุดที่จดไว้ใน PlayerPrefs</summary>
    public static void ClearSavedHighScore()
    {
        PlayerPrefs.DeleteKey(HighScoreKey);
        if (instance != null) instance.HighScore = 0;
    }

    /// <summary>ล้างผลรอบล่าสุดหลังนำไปบันทึกอันดับแล้ว (กันบันทึกซ้ำเมื่อเข้า RankScene อีกครั้ง)</summary>
    public static void ClearLastResult()
    {
        HasLastResult = false;
    }

    /// <summary>จดผลรอบนี้ไว้ก่อนเปลี่ยนฉาก</summary>
    public void SaveResult(float seconds)
    {
        HasLastResult = true;
        LastScore = Score;
        LastCoins = CoinsCollected;
        LastTimeSeconds = seconds;
        if (saveHighScore) PlayerPrefs.Save();
    }

    public static ScoreManager GetOrCreate()
    {
        if (instance == null) instance = FindFirstObjectByType<ScoreManager>();
        if (instance == null) instance = new GameObject("ScoreManager").AddComponent<ScoreManager>();
        return instance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        Changed = null;
        HasLastResult = false;
        LastScore = 0;
        LastCoins = 0;
        LastTimeSeconds = 0f;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        HighScore = saveHighScore ? PlayerPrefs.GetInt(HighScoreKey, 0) : 0;
    }

    /// <summary>เพิ่มคะแนนจากการเก็บเหรียญหนึ่งเหรียญ</summary>
    public void AddCoin(int points)
    {
        CoinsCollected++;
        Score += points;
        if (Score > HighScore)
        {
            HighScore = Score;
            if (saveHighScore) PlayerPrefs.SetInt(HighScoreKey, HighScore);
        }
        if (Changed != null) Changed();
    }

    /// <summary>ล้างคะแนนเริ่มใหม่ (คะแนนสูงสุดยังอยู่)</summary>
    public void ResetScore()
    {
        Score = 0;
        CoinsCollected = 0;
        if (Changed != null) Changed();
    }
}
