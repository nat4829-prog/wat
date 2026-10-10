using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// มินิแมปมุมซ้ายบนของจอ
/// - กล้องมองตรงลงมาจากด้านบน (Orthographic) วาดลง RenderTexture เป็นภาพแผนที่จริงของซีน
/// - ไอคอนผู้เล่น/ศัตรู/เหรียญ/ไอเท็ม วางทับตามตำแหน่งจริง
/// - สร้าง Canvas และ UI เองทั้งหมด วางสคริปต์นี้บน GameObject ใดก็ได้ในซีน
/// </summary>
public class MinimapController : MonoBehaviour
{
    [Serializable]
    public class AutoTag
    {
        public string tag;
        public MinimapMarkerType type;
    }

    [Header("ตำแหน่งบนจอ (มุมซ้ายบน)")]
    public Canvas targetCanvas;                                  // เว้นว่างได้ ระบบสร้างให้
    public Vector2 margin = new Vector2(16f, 16f);
    public float size = 220f;
    public float expandedSize = 480f;
    public bool enableToggleKey = true;                          // กด M ขยาย/ย่อ

    [Header("มุมมองแผนที่")]
    [Tooltip("ปิด = เห็นทั้งแผนที่ / เปิด = ซูมตามผู้เล่น")]
    public bool followPlayer = false;
    public float followHalfSize = 22f;                           // ครึ่งหนึ่งของพื้นที่ที่เห็น (หน่วยเมตร)
    public bool autoFitBounds = true;                            // คำนวณขอบแผนที่จาก MeshRenderer ทั้งซีน
    public Bounds manualBounds = new Bounds(Vector3.zero, new Vector3(100f, 10f, 100f));
    public float boundsPadding = 4f;
    public LayerMask mapLayers = ~0;                             // เลเยอร์ที่กล้องแผนที่มองเห็น (ตัดหลังคาออกได้)
    public Color backgroundColor = new Color(0.04f, 0.035f, 0.06f, 1f);
    public int textureSize = 512;

    [Header("ความสว่างของแผนที่")]
    [Tooltip("ปิดหมอกและเพิ่มแสงรอบทิศเฉพาะตอนกล้องแผนที่เรนเดอร์ เกมมืดแค่ไหนแผนที่ก็ยังอ่านออก")]
    public bool brightenMapRender = true;
    public Color mapAmbient = new Color(0.75f, 0.75f, 0.8f, 1f);

    [Header("ไอคอน")]
    public float sizePlayer = 22f;
    public float sizeEnemy = 15f;
    public float sizeCoin = 11f;
    public float sizeItem = 15f;
    public Color colorPlayer = new Color(0.15f, 0.85f, 0.30f, 1f);   // เขียว
    public Color colorEnemy = new Color(0.95f, 0.10f, 0.10f, 1f);   // แดง
    public Color colorCoin = new Color(1.00f, 0.84f, 0.10f, 1f);   // เหลือง
    public Color colorItem = new Color(1.00f, 1.00f, 1.00f, 1f);   // ขาว
    public Color iconOutline = new Color(0f, 0f, 0f, 0.85f);         // ขอบดำ ให้เห็นชัดบนพื้นสีอ่อน
    [Tooltip("ซ่อนโมเดล 3D ของผู้เล่น/ศัตรู/เหรียญ/ไอเท็มเฉพาะในภาพแผนที่ เหลือแค่ไอคอนสี")]
    public bool hideMarkedObjectsOnMap = true;
    [Tooltip("ศัตรูอยู่ไกลกว่านี้จะไม่โชว์ (0 = โชว์เสมอ)")]
    public float enemyRevealRadius = 0f;

    [Header("ค้นหาอัตโนมัติด้วย Tag (ไม่ต้องแปะ MinimapMarker เอง)")]
    public bool autoMarkByTag = true;
    public List<AutoTag> autoTags = new List<AutoTag>
    {
        new AutoTag { tag = "Player", type = MinimapMarkerType.Player },
        new AutoTag { tag = "Enemy",  type = MinimapMarkerType.Enemy  },
        new AutoTag { tag = "Coin",   type = MinimapMarkerType.Coin   },
        new AutoTag { tag = "Item",   type = MinimapMarkerType.Item   },
    };
    public float rescanInterval = 1f;

