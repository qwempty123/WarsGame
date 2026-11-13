using UnityEngine;

public enum HitSide { Front, Side, Rear }

public class Tank_T90 : MonoBehaviour
{
    [Header("Выделение")]
    public GameObject selectionArrow;

    [Header("Команда")]
    public int teamId = 0;

    [Header("Здоровье")]
    public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }

    [Header("Бронирование (мм)")]
    [Tooltip("Лоб. Чем толще — тем меньше урона спереди")]
    public float frontArmorMM = 200f;
    [Tooltip("Борт")]
    public float sideArmorMM = 80f;
    [Tooltip("Корма. Мало = много урона сзади")]
    public float rearArmorMM = 40f;

    [Header("Настройка баланса")]
    [Tooltip("Эталонная толщина брони (при такой урон идёт 1x). По умолчанию 100мм")]
    public float referenceArmorMM = 100f;

    [Header("Эффекты")]
    public GameObject hitEffectPrefab;
    public GameObject explosionPrefab;

    private Animator animator;
    private TankMovement movement;
    private bool isDead = false;

    void Start()
    {
        CurrentHealth = maxHealth;
        animator = GetComponent<Animator>();
        movement = GetComponent<TankMovement>();
        if (selectionArrow != null) selectionArrow.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (isDead) return;
        if (selectionArrow != null) selectionArrow.SetActive(selected);
    }

    public void TakeDamage(float rawDamage, Vector3 hitPoint)
    {
        if (isDead) return;

        HitSide side = GetHitSide(hitPoint);
        float armorMM = GetArmorMM(side);

        // Коэффициент урона: эталон / реальная броня
        // 100мм = 1x, 200мм = 0.5x, 50мм = 2x
        float multiplier = Mathf.Clamp(referenceArmorMM / Mathf.Max(1f, armorMM), 0.1f, 3f);
        float effectiveDamage = rawDamage * multiplier;

        CurrentHealth -= effectiveDamage;

        // Лог с цветом — для удобства баланса
        string sideName = side == HitSide.Front ? "<color=#66ff66>ЛОБ</color>"
                        : side == HitSide.Rear ? "<color=#ff6666>КОРМА</color>"
                                                 : "<color=#ffcc00>БОРТ</color>";
        Debug.Log($"[Танк {teamId}] {sideName} ({armorMM}мм) | {rawDamage:F0} × {multiplier:F2} = <b>{effectiveDamage:F1}</b> HP | Осталось {Mathf.Max(0, CurrentHealth):F0}");

        // Эффект попадания в точке касания (на краю коллайдера)
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
    }

    // Определяем, куда прилетело: лоб / борт / корма
    private HitSide GetHitSide(Vector3 hitPoint)
    {
        if (movement == null) return HitSide.Side;

        float noseAngle = movement.VisualAngle;
        Vector3 toHit = hitPoint - transform.position;
        float hitAngle = Mathf.Atan2(toHit.y, toHit.x) * Mathf.Rad2Deg;

        // Разница между направлением носа и направлением на точку попадания
        float delta = Mathf.Abs(Mathf.DeltaAngle(hitAngle, noseAngle));

        if (delta < 45f) return HitSide.Front;  // ±45° от носа — лоб
        if (delta < 135f) return HitSide.Side;   // ±90° сектор — борт
        return HitSide.Rear;                     // всё остальное — корма
    }

    private float GetArmorMM(HitSide side)
    {
        switch (side)
        {
            case HitSide.Front: return frontArmorMM;
            case HitSide.Rear: return rearArmorMM;
            default: return sideArmorMM;
        }
    }

    private void Die()
    {
        isDead = true;
        if (movement != null) movement.Stop();

        TankCombat combat = GetComponent<TankCombat>();
        if (combat != null) combat.enabled = false;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders) col.enabled = false;

        if (selectionArrow != null) selectionArrow.SetActive(false);

        if (animator != null)
        {
            int randomDeath = Random.Range(0, 3);
            animator.SetInteger("DeathType", randomDeath);
            animator.SetTrigger("Die");
        }
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
    }

    public bool IsDead => isDead;
}