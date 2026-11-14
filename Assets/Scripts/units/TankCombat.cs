using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShellType { AP, HEAT, HE }

[Serializable]
public class AmmoSettings
{
    public ShellType type;
    [Min(0)] public int amount = 20;
    [Min(0f)] public float damage = 30f;
    [Min(0f)] public float penetration = 100f;
    [Min(0.1f)]
    [InspectorName("Перезарядка (сек.)")]
    [Tooltip("Пауза между выстрелами этим типом снаряда.")]
    public float reloadTime = 2f;

    [Min(0.1f)]
    [InspectorName("Скорость снаряда")]
    [Tooltip("Скорость полёта снаряда в мировых единицах в секунду.")]
    public float projectileSpeed = 12f;
    [Min(0f)] public float splashRadius = 0f;
    [Min(0f)] public float splashDamage = 0f;
}

public class TankCombat : MonoBehaviour
{
    [Header("Дальность и разброс")]
    [Min(1f)] public float maxAttackRangeMeters = 10000f;
    [Min(0.01f)] public float metersPerWorldUnit = 7f;
    [Min(0f)] public float minScatterRadiusMeters = 2f;
    [Min(0f)] public float maxScatterRadiusMeters = 150f;

    // Оставлено публичным для отображения дальности в UnitSelection и Gizmos.
    public float attackRange => maxAttackRangeMeters / Mathf.Max(0.01f, metersPerWorldUnit);

    [Header("Combat")]
    [Range(0f, 45f)] public float aimTolerance = 5f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Ammo")]
    public List<AmmoSettings> ammo = new List<AmmoSettings>
    {
        new AmmoSettings { type = ShellType.AP, amount = 20, damage = 35f, penetration = 150f, reloadTime = 2.5f, projectileSpeed = 14f },
        new AmmoSettings { type = ShellType.HEAT, amount = 10, damage = 50f, penetration = 220f, reloadTime = 3.5f, projectileSpeed = 11f },
        new AmmoSettings { type = ShellType.HE, amount = 8, damage = 15f, penetration = 20f, reloadTime = 3f, projectileSpeed = 10f, splashRadius = 1.5f, splashDamage = 25f }
    };

    [SerializeField] private int selectedAmmoIndex;

    private float nextFireTime;
    private Tank_T90 myTank;
    private TankMovement movement;
    private Tank_T90 currentTarget;

    public ShellType SelectedShell => GetSelectedAmmo()?.type ?? ShellType.AP;
    public int SelectedAmmoCount => GetSelectedAmmo()?.amount ?? 0;

    private void Awake()
    {
        myTank = GetComponent<Tank_T90>();
        movement = GetComponent<TankMovement>();
        selectedAmmoIndex = FindLoadedAmmo(selectedAmmoIndex);
    }

    public void SetTarget(Tank_T90 enemy)
    {
        if (enemy == null || myTank == null || enemy.teamId == myTank.teamId || enemy.IsDead) return;
        currentTarget = enemy;
    }

    public void ClearTarget() => currentTarget = null;

    public bool SelectShell(ShellType type)
    {
        if (ammo == null) return false;

        for (int i = 0; i < ammo.Count; i++)
        {
            if (ammo[i] != null && ammo[i].type == type && ammo[i].amount > 0)
            {
                selectedAmmoIndex = i;
                return true;
            }
        }
        return false;
    }

    public bool SelectNextShell()
    {
        if (ammo == null || ammo.Count == 0) return false;

        int startIndex = ((selectedAmmoIndex % ammo.Count) + ammo.Count) % ammo.Count;
        for (int i = 1; i <= ammo.Count; i++)
        {
            int index = (startIndex + i) % ammo.Count;
            if (ammo[index] != null && ammo[index].amount > 0)
            {
                selectedAmmoIndex = index;
                return true;
            }
        }
        return false;
    }

    public int GetAmmo(ShellType type)
    {
        if (ammo == null) return 0;

        int total = 0;
        foreach (AmmoSettings item in ammo)
            if (item != null && item.type == type) total += item.amount;
        return total;
    }

    private AmmoSettings GetSelectedAmmo()
    {
        if (ammo == null || selectedAmmoIndex < 0 || selectedAmmoIndex >= ammo.Count) return null;
        return ammo[selectedAmmoIndex];
    }

    private int FindLoadedAmmo(int start)
    {
        if (ammo == null || ammo.Count == 0) return 0;
        for (int i = 0; i < ammo.Count; i++)
        {
            int index = (((start % ammo.Count) + ammo.Count) % ammo.Count + i) % ammo.Count;
            if (ammo[index] != null && ammo[index].amount > 0) return index;
        }
        return 0;
    }

    private void Update()
    {
        if (currentTarget == null) return;
        if (currentTarget.IsDead)
        {
            currentTarget = null;
            movement?.Stop();
            return;
        }

        if (Vector2.Distance(transform.position, currentTarget.transform.position) > attackRange)
        {
            movement?.SetDestination(currentTarget.transform.position);
            return;
        }

        movement?.Stop();
        movement?.RotateTowards(currentTarget.transform.position);

        AmmoSettings selected = GetSelectedAmmo();
        if (selected == null || selected.amount <= 0)
        {
            if (!SelectNextShell()) return;
            selected = GetSelectedAmmo();
        }

        if (movement != null && movement.IsFacing(currentTarget.transform.position, aimTolerance) && Time.time >= nextFireTime)
            Shoot(selected, currentTarget);
    }

    private void Shoot(AmmoSettings selected, Tank_T90 target)
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogError($"[{name}] Assign Projectile Prefab and Fire Point.", this);
            return;
        }

        GameObject obj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile projectile = obj.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.LogError("Projectile Prefab has no Projectile component.", obj);
            Destroy(obj);
            return;
        }

        float distanceMeters = Vector2.Distance(transform.position, target.transform.position) * metersPerWorldUnit;
        float range01 = Mathf.Clamp01(distanceMeters / Mathf.Max(1f, maxAttackRangeMeters));
        float scatterRadiusMeters = Mathf.Lerp(minScatterRadiusMeters, maxScatterRadiusMeters, range01);
        float scatterRadiusWorld = scatterRadiusMeters / Mathf.Max(0.01f, metersPerWorldUnit);
        Vector2 scatterOffset = UnityEngine.Random.insideUnitCircle * scatterRadiusWorld;
        Vector3 impactPoint = target.transform.position + (Vector3)scatterOffset;

        // Точка попадания фиксируется при выстреле: снаряд больше не преследует цель.
        projectile.Setup(impactPoint, selected, myTank.teamId);
        selected.amount--;
        nextFireTime = Time.time + Mathf.Max(0.1f, selected.reloadTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}