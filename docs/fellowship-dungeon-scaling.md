# Fellowship Dungeon Difficulty Normalization: Design & Technical Reference

## Purpose

The 18 fellowship-required "capstone" dungeons (`Landblock.FellowshipRequiredLandblocksIds`,
`apps/server/Entity/Landblock.cs`) span five minimum levels, three dungeons each at 10/20/30/40/50.
Every visitor should find each dungeon equally hard, whatever level they are, and whichever dungeon it is.

## How it works (Tier Power)

Monster stats and the reference player they're authored against all come from `TierPower`
(`apps/server/Entity/TierPower.cs`), which both `Creature_ArchetypeSystem` and `LevelScaling` read, so the
two always agree.

- **Everyone is Shrouded.** The dungeons require Shrouding (`Portal.RequiresShrouded`), so every visitor is
  level scaled, natives included.
- **Every monster is the dungeon's level.** `LevelScaling` warns once per landblock when a monster's level
  differs from the first one seen there (`CheckFellowshipDungeonLevel`).
- **Scaled to the monster's level, whichever is higher.** Inside a fellowship landblock `CanScalePlayer` scales
  the player to the monster's level even when the player is lower or at the same level. Every ratio works
  either way.
- **Damage taken: the T6 rate.** Monsters there deal `TierPower.FellowshipIncomingPercent` (6% of the
  reference player's health per second, the level 50 target) instead of their own level's
  (`Creature_ArchetypeSystem.GetNewBaseDamage` and the spell damage multiplier). The health ratio in
  `GetMonsterDamageDealtHealthScalar` carries that to the visitor's own health.
- **Damage dealt: the T6 kill time.** `GetMonsterDamageTakenHealthScalar` multiplies a Shrouded player's damage
  by `GetDamageTakenRatio`:

  ```
  MonsterHealth(monster level) / MonsterHealth(player level) x KillTime(player level) / target
  ```

  The health ratio, with the monster's armor/ward scaled to the player's level (`GetMonsterArmorWardScalar`),
  leaves a reference player killing the monster in their own level's kill time. The kill-time ratio moves
  that to the target: 17.14 s (`TierPower.FellowshipKillTime`, the level 50 target) in a fellowship dungeon,
  or the monster level's kill time anywhere else.

Outside fellowship dungeons the same rule means a Shrouded player kills a lower monster in that monster
level's kill time, as a native does (for example a Shrouded level 100 against level 10 monsters: 11.43 s).

## What it replaced

- `GetFellowshipDealtTtkDivisor`, a hand-derived per-dungeon-level divisor fitted to the old TTK. The kill-time
  ratio derives the same thing from the targets.
- The fellowship exception in `GetMonsterArmorWardScalar`. Monster armor/ward is now scaled there like
  anywhere else.
- `enemyDamage[5]` (7%) as the capstone damage setting. It's 6% now, the level 50 target.

## Tests

`apps/server-tests/LevelScalingTests.cs`: `DamageTaken_AShroudedPlayerKillsInTheMonsterLevelsKillTime` checks
the kill time at every pair of levels, with and without the fellowship target.
