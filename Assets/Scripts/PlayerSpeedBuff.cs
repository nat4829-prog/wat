using System.Collections;
using UnityEngine;
using StarterAssets;

/// <summary>
/// บัฟความเร็วของ Player (SpeedPotion จะเพิ่มสคริปต์นี้ให้อัตโนมัติ)
/// ใช้กับ Starter Assets - ThirdPersonController
/// ถ้าใช้ FirstPersonController ให้เปลี่ยนชนิดตัวแปร controller ด้านล่างเป็น FirstPersonController
/// </summary>
public class PlayerSpeedBuff : MonoBehaviour
{
    private FirstPersonController controller;
    private float baseMoveSpeed;
    private float baseSprintSpeed;
    private Coroutine running;

    private void Awake()
    {
        controller = GetComponent<FirstPersonController>();
        if (controller == null)
        {
            Debug.LogWarning("PlayerSpeedBuff: ไม่พบ ThirdPersonController บน Player");
            return;
        }
        baseMoveSpeed = controller.MoveSpeed;
        baseSprintSpeed = controller.SprintSpeed;
    }

    public void Apply(float multiplier, float duration)
    {
        if (controller == null) return;

        // เก็บโพชั่นซ้ำ = รีเซ็ตเวลา ไม่ซ้อนความเร็ว
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(BuffRoutine(multiplier, duration));
    }

    private IEnumerator BuffRoutine(float multiplier, float duration)
    {
        controller.MoveSpeed = baseMoveSpeed * multiplier;
        controller.SprintSpeed = baseSprintSpeed * multiplier;

        yield return new WaitForSeconds(duration);

        controller.MoveSpeed = baseMoveSpeed;
        controller.SprintSpeed = baseSprintSpeed;
        running = null;
    }

    private void OnDisable()
    {
        if (controller == null) return;
        controller.MoveSpeed = baseMoveSpeed;
        controller.SprintSpeed = baseSprintSpeed;
    }
}
