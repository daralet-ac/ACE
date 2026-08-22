# Fellowship Dungeon Difficulty Normalization — Design & Technical Reference

## Purpose

The 18 fellowship-required "capstone" dungeons (`Landblock.FellowshipRequiredLandblocksIds`,
`apps/server/Entity/Landblock.cs:2401`) span five minimum levels — three dungeons each at
10/20/30/40/50. Players reported wildly inconsistent difficulty when using a Shard of Shrouding
to run a dungeon below their real level: the same shrouded character could feel dramatically
under- or over-powered depending on which dungeon and which side of the fight (damage dealt vs.
damage taken) you looked at.

## Background: what `LevelScaling.cs`'s scalars do by default

`apps/server/Entity/LevelScaling.cs` scales a Shrouded player's stats (and the monster's, from
that player's perspective) whenever `player.Level > monster.Level` and the player has the
Shrouded enchantment (`CanScalePlayer`, `LevelScaling.cs:517`). Its scalars are all
"target/source" ratios pulled from tier tables at the two creatures' levels — e.g. the health
scalar returns `MonsterHealth(player.Level) / MonsterHealth(monster.Level)`.

Composing the health-ratio scalar with the armor-up scalar (as originally built) algebraically
cancels every term tied to the monster's own level, leaving the scaled fight's damage-dealt
efficiency pinned to **the player's own native tier**, not the dungeon's. A shrouded level 50 got
an identical damage-dealt efficiency in every one of the 18 dungeons — uniform across dungeons for
a fixed player level — but within any single dungeon, a native and a shrouded visitor got very
different efficiencies. The damage-taken direction had the mirror problem.

Two goals were wanted:

1. Every dungeon should be equally difficult for a given (high) player level.
2. Within a dungeon, that difficulty shouldn't swing wildly depending on which qualifying level is
   visiting.

