using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// ระบบเลือดของ Player — แปะที่ PlayerCapsule (ต้องมี Tag = Player)
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("UI (ไม่บังคับ)")]
    [SerializeField] private Slider healthSlider;   // ลาก Slider ของหลอดเลือดมาใส่

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onHealed;
    public UnityEvent onDeath;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        UpdateUI();
        onDamaged?.Invoke();

        if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateUI();
        onHealed?.Invoke();
    }

    private void Die()
    {
        IsDead = true;
        Debug.Log("Player died");
        onDeath?.Invoke();
        // TODO: เล่นอนิเมชันตาย / โหลดซีน Game Over ตรงนี้
    }

    private void UpdateUI()
    {
        if (healthSlider == null) return;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }
}
