using UnityEngine;

public class Tank_T90 : MonoBehaviour
{
    [Header("Selection")]
    public GameObject selectionArrow;

    [Header("Team")]
    public int teamId = 0;

    [Header("Health & Armor")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0f)] public float armor = 100f;
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

    public void TakeShellDamage(float penetration, float damage, bool isExplosive, Vector3 hitPoint)
    {
        if (isDead) return;

        if (penetration >= armor)
            TakeDamage(damage, hitPoint);
        else if (isExplosive)
            TakeDamage(damage * 0.2f, hitPoint); // HE still deals a little impact damage.
        else if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
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