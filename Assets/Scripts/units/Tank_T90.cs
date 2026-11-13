using UnityEngine;

public enum HitSide { Front, Side, Rear }

public class Tank_T90 : MonoBehaviour
{
    [Header("Selection")]
    public GameObject selectionArrow;

    [Header("Team")]
    public int teamId = 0;

    [Header("Health")]
    [Min(1f)] public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }

    [Header("Armor (mm)")]
    [Min(1f)] public float frontArmorMM = 200f;
    [Min(1f)] public float sideArmorMM = 80f;
    [Min(1f)] public float rearArmorMM = 40f;

    [Header("Legacy compatibility")]
    [Tooltip("Retained for compatibility with old prefabs/scripts. Shell penetration now determines armor defeat.")]
    public float referenceArmorMM = 100f;

    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public GameObject explosionPrefab;

    private Animator animator;
    private TankMovement movement;
    private bool isDead;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        animator = GetComponent<Animator>();
        movement = GetComponent<TankMovement>();
        if (selectionArrow != null) selectionArrow.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (!isDead && selectionArrow != null) selectionArrow.SetActive(selected);
    }

    public HitSide GetHitSide(Vector3 hitPoint)
    {
        if (movement == null) return HitSide.Side;
        Vector3 toHit = hitPoint - transform.position;
        float hitAngle = Mathf.Atan2(toHit.y, toHit.x) * Mathf.Rad2Deg;
        float delta = Mathf.Abs(Mathf.DeltaAngle(hitAngle, movement.VisualAngle));
        if (delta < 45f) return HitSide.Front;
        if (delta < 135f) return HitSide.Side;
        return HitSide.Rear;
    }

    public float GetArmorMM(HitSide side)
    {
        switch (side)
        {
            case HitSide.Front: return frontArmorMM;
            case HitSide.Rear: return rearArmorMM;
            default: return sideArmorMM;
        }
    }

    public void ApplyShellImpact(ShellImpactResult impact, Vector3 hitPoint, ShellType shellType)
    {
        if (isDead) return;
        if (impact.damage > 0f) ApplyDamage(impact.damage, hitPoint);
        else if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
    }

    // Legacy damage entry point retained for other gameplay scripts.
    public void TakeDamage(float rawDamage, Vector3 hitPoint)
    {
        if (isDead || rawDamage <= 0f) return;
        ApplyDamage(rawDamage, hitPoint);
    }

    private void ApplyDamage(float damage, Vector3 hitPoint)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
        Debug.Log($"[Tank {teamId}] Получено {damage:F1} урона | HP: {CurrentHealth:F1}/{maxHealth:F1}");
        if (CurrentHealth <= 0f) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        movement?.Stop();

        TankCombat combat = GetComponent<TankCombat>();
        if (combat != null) combat.enabled = false;

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        if (selectionArrow != null) selectionArrow.SetActive(false);
        if (animator != null)
        {
            animator.SetInteger("DeathType", Random.Range(0, 3));
            animator.SetTrigger("Die");
        }
        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
    }

    public bool IsDead => isDead;
}