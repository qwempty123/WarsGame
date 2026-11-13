using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    private Transform target;
    private float damage;
    private float lifetime = 3f;
    private int myTeamId = -1;

    public void Setup(Transform targetTransform, float dmg, int teamId)
    {
        target = targetTransform;
        damage = dmg;
        myTeamId = teamId;
    }

    void Update()
    {
        lifetime -= Time.deltaTime;

        if (lifetime <= 0f || target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Запоминаем прошлую позицию
        Vector3 prevPos = transform.position;

        // Двигаем пулю
        Vector2 dir = (target.position - transform.position).normalized;
        Vector3 moveStep = (Vector3)dir * speed * Time.deltaTime;
        transform.position += moveStep;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Ищем ВСЕ объекты на пути пули
        RaycastHit2D[] hits = Physics2D.RaycastAll(prevPos, dir, moveStep.magnitude);

        // Сортируем их по дистанции (от ближнего к дальнему)
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (var hit in hits)
        {
            Tank_T90 hitTank = hit.collider.GetComponentInParent<Tank_T90>();

            if (hitTank != null)
            {
                // Если это ВРАГ и он жив
                if (hitTank.teamId != myTeamId && !hitTank.IsDead)
                {
                    hitTank.TakeDamage(damage, hit.point);
                    Destroy(gameObject);
                    return;
                }
                // Если это СОЮЗНИК (или труп) — просто пропускаем его и летим дальше!
            }
            else
            {
                // Если врезались в стену/препятствие
                Destroy(gameObject);
                return;
            }
        }
    }
}