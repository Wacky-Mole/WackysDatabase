# WackyDB SecondaryAttacks

Optional BepInEx companion for WackysDatabase 2.6.14+. It gives secondary attacks independent damage using WackyDB item YAML. WackyDB alone does not install these combat patches.

## Install and configure

Install on Clients and Server.

Local Debug and Release builds also copy the companion DLL to `C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins`, matching the main WackyDB build.

```yaml
name: SwordIron
Secondary_Attack:
  Damage:
    Slash: 90
    Pierce: 20
  Damage_Per_Level:
    Slash: 6
```

- Supported only inside `Secondary_Attack`; primary damage stays in the item's top-level blocks.
- Each provided block replaces that entire base/per-level block. Omitted damage types in a provided block become zero, including per-level damage if you supply an empty block.
- Each omitted block independently falls back to the weapon's normal shared damage. Omit both for existing behavior.
- Quality and world-level scaling remain native, as do `DmgMultiplier`, chain bonuses, skill scaling, status effects, and other downstream attack modifiers.
- Handles melee, area hits, and projectile bursts. Ammunition damage is not replaced.
- The tooltip appends secondary base damage using the native damage tooltip formatting, followed by the secondary damage multiplier (for example, `3x`). That multiplier applies to the listed base damage: Slash 90 with `DmgMultiplier: 3` becomes Slash 270 before other combat modifiers. Chain bonuses, ammunition and other modifiers are not included in this example; it is not a prediction of final damage dealt.
- The secondary multiplier is shown in blue. When it is greater than `1x`, blue multiplied damage and its skill-based range appear immediately after the yellow base range on each damage line. This preview includes the secondary multiplier, but not other combat modifiers. Multipliers of `1x` or less do not add multiplied values.
- WackyDB synchronizes and exports the optional fields normally. Removing the blocks, or deleting an item's YAML, clears its override on the next full reload.

## Implementation and compatibility

The main mod only stores YAML metadata. This companion tracks cloned attack definitions, redirects weapon damage reads, and scopes overrides around the original `ItemData.GetDamage` pipeline. Normal `GetDamage` patches from other mods can still run.

Patch targets are checked during startup. 

