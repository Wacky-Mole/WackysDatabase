# Changelog

## 1.0.1
 Icon Update

## 1.0.0

- Add independent secondary-attack damage and damage per upgrade level through WackyDB YAML.
- Support melee, area attacks and projectile bursts while preserving ammunition damage.
- Add secondary base damage to item tooltips, including instantiated inventory items resolved through their prefab identity.
- Require WackysDatabase 2.6.14 or newer. Restart the game after updating both DLLs; an already running game keeps its previously loaded code.
- With Slash 90 and Pierce 20 configured, quality 1 shows those secondary base values. A secondary DmgMultiplier of 3 still applies afterward; the main damage section continues to show primary damage.
