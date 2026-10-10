using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance { get; private set; }

    [Header("References")]
    public Camera minimapCamera;          // กล้อง Orthographic มองลงมา
    public RectTransform minimapRect;     // RawImage ที่แสดงมินิแมพบน Canvas
    public Transform player;

    [Header("Map Area (ขนาดแมพในโลก)")]
    public Vector3 mapCenter = Vector3.zero;
    public float mapSize = 60f;           // ครึ่งหนึ่งของความกว้างแมพ = orthographicSize
    public float cameraHeight = 80f;

    [Header("Icons")]
    public Sprite coinIconSprite;         // ถ้าไม่ใส่ จะเป็นสี่เหลี่ยมสีเหลือง
    public Color coinColor = Color.yellow;
    public float coinIconSize = 10f;
    public Color playerColor = Color.cyan;
    public float playerIconSize = 14f;

    [Header("Options")]
    public bool followPlayer = false;     // true = กล้องตามผู้เล่น (ซูมเฉพาะรอบตัว)
    public bool rotateWithPlayer = false;

    private readonly Dictionary<Coin, RectTransform> coinIcons = new Dictionary<Coin, RectTransform>();
    private RectTransform playerIcon;

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
        SetupCamera();
        CreatePlayerIcon();

        // เผื่อเหรียญที่ Start ก่อน Manager
        foreach (var coin in FindObjectsByType<Coin>(FindObjectsSortMode.None))
            RegisterCoin(coin);
    }

    private void SetupCamera()
    {
        if (minimapCamera == null) return;

        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = mapSize;
        minimapCamera.transform.position = mapCenter + Vector3.up * cameraHeight;
        minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void CreatePlayerIcon()
    {
        if (minimapRect == null) return;
        playerIcon = CreateIcon("PlayerIcon", playerColor, playerIconSize, null);
    }

    public void RegisterCoin(Coin coin)
    {
        if (coin == null || minimapRect == null || coinIcons.ContainsKey(coin)) return;

        RectTransform icon = CreateIcon("CoinIcon", coinColor, coinIconSize, coinIconSprite);
        coinIcons.Add(coin, icon);
        UpdateIconPosition(icon, coin.transform.position);
    }

    public void UnregisterCoin(Coin coin)
    {
        if (coin == null) return;
        if (coinIcons.TryGetValue(coin, out RectTransform icon))
        {
            if (icon != null) Destroy(icon.gameObject);
            coinIcons.Remove(coin);
        }
    }

    private RectTransform CreateIcon(string iconName, Color color, float size, Sprite sprite)
    {
        GameObject go = new GameObject(iconName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(minimapRect, false);

        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        if (sprite != null) img.sprite = sprite;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    private void LateUpdate()
    {
        if (minimapCamera == null || minimapRect == null) return;

        // กล้องตามผู้เล่น (ถ้าเปิดใช้)
        if (followPlayer && player != null)
        {
            Vector3 p = player.position;
            minimapCamera.transform.position = new Vector3(p.x, cameraHeight, p.z);
            float yaw = rotateWithPlayer ? player.eulerAngles.y : 0f;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
        }

        // อัปเดตตำแหน่งไอคอนเหรียญ
        foreach (var pair in coinIcons)
        {
            if (pair.Key == null || pair.Value == null) continue;
            UpdateIconPosition(pair.Value, pair.Key.transform.position);
        }

        // อัปเดตไอคอนผู้เล่น
        if (player != null && playerIcon != null)
        {
            UpdateIconPosition(playerIcon, player.position);
            playerIcon.localRotation = Quaternion.Euler(0f, 0f,
                rotateWithPlayer ? 0f : -player.eulerAngles.y);
        }
    }

    // แปลงตำแหน่งในโลก -> ตำแหน่งบน UI มินิแมพ
    private void UpdateIconPosition(RectTransform icon, Vector3 worldPos)
    {
        Vector3 vp = minimapCamera.WorldToViewportPoint(worldPos);

        bool visible = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
        icon.gameObject.SetActive(visible);
        if (!visible) return;

        Vector2 size = minimapRect.rect.size;
        icon.anchoredPosition = new Vector2((vp.x - 0.5f) * size.x, (vp.y - 0.5f) * size.y);
    }
}