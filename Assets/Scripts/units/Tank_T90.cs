using UnityEngine;

public class Tank_T90 : MonoBehaviour
{
    [Header("Selection")]
    public GameObject selectionArrow;

    [Header("Team")]
    public int teamId = 0;

    [Header("Health & Armor")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0f), Tooltip("Базовая броня лобовой проекции.")]
    public float armor = 100f;
    [Range(0.1f, 1f), Tooltip("Доля лобовой брони для бортового попадания.")]
    public float sideArmorMultiplier = 0.6f;
    [Range(0.1f, 1f), Tooltip("Доля лобовой брони для попадания в корму.")]
    public float rearArmorMultiplier = 0.4f;
    [Range(30f, 160f), Tooltip("Полная ширина лобовой зоны в градусах.")]
    public float frontArmorArc = 120f;

    [Header("Damage By Hit Zone")]
    [Range(0.1f, 2f), Tooltip("Множитель урона при попадании в лоб.")]
    public float frontDamageMultiplier = 0.75f;
    [Range(0.1f, 2f), Tooltip("Множитель урона при попадании в борт.")]
    public float sideDamageMultiplier = 1f;
    [Range(0.1f, 2f), Tooltip("Множитель урона при попадании в корму.")]
    public float rearDamageMultiplier = 1.25f;

    public float CurrentHealth { get; private set; }

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

    public void TakeShellDamage(float penetration, float damage, bool isExplosive, Vector3 hitPoint, Vector2 projectileDirection)
    {
        if (isDead) return;

        ArmorZone zone = GetArmorZone(projectileDirection);
        float effectiveArmor = GetEffectiveArmor(zone);

        Debug.Log($"[Tank {teamId}] Hit zone: {zone} | Armor: {effectiveArmor:F1} | Penetration: {penetration:F1}");

        if (penetration >= effectiveArmor)
        {
            float damageMultiplier = GetZoneDamageMultiplier(zone);
            float finalDamage = damage * damageMultiplier;
            Debug.Log($"[Tank {teamId}] Penetrated {zone} | Damage: {finalDamage:F1} (x{damageMultiplier:F2})");
            TakeDamage(finalDamage, hitPoint);
        }
        else if (isExplosive)
        {
            // HE может нанести ограниченный урон даже без пробития.
            float blockedDamage = damage * 0.2f;
            Debug.Log($"[Tank {teamId}] HE blocked by {zone} armor | Damage: {blockedDamage:F1}");
            TakeDamage(blockedDamage, hitPoint);
        }
        else
        {
            Debug.Log($"[Tank {teamId}] Shell bounced: {zone} armor stopped penetration.");
            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
        }
    }

    private enum ArmorZone { Front, Side, Rear }

    private ArmorZone GetArmorZone(Vector2 projectileDirection)
    {
        Vector2 hullForward = movement != null
            ? new Vector2(Mathf.Cos(movement.VisualAngle * Mathf.Deg2Rad), Mathf.Sin(movement.VisualAngle * Mathf.Deg2Rad))
            : (Vector2)transform.right;

        // Направление от танка к стрелявшему: снаряд летит в противоположную сторону.
        Vector2 directionToShooter = -projectileDirection.normalized;
        float angle = Vector2.Angle(hullForward, directionToShooter);

        if (angle <= frontArmorArc * 0.5f) return ArmorZone.Front;
        if (angle >= 180f - frontArmorArc * 0.5f) return ArmorZone.Rear;
        return ArmorZone.Side;
    }

    private float GetEffectiveArmor(ArmorZone zone)
    {
        switch (zone)
        {
            case ArmorZone.Side: return armor * sideArmorMultiplier;
            case ArmorZone.Rear: return armor * rearArmorMultiplier;
            default: return armor;
        }
    }

    private float GetZoneDamageMultiplier(ArmorZone zone)
    {
        switch (zone)
        {
            case ArmorZone.Front: return frontDamageMultiplier;
            case ArmorZone.Side: return sideDamageMultiplier;
            case ArmorZone.Rear: return rearDamageMultiplier;
            default: return 1f;
        }
    }

    // Used by splash damage and other gameplay scripts.
    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (isDead || damage <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
        Debug.Log($"[Tank {teamId}] Damage: {damage:F1} | HP: {CurrentHealth:F1}/{maxHealth:F1}");

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