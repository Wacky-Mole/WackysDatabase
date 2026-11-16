# WackyDB SecondaryAttacks

Optional companion mod for **WackysDatabase 2.6.14+** that lets you give a weapon's secondary attack its own `Damage` and `Damage_Per_Level` in WackyDB item YAML.

WackysDatabase alone doesn't include these combat patches, so if you want separate secondary attack damage, install this alongside it.

## Install

Install on **both clients and server** (requires WackysDatabase 2.6.14+).

## How to Use

Add `Damage` and/or `Damage_Per_Level` inside `Secondary_Attack` in your item YAML:

```yaml
name: SwordIron
Secondary_Attack:
  Damage:
    Slash: 90
    Pierce: 20
  Damage_Per_Level:
    Slash: 6
```

- **Secondary only:** Primary attacks still use the item's normal top-level `Damage` and `Damage_Per_Level`.
- **Overrides per block:** If you add `Damage` or `Damage_Per_Level` under `Secondary_Attack`, it replaces that whole block (any damage type you leave out becomes `0`). Leave a block out entirely and it falls back to the weapon's normal damage.
- **Native scaling still applies:** Quality, world level, skill scaling, `DmgMultiplier`, chain bonuses, and status effects all work as usual.
- **Attack types:** Works for melee, area hits, and projectile bursts. Ammo damage (like arrows) is not replaced.
- **Tooltips:** Adds a **Secondary base damage** section to the item tooltip, including `Chop` and `Pickaxe` if used. If `DmgMultiplier` is above `1x`, the multiplied damage preview and multiplier show in blue (before other combat modifiers).
- **Reloading & sync:** Syncs and exports through WackysDatabase automatically. Remove the blocks (or delete the YAML) and run a reload to clear the override.

## Compatibility

Overrides are only active while calculating secondary attack damage or building the secondary tooltip, so other mods that patch `ItemData.GetDamage` should continue to work normally.

