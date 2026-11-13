using UnityEngine;

public class TankCombat : MonoBehaviour
{
    [Header("Характеристики стрельбы")]
    public float attackRange = 5f;
    public float damage = 25f;

    [Tooltip("Время перезарядки в секундах (1 = выстрел раз в секунду, 2 = раз в 2 секунды)")]
    public float reloadTime = 2f;

    [Tooltip("Максимальный угол (в градусах) между носом и целью, при котором разрешено стрелять")]
    public float aimTolerance = 5f;

    public GameObject projectilePrefab;
    public Transform firePoint;

    private float nextFireTime = 0f;
    private Tank_T90 myTank;
    private TankMovement movement;
    private Tank_T90 currentTarget;

    void Start()
    {
        myTank = GetComponent<Tank_T90>();
        movement = GetComponent<TankMovement>();

        if (reloadTime <= 0.1f) reloadTime = 0.1f;
    }

    public void SetTarget(Tank_T90 enemy)
    {
        if (enemy == null) return;
        if (enemy.teamId == myTank.teamId) return;
        if (enemy.IsDead) return; // Нельзя приказать атаковать труп

        currentTarget = enemy;
    }

    public void ClearTarget()
    {
        currentTarget = null;
    }

    void Update()
    {
        if (currentTarget == null) return;

        // Если цель удалена ИЛИ цель мертва — сбрасываем приказ и останавливаемся
        if (currentTarget.gameObject == null || currentTarget.IsDead)
        {
            currentTarget = null;
            movement.Stop();
            return;
        }

        float dist = Vector2.Distance(transform.position, currentTarget.transform.position);

        if (dist > attackRange)
        {
            // Враг далеко - едем к нему
            movement.SetDestination(currentTarget.transform.position);
        }
        else
        {
            // Враг в зоне видимости - стоим и доворачиваем корпус
            movement.Stop();
            movement.RotateTowards(currentTarget.transform.position);

            bool facingTarget = movement.IsFacing(currentTarget.transform.position, aimTolerance);
            bool isReloaded = Time.time >= nextFireTime;

            if (facingTarget && isReloaded)
            {
                Shoot(currentTarget);
                nextFireTime = Time.time + reloadTime;
            }
        }
    }

    private void Shoot(Tank_T90 target)
    {
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile projScript = proj.GetComponent<Projectile>();
            if (projScript != null)
            {
                // Передаем цель, урон и НАШ ID (myTank.teamId)
                projScript.Setup(target.transform, damage, myTank.teamId);
            }
        }
        else
        {
            Debug.LogError("Не назначен Prefab пули или FirePoint!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}