    [Header("ตรวจสอบปัญหา")]
    [Tooltip("พิมพ์จำนวนไอคอนแต่ละชนิดใน Console เมื่อจำนวนเปลี่ยน")]
    public bool logCounts = true;
    string lastCountLog = "";
    bool warnedNoPlayer;

    // ---------- runtime ----------
    Camera mapCam;
    RenderTexture rt;
    RectTransform frameRt, viewRt, iconRoot;
    Vector3 fixedCenter;
    float fixedHalfSize, camHeight;
    bool expanded;
    float rescanTimer;
    MinimapMarker playerMarker;

    readonly Dictionary<MinimapMarker, Image> icons = new Dictionary<MinimapMarker, Image>();
    readonly Dictionary<MinimapMarker, Renderer[]> markerRenderers = new Dictionary<MinimapMarker, Renderer[]>();
    readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    readonly HashSet<string> warnedTags = new HashSet<string>();
    readonly List<MinimapMarker> removeBuffer = new List<MinimapMarker>();
    Sprite circleSprite, triangleSprite, diamondSprite;

    bool lookApplied, prevFog;
    Color prevAmbient;
    AmbientMode prevAmbientMode;

    /* ====================================================== */

    void Start()
    {
        BuildSprites();
        ComputeBounds();
        BuildCamera();
        BuildUI();

        MinimapMarker.MarkerEnabled += OnMarkerEnabled;
        MinimapMarker.MarkerDisabled += OnMarkerDisabled;
        foreach (var m in MinimapMarker.All.ToArray()) OnMarkerEnabled(m);

        RenderPipelineManager.beginCameraRendering += OnBeginCameraSrp;
        RenderPipelineManager.endCameraRendering += OnEndCameraSrp;
        Camera.onPreCull += BeginLook;          // Built-in RP (ต้องซ่อนก่อน culling)
        Camera.onPostRender += EndLook;

        if (autoMarkByTag) ScanTags();
    }

    void OnDestroy()
    {
        MinimapMarker.MarkerEnabled -= OnMarkerEnabled;
        MinimapMarker.MarkerDisabled -= OnMarkerDisabled;
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraSrp;
        RenderPipelineManager.endCameraRendering -= OnEndCameraSrp;
        Camera.onPreCull -= BeginLook;
        Camera.onPostRender -= EndLook;
        if (lookApplied) RestoreLook();
        if (rt != null) { rt.Release(); Destroy(rt); }
    }

    /* ---------- setup ---------- */

