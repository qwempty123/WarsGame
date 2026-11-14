using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Vector3 impactPosition;
    private ShellType shellType;
    private int shooterTeamId;
    private float damage;
    private float penetration;
    private float speed;
    private float splashRadius;
    private float splashDamage;
    private float lifeTime = 180f;

    public void Setup(Vector3 targetPosition, AmmoSettings ammo, int teamId)
    {
        impactPosition = targetPosition;
        shooterTeamId = teamId;
        shellType = ammo.type;
        damage = ammo.damage;
        penetration = ammo.penetration;
        speed = Mathf.Max(0.1f, ammo.projectileSpeed);
        splashRadius = ammo.splashRadius;
        splashDamage = ammo.splashDamage;
    }

    private void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 toImpact = (Vector2)(impactPosition - transform.position);
        float remainingDistance = toImpact.magnitude;

        if (remainingDistance <= 0.01f)
        {
            Detonate(impactPosition, null);
            return;
        }

        Vector2 direction = toImpact / remainingDistance;
        float travelDistance = Mathf.Min(speed * Time.deltaTime, remainingDistance);
        Vector3 previousPosition = transform.position;

        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        RaycastHit2D[] hits = Physics2D.RaycastAll(previousPosition, direction, travelDistance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit2D hit in hits)
        {
            Tank_T90 tank = hit.collider.GetComponentInParent<Tank_T90>();
            if (tank != null)
            {
                if (tank.teamId == shooterTeamId || tank.IsDead) continue;

                HitTank(tank, hit.point);
                return;
            }

            // Столкновение с препятствием прерывает полёт.
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(direction * travelDistance);

        if (travelDistance >= remainingDistance)
            Detonate(impactPosition, null);
    }

    private void HitTank(Tank_T90 tank, Vector3 hitPoint)
    {
        tank.TakeShellDamage(penetration, damage, shellType == ShellType.HE, hitPoint);
        Detonate(hitPoint, tank);
    }

    private void Detonate(Vector3 point, Tank_T90 directHit)
    {
        if (shellType == ShellType.HE && splashRadius > 0f && splashDamage > 0f)
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(point, splashRadius);
            foreach (Collider2D col in nearby)
            {
                Tank_T90 other = col.GetComponentInParent<Tank_T90>();
                if (other == null || other == directHit || other.IsDead || other.teamId == shooterTeamId) continue;

                float distance = Vector2.Distance(point, other.transform.position);
                float falloff = 1f - Mathf.Clamp01(distance / splashRadius);
                other.TakeDamage(splashDamage * falloff, point);
            }
        }

        Destroy(gameObject);
    }
}