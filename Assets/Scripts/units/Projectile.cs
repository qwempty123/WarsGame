using UnityEngine;
public class Projectile : MonoBehaviour
{
    private Vector2 direction;
    private ShellType shellType;
    private int shooterTeamId;
    private float damage, penetration, speed, splashRadius, splashDamage;
    private float lifeTime = 5f;
    public void Setup(Vector2 shotDirection, AmmoSettings ammo, int teamId)
    {
        direction = shotDirection.normalized;
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
        if (lifeTime <= 0f || direction.sqrMagnitude < 0.001f) { Destroy(gameObject); return; }
        Vector3 previousPosition = transform.position;
        float distance = speed * Time.deltaTime;
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
        transform.position += (Vector3)(direction * distance);
    }
    private void HitTank(Tank_T90 tank, Vector3 hitPoint)
    {
        tank.TakeShellDamage(penetration, damage, shellType == ShellType.HE, hitPoint);
        if (shellType == ShellType.HE && splashRadius > 0f && splashDamage > 0f)
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(hitPoint, splashRadius);
            foreach (Collider2D col in nearby)
            {
                Tank_T90 other = col.GetComponentInParent<Tank_T90>();
                if (other == null || other == tank || other.IsDead || other.teamId == shooterTeamId) continue;
                float dist = Vector2.Distance(hitPoint, other.transform.position);
                float falloff = 1f - Mathf.Clamp01(dist / splashRadius);
                other.TakeDamage(splashDamage * falloff, hitPoint);
            }
        }
        Destroy(gameObject);
    }
}