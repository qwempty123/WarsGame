using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShellAmmoSlot
{
    public ShellData shell;
    [Min(0)] public int capacity = 20;
    [Min(0)] public int startingAmount = 20;
    [HideInInspector] public int remaining;

    public void Initialize()
    {
        remaining = Mathf.Clamp(startingAmount, 0, capacity);
    }
}

public class TankCombat : MonoBehaviour
{
    [Header("Targeting")]
    [Min(0.1f)] public float attackRange = 5f;
    [Range(0f, 45f)] public float aimTolerance = 5f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Ammunition")]
    [SerializeField] private List<ShellAmmoSlot> ammunition = new List<ShellAmmoSlot>();
    [SerializeField] private int selectedAmmoIndex;
    [SerializeField] private bool refillOnStart = true;

    private float nextFireTime;
    private Tank_T90 myTank;
    private TankMovement movement;
    private Tank_T90 currentTarget;
    private bool initialized;

    public ShellData SelectedShell => GetSelectedSlot()?.shell;
    public int SelectedAmmo => GetSelectedSlot()?.remaining ?? 0;
    public int SelectedAmmoCapacity => GetSelectedSlot()?.capacity ?? 0;
    public IReadOnlyList<ShellAmmoSlot> Ammunition => ammunition;

    private void Awake()
    {
        myTank = GetComponent<Tank_T90>();
        movement = GetComponent<TankMovement>();
        InitializeAmmo();
    }

    private void InitializeAmmo()
    {
        if (initialized) return;
        foreach (ShellAmmoSlot slot in ammunition)
        {
            if (slot == null) continue;
            slot.remaining = refillOnStart
                ? Mathf.Clamp(slot.startingAmount, 0, slot.capacity)
                : Mathf.Clamp(slot.remaining, 0, slot.capacity);
        }
        selectedAmmoIndex = FindLoadedSlot(selectedAmmoIndex);
        initialized = true;
    }

    public void SetTarget(Tank_T90 enemy)
    {
        if (enemy == null || myTank == null || enemy.teamId == myTank.teamId || enemy.IsDead) return;
        currentTarget = enemy;
    }

    public void ClearTarget() => currentTarget = null;

    public bool SelectShell(ShellType type)
    {
        for (int i = 0; i < ammunition.Count; i++)
        {
            ShellAmmoSlot slot = ammunition[i];
            if (slot != null && slot.shell != null && slot.shell.shellType == type && slot.remaining > 0)
            {
                selectedAmmoIndex = i;
                return true;
            }
        }
        return false;
    }

    public bool SelectNextShell()
    {
        if (ammunition.Count == 0) return false;
        for (int offset = 1; offset <= ammunition.Count; offset++)
        {
            int index = (selectedAmmoIndex + offset) % ammunition.Count;
            ShellAmmoSlot slot = ammunition[index];
            if (slot != null && slot.shell != null && slot.remaining > 0)
            {
                selectedAmmoIndex = index;
                return true;
            }
        }
        return false;
    }

    public int GetAmmo(ShellType type)
    {
        int total = 0;
        foreach (ShellAmmoSlot slot in ammunition)
            if (slot != null && slot.shell != null && slot.shell.shellType == type)
                total += slot.remaining;
        return total;
    }

    private ShellAmmoSlot GetSelectedSlot()
    {
        if (ammunition == null || selectedAmmoIndex < 0 || selectedAmmoIndex >= ammunition.Count) return null;
        return ammunition[selectedAmmoIndex];
    }

    private int FindLoadedSlot(int preferred)
    {
        if (ammunition.Count == 0) return 0;
        preferred = Mathf.Clamp(preferred, 0, ammunition.Count - 1);
        for (int offset = 0; offset < ammunition.Count; offset++)
        {
            int index = (preferred + offset) % ammunition.Count;
            ShellAmmoSlot slot = ammunition[index];
            if (slot != null && slot.shell != null && slot.remaining > 0) return index;
        }
        return preferred;
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

        float distance = Vector2.Distance(transform.position, currentTarget.transform.position);
        if (distance > attackRange)
        {
            movement?.SetDestination(currentTarget.transform.position);
            return;
        }

        movement?.Stop();
        movement?.RotateTowards(currentTarget.transform.position);

        ShellAmmoSlot slot = GetSelectedSlot();
        if (slot == null || slot.shell == null || slot.remaining <= 0)
        {
            if (SelectNextShell()) slot = GetSelectedSlot();
            else return;
        }

        if (movement != null && movement.IsFacing(currentTarget.transform.position, aimTolerance) && Time.time >= nextFireTime)
            Shoot(slot, currentTarget);
    }

    private void Shoot(ShellAmmoSlot slot, Tank_T90 target)
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogError($"[{name}] Не назначены Projectile Prefab или FirePoint.", this);
            return;
        }

        GameObject projectileObject = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.LogError($"[{name}] На Projectile Prefab отсутствует компонент Projectile.", projectileObject);
            Destroy(projectileObject);
            return;
        }

        projectile.Setup(target.transform, slot.shell, myTank.teamId);
        slot.remaining--;
        nextFireTime = Time.time + Mathf.Max(0.1f, slot.shell.reloadSeconds);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}