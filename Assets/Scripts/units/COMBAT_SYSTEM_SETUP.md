# Hybrid combat system setup

The combat code targets Unity 2022.3 and keeps the existing RTS target/movement flow.

## Runtime flow

- `TankCombat` owns target tracking, selected shell, per-shell reload, and finite ammunition.
- `ShellData` is a ScriptableObject containing penetration, damage, reload, projectile speed, ricochet threshold, and optional splash values.
- `Projectile` performs swept 2D collision checks and resolves the first hostile tank or world obstacle on its path.
- `DamageCalculator` resolves the struck armor sector, impact angle, effective armor, ricochet, penetration, and direct damage.
- `Tank_T90` exposes front/side/rear armor and applies the resolved damage.

## Create shell assets

In the Unity Project window, use **Create → WarGame → Combat → Shell Data**. Create three assets and configure them as a starting balance pass:

| Property | APFSDS | HEAT | HE |
|---|---:|---:|---:|
| Shell Type | APFSDS | HEAT | HE |
| Penetration (mm) | 450 | 400 | 25 |
| Damage | 48 | 58 | 12 |
| Reload (s) | 3.0 | 4.0 | 3.0 |
| Projectile speed | 22 | 16 | 14 |
| Ricochet angle | 78° | 70° | 82° |
| Side/rear penetration multiplier | 1.0 | 1.0 | 1.0 |
| Splash radius | 0 | 0 | 2.0 |
| Splash damage | 0 | 0 | 24 |

These are initial gameplay tuning values, not real-world ammunition specifications. Tune against the actual armor values and game scale.

## Configure a tank prefab

1. Keep `Tank_T90`, `TankMovement`, and `TankCombat` on the tank root.
2. Assign `projectilePrefab` and `firePoint` on `TankCombat`. The projectile prefab needs a `Projectile` component and a 2D collider only if your existing projectile visual requires one; collision is queried along its movement path.
3. In **TankCombat → Ammunition**, add one slot for each shell asset.
4. Set each slot's `capacity` and `startingAmount`. `startingAmount` is clamped to capacity when the tank starts. Example: APFSDS 20/20, HEAT 12/12, HE 8/8.
5. Set front, side, and rear armor values on `Tank_T90`.

## Selecting ammunition

The selected shell is exposed by `TankCombat.SelectedShell`; current and maximum selected ammo are exposed by `SelectedAmmo` and `SelectedAmmoCapacity`.

Call these public methods from a HUD, input controller, or command system:
- `SelectShell(ShellType.APFSDS)`
- `SelectShell(ShellType.HEAT)`
- `SelectShell(ShellType.HE)`
- `SelectNextShell()`
- `GetAmmo(ShellType.HEAT)`

The tank automatically advances to the next loaded slot when the selected shell is depleted. If every slot is empty, it cannot fire. Reload duration is read from the shell asset at the moment of firing.

## Notes

- Armor penetration is deterministic in this hybrid implementation: penetration must meet or exceed effective armor. A ricochet occurs when impact angle reaches the shell's configured threshold.
- Effective armor increases with obliquity. The calculator uses the incoming direction and the outward hit normal approximated from the hit point and tank center.
- HE applies reduced direct damage against armor and optional radial splash damage to other hostile tanks. Splash falloff is linear.
- The legacy `TakeDamage(float, Vector3)` entry point and `referenceArmorMM` field remain for compatibility with older scripts/prefabs. New projectile hits use shell penetration instead.
- No automatic resupply is implemented. Ammunition remains finite for the lifetime of the tank.
