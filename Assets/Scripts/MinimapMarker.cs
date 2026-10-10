using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>ชนิดของสิ่งที่จะแสดงบนมินิแมป</summary>
public enum MinimapMarkerType
{
    Player,
    Enemy,
    Coin,
    Item
}

/// <summary>
/// แปะสคริปต์นี้บน Player / Enemy / Coin / Item (หรือบน Prefab)
/// เมื่อวัตถุถูกปิดหรือถูกทำลาย (เช่นเก็บเหรียญแล้ว) ไอคอนจะหายจากแผนที่เอง
/// ถ้าไม่อยากแปะเอง ให้ใช้ Auto Mark By Tag ใน MinimapController แทน
/// </summary>
[DisallowMultipleComponent]
public class MinimapMarker : MonoBehaviour
{
    public static readonly List<MinimapMarker> All = new List<MinimapMarker>();
    public static event Action<MinimapMarker> MarkerEnabled;
    public static event Action<MinimapMarker> MarkerDisabled;

    public MinimapMarkerType type = MinimapMarkerType.Coin;

    [Tooltip("ไอคอนหมุนตามทิศที่วัตถุหัน (ใช้กับผู้เล่น)")]
    public bool rotateWithObject;

    [Tooltip("สีเฉพาะตัว ถ้า Alpha เป็น 0 จะใช้สีมาตรฐานของชนิดนั้น")]
    public Color overrideColor = new Color(0f, 0f, 0f, 0f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        All.Clear();
        MarkerEnabled = null;
        MarkerDisabled = null;
    }

    void Reset()
    {
        // ตั้งค่าเริ่มต้นให้เหมาะกับผู้เล่นตอนเพิ่มสคริปต์ใน Editor
        rotateWithObject = type == MinimapMarkerType.Player;
    }

    void OnEnable()
    {
        All.Add(this);
        if (MarkerEnabled != null) MarkerEnabled(this);
    }

    void OnDisable()
    {
        All.Remove(this);
        if (MarkerDisabled != null) MarkerDisabled(this);
    }
}
