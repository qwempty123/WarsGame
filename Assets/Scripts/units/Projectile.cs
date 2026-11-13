using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float defaultSpeed = 10f;
    private Transform target;
    private ShellData shell;
    private int shooterTeamId = -1;
    private float remainingLifetime = 5f;
    private bool hasImpacted;

    public void Setup(Transform targetTransform, ShellData shellData, int teamId)
    {
        target = targetTransform;
        shell = shellData;
        shooterTeamId = teamId;
        if (shell != null)
        {
            remainingLifetime = shell.lifetime;
            defaultSpeed = shell.projectileSpeed;
        }
    }

    // Compatibility overload for older callers. New combat code should pass ShellData.
    public void Setup(Transform targetTransform, float legacyDamage, int teamId)
    {
        target = targetTransform;
        shooterTeamId = teamId;
        remainingLifetime = 3f;
    }

    private void Update()
    {
        if (hasImpacted) return;
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f || target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 previousPosition = transform.position;
        Vector2 direction = ((Vector2)(target.position - transform.position)).normalized;
        float stepDistance = defaultSpeed * Time.deltaTime;
        Vector3 movement = (Vector3)(direction * stepDistance);
        transform.position += movement;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        RaycastHit2D[] hits = Physics2D.RaycastAll(previousPosition, direction, movement.magnitude);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit2D hit in hits)
        {
            Tank_T90 tank = hit.collider.GetComponentInParent<Tank_T90>();
            if (tank != null)
            {
                if (tank.teamId == shooterTeamId || tank.IsDead) continue;
                ResolveTankImpact(tank, hit.point, direction);
                return;
            }

            hasImpacted = true;
            Destroy(gameObject);
            return;
        }
    }

    private void ResolveTankImpact(Tank_T90 targetTank, Vector3 hitPoint, Vector2 direction)
    {
        hasImpacted = true;
        if (shell == null)
        {
            targetTank.TakeDamage(10f, hitPoint);
            Destroy(gameObject);
            return;
        }

        ShellImpactResult result = DamageCalculator.Calculate(shell, targetTank, direction, hitPoint);
        targetTank.ApplyShellImpact(result, hitPoint, shell.shellType);

        if (shell.splashRadius > 0f && shell.splashDamage > 0f)
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(hitPoint, shell.splashRadius);
            foreach (Collider2D collider in nearby)
            {
                Tank_T90 nearbyTank = collider.GetComponentInParent<Tank_T90>();
                if (nearbyTank == null || nearbyTank == targetTank || nearbyTank.IsDead || nearbyTank.teamId == shooterTeamId) continue;
                float distance = Vector2.Distance(hitPoint, nearbyTank.transform.position);
                float falloff = 1f - Mathf.Clamp01(distance / shell.splashRadius);
                nearbyTank.TakeDamage(shell.splashDamage * falloff, collider.ClosestPoint(hitPoint));
            }
        }

        Debug.Log($"[{shell.displayName}] {(result.ricocheted ? "РИКОШЕТ" : result.penetrated ? "ПРОБИТИЕ" : "НЕПРОБИТИЕ")} | броня {result.effectiveArmorMM:F0} мм | пробитие {result.penetrationMM:F0} мм | урон {result.damage:F1}");
        Destroy(gameObject);
    }
}