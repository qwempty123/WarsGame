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
    [Min(0.1f), InspectorName("Перезарядка (сек.)"), Tooltip("Пауза между выстрелами этим типом снаряда.")]
    public float reloadTime = 2f;
    [Min(0.1f), InspectorName("Скорость снаряда"), Tooltip("Скорость полёта снаряда в мировых единицах в секунду.")]
    public float projectileSpeed = 12f;
    [Min(0f)] public float splashRadius = 0f;
    [Min(0f)] public float splashDamage = 0f;
}
public class TankCombat : MonoBehaviour
{
    [Header("Combat")]
    [Min(0.1f)] public float attackRange = 5f;
    [Range(0f, 45f)] public float aimTolerance = 5f;
    [Min(1f)] public float turretTurnSpeed = 120f;
    public GameObject projectilePrefab;
    public Transform turretTransform;
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
        if (turretTransform == null) Debug.LogError($"[{name}] Не назначен Turret Transform в TankCombat.", this);
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
            if (ammo[i] != null && ammo[i].type == type && ammo[i].amount > 0) { selectedAmmoIndex = i; return true; }
        return false;
    }
    public bool SelectNextShell()
    {
        if (ammo == null || ammo.Count == 0) return false;
        int start = ((selectedAmmoIndex % ammo.Count) + ammo.Count) % ammo.Count;
        for (int i = 1; i <= ammo.Count; i++)
        {
            int index = (start + i) % ammo.Count;
            if (ammo[index] != null && ammo[index].amount > 0) { selectedAmmoIndex = index; return true; }
        }
        return false;
    }
    public int GetAmmo(ShellType type)
    {
        if (ammo == null) return 0;
        int total = 0;
        foreach (AmmoSettings item in ammo) if (item != null && item.type == type) total += item.amount;
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
        if (currentTarget.IsDead) { currentTarget = null; movement?.Stop(); return; }
        if (turretTransform == null) return;
        RotateTurretTowards(currentTarget.transform.position);
        float distance = Vector2.Distance(transform.position, currentTarget.transform.position);
        if (distance > attackRange) { movement?.SetDestination(currentTarget.transform.position); return; }
        movement?.Stop();
        AmmoSettings selected = GetSelectedAmmo();
        if (selected == null || selected.amount <= 0)
        {
            if (!SelectNextShell()) return;
            selected = GetSelectedAmmo();
        }
        if (IsTurretAimedAt(currentTarget.transform.position) && Time.time >= nextFireTime)
            Shoot(selected, currentTarget);
    }
    private void RotateTurretTowards(Vector3 worldPosition)
    {
        Vector2 toTarget = (Vector2)(worldPosition - turretTransform.position);
        if (toTarget.sqrMagnitude < 0.001f) return;
        float forwardAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, forwardAngle + 180f); // башня смотрит в локальный -X
        turretTransform.rotation = Quaternion.RotateTowards(turretTransform.rotation, targetRotation, turretTurnSpeed * Time.deltaTime);
    }
    private bool IsTurretAimedAt(Vector3 worldPosition)
    {
        Vector2 toTarget = (Vector2)(worldPosition - turretTransform.position);
        if (toTarget.sqrMagnitude < 0.001f) return true;
        return Vector2.Angle(-turretTransform.right, toTarget.normalized) <= aimTolerance;
    }
    private void Shoot(AmmoSettings selected, Tank_T90 target)
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogError($"[{name}] Assign Projectile Prefab and Fire Point.", this);
            return;
        }
        Vector2 direction = -firePoint.right;
        GameObject obj = Instantiate(projectilePrefab, firePoint.position, Quaternion.FromToRotation(Vector3.right, direction));
        Projectile projectile = obj.GetComponent<Projectile>();
        if (projectile == null) { Debug.LogError("Projectile Prefab has no Projectile component.", obj); Destroy(obj); return; }
        projectile.Setup(direction, selected, myTank.teamId);
        selected.amount--;
        nextFireTime = Time.time + Mathf.Max(0.1f, selected.reloadTime);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}