    void ComputeBounds()
    {
        Bounds b = manualBounds;
        if (autoFitBounds)
        {
            bool has = false;
            Bounds acc = new Bounds();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled) continue;
                if ((mapLayers.value & (1 << r.gameObject.layer)) == 0) continue;
                if (!has) { acc = r.bounds; has = true; }
                else acc.Encapsulate(r.bounds);
            }
            if (has) b = acc;
        }
        fixedCenter = new Vector3(b.center.x, 0f, b.center.z);
        fixedHalfSize = Mathf.Max(b.extents.x, b.extents.z) + boundsPadding;
        camHeight = b.max.y + 40f;
    }

    /// <summary>เรียกจากโค้ดได้ ถ้าสร้างด่านใหม่และต้องการให้แผนที่คำนวณขอบใหม่</summary>
    public void Refit()
    {
        ComputeBounds();
    }

    void BuildCamera()
    {
        rt = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32);
        rt.name = "MinimapRT";
        rt.antiAliasing = 2;

        var go = new GameObject("MinimapCamera");
        go.transform.SetParent(transform, false);
        mapCam = go.AddComponent<Camera>();
        mapCam.orthographic = true;
        mapCam.clearFlags = CameraClearFlags.SolidColor;
        mapCam.backgroundColor = backgroundColor;
        mapCam.cullingMask = mapLayers;
        mapCam.nearClipPlane = 0.3f;
        mapCam.farClipPlane = 600f;
        mapCam.targetTexture = rt;
        mapCam.allowHDR = false;
        mapCam.allowMSAA = false;
        mapCam.depth = -50f;                       // เรนเดอร์ก่อนกล้องหลัก
        mapCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // มองลง, ขึ้นบนจอ = แกน +Z
        mapCam.orthographicSize = fixedHalfSize;
        mapCam.transform.position = new Vector3(fixedCenter.x, camHeight, fixedCenter.z);
    }

    void BuildUI()
    {
        Canvas canvas = targetCanvas;
        if (canvas == null)
        {
            var cgo = new GameObject("MinimapCanvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // กรอบ
        var frameGo = new GameObject("Minimap", typeof(RectTransform), typeof(Image));
        frameGo.transform.SetParent(canvas.transform, false);
        frameRt = frameGo.GetComponent<RectTransform>();
        frameRt.anchorMin = frameRt.anchorMax = new Vector2(0f, 1f);     // มุมซ้ายบน
        frameRt.pivot = new Vector2(0f, 1f);
        frameRt.anchoredPosition = new Vector2(margin.x, -margin.y);
        frameRt.sizeDelta = new Vector2(size, size);
        var frameImg = frameGo.GetComponent<Image>();
        frameImg.color = new Color(0.91f, 0.89f, 0.83f, 0.9f);
        frameImg.raycastTarget = false;

        // ภาพแผนที่
        var viewGo = new GameObject("View", typeof(RectTransform), typeof(RawImage), typeof(RectMask2D));
        viewGo.transform.SetParent(frameRt, false);
        viewRt = viewGo.GetComponent<RectTransform>();
        viewRt.anchorMin = Vector2.zero;
        viewRt.anchorMax = Vector2.one;
        viewRt.offsetMin = new Vector2(3f, 3f);
        viewRt.offsetMax = new Vector2(-3f, -3f);
        var raw = viewGo.GetComponent<RawImage>();
        raw.texture = rt;
        raw.raycastTarget = false;

        // ที่วางไอคอน
        var iconGo = new GameObject("Icons", typeof(RectTransform));
        iconGo.transform.SetParent(viewRt, false);
        iconRoot = iconGo.GetComponent<RectTransform>();
        iconRoot.anchorMin = Vector2.zero;
        iconRoot.anchorMax = Vector2.one;
        iconRoot.offsetMin = Vector2.zero;
        iconRoot.offsetMax = Vector2.zero;
    }

    /* ---------- sprites ---------- */

    void BuildSprites()
    {
        circleSprite = MakeSprite((x, y) => x * x + y * y <= 1f);
        diamondSprite = MakeSprite((x, y) => Mathf.Abs(x) + Mathf.Abs(y) <= 1f);
        // ลูกศรชี้ขึ้น: สามเหลี่ยมยอดอยู่บน
        triangleSprite = MakeSprite((x, y) =>
        {
            float yy = (y + 1f) * 0.5f;                // 0 ล่าง → 1 บน
            return yy >= 0f && yy <= 1f && Mathf.Abs(x) <= (1f - yy) * 0.9f + 0.02f;
        });
    }

    static Sprite MakeSprite(Func<float, float, bool> inside)
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[n * n];
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                // ซูเปอร์แซมเปิล 3x3 ให้ขอบเนียน
                int hit = 0;
                for (int sj = 0; sj < 3; sj++)
                    for (int si = 0; si < 3; si++)
                    {
                        float x = ((i + (si + 0.5f) / 3f) / n) * 2f - 1f;
                        float y = ((j + (sj + 0.5f) / 3f) / n) * 2f - 1f;
                        if (inside(x, y)) hit++;
                    }
                px[j * n + i] = new Color32(255, 255, 255, (byte)(hit * 255 / 9));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }

    /* ---------- markers ---------- */

    void OnMarkerEnabled(MinimapMarker m)
    {
        if (m == null || icons.ContainsKey(m) || iconRoot == null) return;

        var go = new GameObject("Icon_" + m.type, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(iconRoot, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.sprite = SpriteFor(m.type);
        img.color = ColorFor(m);
        float s = SizeFor(m.type);
        var rtf = go.GetComponent<RectTransform>();
        rtf.anchorMin = rtf.anchorMax = new Vector2(0.5f, 0.5f);
        rtf.pivot = new Vector2(0.5f, 0.5f);
        rtf.sizeDelta = new Vector2(s, s);

        var outline = go.AddComponent<Outline>();
        outline.effectColor = iconOutline;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        icons[m] = img;
        markerRenderers[m] = m.GetComponentsInChildren<Renderer>(true);

        if (m.type == MinimapMarkerType.Player && playerMarker == null) playerMarker = m;
        SortIcons();
    }

    void OnMarkerDisabled(MinimapMarker m)
    {
        if (m == null) return;
        Image img;
        if (icons.TryGetValue(m, out img))
        {
            if (img != null) Destroy(img.gameObject);
            icons.Remove(m);
        }
        markerRenderers.Remove(m);
        if (m == playerMarker) playerMarker = null;
    }

    // ผู้เล่นอยู่บนสุด ตามด้วยศัตรู ไอเท็ม เหรียญ
    void SortIcons()
    {
        var order = new[] { MinimapMarkerType.Coin, MinimapMarkerType.Item, MinimapMarkerType.Enemy, MinimapMarkerType.Player };
        int idx = 0;
        foreach (var t in order)
            foreach (var kv in icons)
                if (kv.Key != null && kv.Key.type == t && kv.Value != null)
                    kv.Value.transform.SetSiblingIndex(idx++);
    }

    Sprite SpriteFor(MinimapMarkerType t)
    {
        switch (t)
        {
            case MinimapMarkerType.Player: return triangleSprite;
            case MinimapMarkerType.Item: return diamondSprite;
            default: return circleSprite;
        }
    }

    float SizeFor(MinimapMarkerType t)
    {
        switch (t)
        {
            case MinimapMarkerType.Player: return sizePlayer;
            case MinimapMarkerType.Enemy: return sizeEnemy;
            case MinimapMarkerType.Item: return sizeItem;
            default: return sizeCoin;
        }
    }

    Color ColorFor(MinimapMarker m)
    {
        if (m.overrideColor.a > 0.01f) return m.overrideColor;
        switch (m.type)
        {
            case MinimapMarkerType.Player: return colorPlayer;
            case MinimapMarkerType.Enemy: return colorEnemy;
            case MinimapMarkerType.Item: return colorItem;
            default: return colorCoin;
        }
    }

    // ติด MinimapMarker ให้วัตถุที่มี Tag ตรงกัน (รวมวัตถุที่ spawn ทีหลัง)
    void ScanTags()
    {
        foreach (var at in autoTags)
        {
            if (string.IsNullOrEmpty(at.tag)) continue;
            GameObject[] found;
            try { found = GameObject.FindGameObjectsWithTag(at.tag); }
            catch (UnityException)
            {
                if (warnedTags.Add(at.tag))
                    Debug.LogWarning("[Minimap] ยังไม่มี Tag \"" + at.tag + "\" ในโปรเจกต์ (Tags & Layers) ข้ามไปก่อน");
                continue;
            }
            foreach (var go in found)
            {
                if (go.GetComponent<MinimapMarker>() != null) continue;
                var mk = go.AddComponent<MinimapMarker>();
                mk.type = at.type;
                mk.rotateWithObject = at.type == MinimapMarkerType.Player;
                // AddComponent เรียก OnEnable ทันที ตอนนั้น type ยังเป็นค่าเริ่มต้น (Coin)
                // จึงต้องสร้างไอคอนใหม่หลังกำหนดชนิดแล้ว ไม่งั้นทุกอย่างจะเป็นสีเหรียญ
                OnMarkerDisabled(mk);
                OnMarkerEnabled(mk);
            }
        }
    }

    void LogCounts()
    {
        int p = 0, e = 0, c = 0, it = 0;
        foreach (var kv in icons)
        {
            if (kv.Key == null) continue;
            switch (kv.Key.type)
            {
                case MinimapMarkerType.Player: p++; break;
                case MinimapMarkerType.Enemy: e++; break;
                case MinimapMarkerType.Item: it++; break;
                default: c++; break;
            }
        }
        string s = "[Minimap] ไอคอนบนแผนที่: ผู้เล่น=" + p + " ศัตรู=" + e + " เหรียญ=" + c + " ไอเท็ม=" + it;
        if (s != lastCountLog) { lastCountLog = s; Debug.Log(s); }

        if (p == 0 && !warnedNoPlayer && Time.timeSinceLevelLoad > 2f)
        {
            warnedNoPlayer = true;
            Debug.LogWarning("[Minimap] ไม่พบผู้เล่น: ตั้ง Tag \"Player\" ที่ PlayerCapsule (วัตถุตัวที่เคลื่อนที่จริง) หรือแปะ MinimapMarker ชนิด Player");
        }
    }

    /* ---------- per-frame ---------- */

    void Update()
    {
        if (!enableToggleKey) return;
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && kb.mKey.wasPressedThisFrame) pressed = true;
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.M)) pressed = true;
#endif
        if (pressed) SetExpanded(!expanded);
    }

    public void SetExpanded(bool value)
    {
        expanded = value;
        if (frameRt != null) frameRt.sizeDelta = Vector2.one * (expanded ? expandedSize : size);
    }

    void LateUpdate()
    {
        if (mapCam == null) return;

        rescanTimer -= Time.unscaledDeltaTime;
        if (rescanTimer <= 0f)
        {
            rescanTimer = rescanInterval;
            if (autoMarkByTag) ScanTags();
            if (logCounts) LogCounts();
        }

        // ตำแหน่งกล้องแผนที่
        if (followPlayer && playerMarker != null)
        {
            Vector3 p = playerMarker.transform.position;
            mapCam.orthographicSize = followHalfSize;
            mapCam.transform.position = new Vector3(p.x, camHeight, p.z);
        }
        else
        {
            mapCam.orthographicSize = fixedHalfSize;
            mapCam.transform.position = new Vector3(fixedCenter.x, camHeight, fixedCenter.z);
        }

        UpdateIcons();
    }

    void UpdateIcons()
    {
        Vector2 half = viewRt.rect.size * 0.5f;
        Vector3 playerPos = playerMarker != null ? playerMarker.transform.position : Vector3.zero;

        removeBuffer.Clear();
        foreach (var kv in icons)
        {
            var m = kv.Key;
            var img = kv.Value;
            if (m == null || img == null) { removeBuffer.Add(m); continue; }

            Vector3 world = m.transform.position;
            Vector3 vp = mapCam.WorldToViewportPoint(world);
            bool visible = vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

            if (visible && m.type == MinimapMarkerType.Enemy && enemyRevealRadius > 0f && playerMarker != null)
            {
                float dx = world.x - playerPos.x, dz = world.z - playerPos.z;
                visible = dx * dx + dz * dz <= enemyRevealRadius * enemyRevealRadius;
            }

            if (img.enabled != visible) img.enabled = visible;
            if (!visible) continue;

            var r = img.rectTransform;
            r.anchoredPosition = new Vector2((vp.x - 0.5f) * 2f * half.x, (vp.y - 0.5f) * 2f * half.y);

            if (m.rotateWithObject)
                r.localRotation = Quaternion.Euler(0f, 0f, -m.transform.eulerAngles.y);

            // เหรียญ/ไอเท็ม/ศัตรู กะพริบเบา ๆ ให้เห็นง่าย
            if (m.type != MinimapMarkerType.Player)
            {
                float s = SizeFor(m.type) * (1f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f + m.GetInstanceID() * 0.37f));
                r.sizeDelta = new Vector2(s, s);
            }
        }
        foreach (var m in removeBuffer)
        {
            if (m != null) icons.Remove(m);
        }
        // ลบไอคอนของ marker ที่ถูกทำลาย (key เป็น null)
        if (removeBuffer.Count > 0)
        {
            var dead = new List<MinimapMarker>();
            foreach (var kv in icons) if (kv.Key == null) dead.Add(kv.Key);
            foreach (var k in dead) { if (icons[k] != null) Destroy(icons[k].gameObject); icons.Remove(k); }
        }
    }

    /* ---------- map-only look (no fog, brighter ambient) ---------- */

    void OnBeginCameraSrp(ScriptableRenderContext ctx, Camera cam) { BeginLook(cam); }
    void OnEndCameraSrp(ScriptableRenderContext ctx, Camera cam) { EndLook(cam); }

    void BeginLook(Camera cam)
    {
        if (cam != mapCam || lookApplied) return;
        lookApplied = true;
        prevFog = RenderSettings.fog;
        prevAmbient = RenderSettings.ambientLight;
        prevAmbientMode = RenderSettings.ambientMode;
        RenderSettings.fog = false;
        if (brightenMapRender)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = mapAmbient;
        }
        if (hideMarkedObjectsOnMap)
        {
            foreach (var kv in markerRenderers)
            {
                var rs = kv.Value;
                if (rs == null) continue;
                foreach (var r in rs)
                {
                    if (r == null || !r.enabled) continue;
                    r.enabled = false;
                    hiddenRenderers.Add(r);
                }
            }
        }
    }

    void EndLook(Camera cam)
    {
        if (cam != mapCam || !lookApplied) return;
        RestoreLook();
    }

    void RestoreLook()
    {
        RenderSettings.fog = prevFog;
        RenderSettings.ambientMode = prevAmbientMode;
        RenderSettings.ambientLight = prevAmbient;
        foreach (var r in hiddenRenderers)
            if (r != null) r.enabled = true;
        hiddenRenderers.Clear();
        lookApplied = false;
    }
}
