using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform target;
    private ShellType shellType;
    private int shooterTeamId;
    private float damage;
    private float penetration;
    private float speed;
    private float splashRadius;
    private float splashDamage;
    private float lifeTime = 5f;

    public void Setup(Transform targetTransform, AmmoSettings ammo, int teamId)
    {
        target = targetTransform;
        shooterTeamId = teamId;
        shellType = ammo.type;
        damage = ammo.damage;
        penetration = ammo.penetration;
        speed = ammo.projectileSpeed;
        splashRadius = ammo.splashRadius;
        splashDamage = ammo.splashDamage;
    }

    private void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f || target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 previousPosition = transform.position;
        Vector2 direction = ((Vector2)(target.position - transform.position)).normalized;
        float distance = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * distance);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        RaycastHit2D[] hits = Physics2D.RaycastAll(previousPosition, direction, distance);
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

            Destroy(gameObject);
            return;
        }
    }

    private void HitTank(Tank_T90 tank, Vector3 hitPoint)
    {
        // Armor is a single simple value: a shell penetrates when penetration >= armor.
        tank.TakeShellDamage(penetration, damage, shellType == ShellType.HE, hitPoint);

        if (shellType == ShellType.HE && splashRadius > 0f && splashDamage > 0f)
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(hitPoint, splashRadius);
            foreach (Collider2D col in nearby)
            {
                Tank_T90 other = col.GetComponentInParent<Tank_T90>();
                if (other == null || other == tank || other.IsDead || other.teamId == shooterTeamId) continue;

                float distance = Vector2.Distance(hitPoint, other.transform.position);
                float falloff = 1f - Mathf.Clamp01(distance / splashRadius);
                other.TakeDamage(splashDamage * falloff, hitPoint);
            }
        }

        Destroy(gameObject);
    }
}