An earlier version of this work pinned monster Health/Armor/Ward to a single fixed tier for every
visitor including natives, to chase both goals exactly. That was reverted: it meant every native
player's own home dungeon got measurably tankier (a level-10 dungeon monster needed ~90% more
health), which is a much bigger, more visible change than the actual complaint — shrouded play —
called for. The design below is scoped to **Shrouded visitors only**. Natives playing their own
dungeon at their own level are completely unaffected; `CanScalePlayer`'s `player.Level >
monster.Level` requirement already guarantees this without any extra guard.

## Damage taken: flattened across dungeons, for a fixed Shrouded level

`GetMonsterDamageDealtHealthScalar` (despite the name — confirmed via its `DamageEvent.cs:855-856`
call site to be the function actually driving incoming damage, i.e. damage the player *takes*) and
`GetPlayerArmorWardScalar` (`LevelScaling.cs:45`, `:169`) both return `1.0f` immediately for
fellowship-required landblocks, before their normal `PlayerHealth`/`PlayerArmorWard` tier-ratio
calculation. With both suppressed, a Shrouded player takes damage using their own real health and
armor against the monster's own real (unscaled) outgoing damage — identical, in relative terms,
in all 18 dungeons. Verified: before this guard, a shrouded level 50 took 7.09× more damage per
hit in the level-10 dungeon than in the level-50 one; after, it's 1.00× everywhere.

## Damage dealt: brought close to native level-50 pace, not pinned exactly

This direction keeps both existing scalars *active* rather than suppressing them, plus one
addition:

- `GetMonsterArmorWardScalar` (`LevelScaling.cs:164`) still returns `1.0f` for fellowship
  landblocks — no armor-up scaling at all. Real weapon-DPS growth and real Archetype
  monster-health growth already track each other reasonably well tier-over-tier (the "surplus"
  property below), so the armor-up scalar was mostly overcorrection, not a necessary compensator.
- `GetMonsterDamageTakenHealthScalar` (misleadingly named the mirror of the above — confirmed via
  the same call-site trace to be the function driving damage the player *deals*,
  `LevelScaling.cs:89`) keeps its normal `MonsterHealth(monster.Level) / MonsterHealth(player.Level)`
  calculation, then for fellowship landblocks additionally divides by a new per-dungeon-tier
  constant from `GetFellowshipDealtTtkDivisor()` (`LevelScaling.cs:145`).

The divisor per tier is the ratio of native level-50 TTK to that tier's own native TTK, where
native TTK = `ArchetypeHealth / (real weapon DPS × real attribute mod × armor mitigation)`, built
from `LootGenerationFactory_Weapon.GetWeaponBaseDps`, the real Str-by-tier progression, and
`Creature_ArchetypeSystem`'s `enemyHealth`/`enemyArmorWard` tables:

| Dungeon min level | Native TTK (unitless) | Divisor |
|---|---|---|
| 10 | 6.78 | 1.906 |
| 20 | 8.33 | 1.551 |
| 30 | 8.83 | 1.464 |
| 40 | 11.03 | 1.171 |
| 50 | 12.92 | 1.000 (no-op) |

These are hand-computed constants, not derived at runtime — recompute them if the reference level
(currently native level 50) changes, or if `GetWeaponBaseDps`/the Archetype health or armor tables
change. `LevelScaling.cs` already has an `AvgTimeToKillMonster` table that looks like it might have
been intended for exactly this kind of reference curve, but it's dead code (`GetTtkMonsterAtLevel`
is defined and never called) and its provenance couldn't be confirmed, so it wasn't used.

### Why this isn't exact, and how far off it gets

Unlike the taken-side fix, this one is an approximation. The divisor is solved so a *native*
player's own TTK against each tier matches the level-50 target exactly, by construction — but a
*Shrouded* visitor's existing health-ratio scalar wasn't derived with that same target in mind, so
the two don't cancel perfectly. Verified against real (player level, dungeon) pairs:

| Dungeon | Shrouded 20 | Shrouded 30 | Shrouded 40 | Shrouded 50 | Shrouded 75 | Shrouded 100 |
|---|---|---|---|---|---|---|
| 10 | 1.02× | 0.93× | 0.97× | 0.91× | 0.75× | 0.72× |
| 20 | — | 0.91× | 0.96× | 0.89× | 0.74× | 0.71× |
| 30 | — | — | 1.04× | 0.98× | 0.81× | 0.77× |
| 40 | — | — | — | 0.93× | 0.77× | 0.74× |
| 50 | — | — | — | 1.00× | 0.83× | 0.79× |

For realistic shrouding (levels 20–50 into these dungeons), everything lands within about ±10% of
the level-50 target. It drifts further for much higher shrouded levels — a level 100 in the
level-10 dungeon lands at 0.72×, meaningfully faster than target but nowhere near the original
~8× distortion. This is a deliberate trade: a small, self-contained scalar change that gets close
for realistic play, instead of a larger mechanism (touching monster spawn stats, or normalizing
every visiting player's power individually) that could close the gap exactly.

## Explicitly out of scope

| Stat | Affected? | Why |
|---|---|---|
| Monster Health/Armor/Ward (the actual spawned stats) | **No** | Everything here is a `LevelScaling` scalar applied per-fight, not a change to what a creature spawns with. Natives and the monster's baseline are untouched. |
| Attack skill / Defense skill | **No** | Left on whatever basis they already use (native per-dungeon tier). Scaling a Shrouded player's effective attack/defense skill was never part of this specific fix. |
| Outgoing monster damage | **No** | Stays on the pre-existing, separate fellowship override in `GetNewBaseDamage()` / `GetArchetypeSpellDamageMultiplier()` (`Creature_ArchetypeSystem.cs:1163`, `:1257`), which forces `enemyDamage[5]` for these landblocks regardless of dungeon tier. Unrelated to and unchanged by this work. |
| Native (non-Shrouded) play | **No** | `CanScalePlayer` requires `player.Level > monster.Level`; a native at a dungeon's own minimum level never triggers any of `LevelScaling`'s scalars, before or after this change. |

## Future work

- If the "close but not exact" gap at high shrouded levels (75+) turns out to matter in practice,
  closing it fully would need a materially different, bigger mechanism — most likely normalizing
  every visitor's effective damage output to the reference tier directly, which was considered and
  set aside in favor of this simpler, scalar-only approach.
- If the target reference level changes (e.g. the level cap eventually rising past 50),
  `GetFellowshipDealtTtkDivisor()`'s five constants need recomputing by hand against the new
  target — there's no dynamic dependency to update automatically.
- Runtime verification (spawn-testing actual creatures in a dev session, confirming both scalars
  fire correctly in the fellowship dungeons and nowhere else) is still outstanding — everything
  above has been verified by build and by offline computation, not by running the server.
