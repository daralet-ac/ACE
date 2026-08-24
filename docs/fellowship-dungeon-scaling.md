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

## Damage taken: exact, and not Shroud-specific — fixed at the root

`GetMonsterDamageDealtHealthScalar` (despite the name — confirmed via its `DamageEvent.cs:855-856`
call site to be the function actually driving incoming damage, i.e. damage the player *takes*) and
`GetPlayerArmorWardScalar` (`LevelScaling.cs:45`, `:202`) are the standard, unconditional
`PlayerHealth`/`PlayerArmorWard` target/source ratios — no fellowship-specific handling. An earlier
version of this work suppressed both of them for fellowship landblocks to force a Shrouded player
onto their own real health/armor. That's no longer necessary, because the actual root cause turned
out to live one level down, in how monster damage gets authored in the first place — see the next
section. Once that's fixed, these two scalars *combined with the monster's own (now correctly
calibrated) damage* produce an exact result on their own: a Shrouded player's damage taken lands on
precisely the same %HP-per-hit as their real health pool, for every dungeon and every player level.
Verified algebraically and numerically flat at **7.0%** (the fellowship override's target rate)
for every (player level, dungeon) pair tested, 10 through 50.

### Root cause: `GetNewBaseDamage()`'s authoring-stage armor assumption didn't match real combat

`Creature_ArchetypeSystem.GetNewBaseDamage()` authors a monster's raw damage once, at spawn, by
dividing a health-based target by several "assumed defender" factors — including
`avgPlayerArmorReduction[tier]` — so that when *real* combat re-applies real armor mitigation, the
two cancel and the realized damage lands back on the intended rate (`enemyDamage[5]`, forced to the
same value for all 18 dungeons by the pre-existing fellowship override). The bug: this table was
authored independently and never actually matched `100/(100+RealAL)`, the real formula
(`SkillFormula.CalcArmorMod`) that runs during live combat. Confirmed by tracing the full chain —
including the Strength-derived attack-skill floor (`AdjustAttributesForArchetypeConstraints`) and
`_attributeMod`, which also cancels correctly since it's applied generically to any attacker,
player or monster — this mismatch alone was enough to make a **native** level-10 character take
about 1.8× the %HP-per-hit that a native level-50 takes in their own dungeon, even before Shrouding
enters the picture at all. `avgPlayerLifeProtReduction` (the same authoring stage's other
"cancel-later" assumption) was checked too and confirmed *correct* — its curve (0%, 0%, 10%, 10%,
15%, 20%, 20%, 25%, 25% reduction by tier) matches the real Minor/Major/Epic/Legendary cantrip
progression exactly, so it needed no change.

Fix: `avgPlayerArmorReduction` (`Creature_ArchetypeSystem.cs:29`) now reads directly as
`100/(100+RealAL[tier])` against the same real player-armor table used elsewhere this session
(`50, 100, 200, 300, 400, 500, 600, 700, 800`). This is a global fix, not scoped to the 18
dungeons — it corrects native damage-taken uniformity everywhere `UseArchetypeSystem` creatures
spawn, and the fellowship dungeons' Shrouded-visitor uniformity falls out of it for free, combined
with the two (already-unconditional) `LevelScaling` scalars above.

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
| Native (non-Shrouded) play, damage **dealt** | **No** | `CanScalePlayer` requires `player.Level > monster.Level`; a native at a dungeon's own minimum level never triggers the dealt-side `LevelScaling` scalars. |
| Native (non-Shrouded) play, damage **taken** | **Yes, incidentally** | The `avgPlayerArmorReduction` fix isn't `LevelScaling`-gated — it corrects `GetNewBaseDamage()`'s authoring for every `UseArchetypeSystem` creature. Natives now also take a flat 7.0%/hit in their own dungeon at every tier, where before it ranged as high as ~180% of the level-50 rate at level 10. Not the goal of this specific fix, but a direct, welcome side effect of correcting the underlying bug rather than working around it. |

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
