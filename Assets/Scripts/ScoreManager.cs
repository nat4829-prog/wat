using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI winText;       // (ไม่บังคับ) ข้อความตอนเก็บครบ

    [Header("Settings")]
    public string prefix = "เหรียญ: ";

    public int Score { get; private set; }
    public int TotalCoins { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // นับเหรียญทั้งหมดในฉาก
        TotalCoins = FindObjectsByType<Coin>(FindObjectsSortMode.None).Length;
        if (winText != null) winText.gameObject.SetActive(false);
        UpdateUI();
    }

    public void AddScore(int amount)
    {
        Score += amount;
        UpdateUI();

        if (Score >= TotalCoins && TotalCoins > 0 && winText != null)
        {
            winText.gameObject.SetActive(true);
            winText.text = "เก็บเหรียญครบแล้ว!";
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"{prefix}{Score} / {TotalCoins}";
    }
}