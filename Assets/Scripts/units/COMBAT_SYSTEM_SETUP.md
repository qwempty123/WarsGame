# Simple top-down combat setup

This version is intentionally lightweight for a 2D top-down pixel-art RTS.

## How it works

- Every tank has one `armor` value and one health value.
- A shell penetrates when `penetration >= armor`.
- AP: balanced direct damage.
- HEAT: higher penetration and damage, slower reload.
- HE: low penetration, small direct damage if it fails to penetrate, plus area damage.
- Each shell type has its own finite amount and reload time.
- When the selected ammo is empty, the tank automatically switches to another loaded type.

## Unity setup

1. Update to this branch and let Unity recompile.
2. Select the tank prefab. Set `Tank_T90 → Armor` (for example: light tank 60, medium 100, heavy 160).
3. In `TankCombat → Ammo`, configure AP, HEAT and HE. Defaults are already provided in the list.
4. Assign the projectile prefab and fire point.
5. Ensure the projectile prefab has the `Projectile` component.

## Changing ammo from UI or input

Call these methods on `TankCombat`:
- `SelectShell(ShellType.AP)`
- `SelectShell(ShellType.HEAT)`
- `SelectShell(ShellType.HE)`
- `SelectNextShell()`
- `GetAmmo(ShellType.AP)`

The selected type and count are available as `SelectedShell` and `SelectedAmmoCount`.

## Ammo HUD and hotkeys

The current SampleScene already has a SelectionManager object with the
UnitSelection component. The HUD is drawn by that existing component using
Unity's built-in IMGUI, so no Canvas, EventSystem, extra prefab, or Inspector
references are required.

- Select one or more friendly tanks as usual.
- Press **1** to select AP, **2** to select HEAT, or **3** to select HE.
- The key press is sent through `TankCombat.SelectShell`; tanks without that
  shell loaded keep their current selection.
- With multiple tanks selected, the hotkey is applied to every selected tank
  that has `TankCombat`.
- The compact HUD appears in the lower-left corner while at least one selected
  tank has `TankCombat`. It displays AP, HEAT, and HE counts for the first
  selected combat tank, with the active shell highlighted.
- Ammo counts are read from `TankCombat.GetAmmo` and refresh automatically
  after firing, automatic ammo switching, or manual selection. The HUD does not
  maintain a separate ammo state.

The HUD is part of `UnitSelection`; if you use a different scene, make sure its
selection manager has this component enabled.

## Balance examples

| Shell | Damage | Penetration | Reload | Starting amount |
|---|---:|---:|---:|---:|
| AP | 35 | 150 | 2.5 s | 20 |
| HEAT | 50 | 220 | 3.5 s | 10 |
| HE | 15 | 20 | 3 s | 8 |

HE example: splash radius 1.5 world units, splash damage 25. These are gameplay values; adjust them to your sprite scale and desired time-to-kill.
## Reload and projectile speed settings

Both values are configured separately for each shell type on the tank prefab:

1. Select the tank prefab or tank GameObject.
2. Open the `TankCombat` component and expand the `Ammo` list.
3. Select an ammo entry (AP, HEAT, or HE) and edit:
   - **Перезарядка (сек.)** — delay after firing before the tank can fire again.
   - **Скорость снаряда** — projectile travel speed in world units per second.

The values are independent for AP, HEAT, and HE. For example, you can make AP reload quickly, HEAT reload slowly, and give each shell a different projectile speed. Changes are made in the Inspector; no code edits are required.

Current defaults:

| Shell | Reload | Projectile speed |
|---|---:|---:|
| AP | 2.5 s | 14 |
| HEAT | 3.5 s | 11 |
| HE | 3 s | 10 |

## Selection cleanup

Destroyed or missing tanks are automatically removed from the current selection during `UnitSelection.Update`. They no longer receive group commands or ammo hotkeys, and the ammo HUD stops using them.
