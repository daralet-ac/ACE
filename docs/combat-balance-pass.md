# Combat balance pass (2026-11)

A pass over the three attribute gear sets (Strength, Coordination, Self), the six Combat Focuses and their abilities, and
the combat skills, built around the seven builds people play:

| Attribute | Build | Armor | Usual focuses |
|---|---|---|---|
| Strength | Two-handed | Heavy | Warrior, Spellsword, Blademaster |
| Strength | One-handed + shield | Heavy | Warrior, Spellsword, Blademaster |
| Strength | Thrown | Heavy | Warrior, Blademaster, Archer |
| Coordination | Dual wield | Light | Blademaster, Vagabond |
| Coordination | Bow | Heavy | Archer, Vagabond |
| Self | War magic | Cloth | Sorcerer, Vagabond |
| Self | Life magic | Cloth | Sorcerer |

Every number here comes from the server's own formulas, reproduced in [`combat-balance-model.py`](combat-balance-model.py)
(`python3 docs/combat-balance-model.py [--after] [--markdown]`). It models tier 8 at level 100 against a same-level
monster: bottom-roll gear, full power/accuracy bar, the tier's average Quickness (195), player attack and defense skill at
the tier's average (300), and monster defense at that plus the archetype system's 50-point gap. Damage is relative to the
tier's target DPS (`TierPower`), so 1.00 is what a bottom-roll weapon of the tier is sized to deal. Damage taken is
relative to the reference player (bottom-roll Covenant), which monster damage is tuned against.

## How the three weapon families compare

Weapon damage is already normalized across families: melee and thrown weapons roll their Damage so the bottom roll deals
the tier's target DPS at the weapon's own speed; launchers and casters share one multiplier ladder sized so the tier's
ammo or spell level deals it. The differences are everything after that:

| | Melee | Missile (bows, thrown) | Magic |
|---|---|---|---|
| Power/accuracy bar | x0.75 at full, refill time paid back (`GetPowerMod`) | x0.75, paid back against 55% of the attack time, so it's worth more (bow x1.17 of target, crossbow/atlatl/thrown x1.0) | none (x1.0) |
| Hit chance | Medium height +10% attack (+20% specialized) | accuracy bar +10% and Medium height | nothing (fixed: see below) |
| Combat skill damage | Two-handed / Dual Wield up to +50% | none | none |
| Crits | 10% for 2x max | 10% for 2x max | 10% for 2x max (harms x1.5) |

## What was out of line

**Before** (T8, single target, misses included):

| Build | Damage | Physical taken | Magic taken | Damage / physical taken |
|---|---:|---:|---:|---:|
| 2H heavy - Warrior | 1.23 | 0.92 | 1.00 | 1.34 |
| 2H heavy - Blademaster | 1.31 | 0.92 | 1.00 | 1.42 |
| 2H heavy - Spellsword | 0.96 | 0.90 | 1.00 | 1.07 |
| S&B heavy - Warrior | 0.83 | 0.81 | 1.00 | 1.02 |
| S&B heavy - Blademaster | 0.89 | 0.81 | 1.00 | 1.09 |
| S&B heavy - Spellsword | 0.65 | 0.79 | 1.00 | 0.82 |
| Thrown heavy - Warrior | 1.28 | 1.03 | 1.00 | 1.24 |
| Thrown heavy - Archer | 2.04 | 1.03 | 1.00 | 1.98 |
| DW light - Blademaster | 1.89 | 1.08 | 1.22 | 1.75 |
| DW light - Vagabond | 1.51 | 1.05 | 1.22 | 1.43 |
| Bow heavy - Archer | **2.36** | 1.03 | 1.00 | **2.30** |
| Bow heavy - Vagabond | 1.11 | 1.00 | 1.00 | 1.11 |
| War cloth - Sorcerer | 0.95 | 1.25 | 1.10 | 0.76 |
| War cloth - Vagabond | 0.70 | 1.25 | 1.10 | 0.56 |
| Life cloth - Sorcerer (harms) | 0.45 | 1.25 | 1.10 | 0.36 |

1. **Archer with a bow led everything by a wide margin** (2.36x target), at range, in the best armor. Its focus stacks
   Fury, Steady Strike and Multishot, and Multishot's fallback (the two bonus arrows hit the main target for 50% when
   nothing else is near) doubled single-target damage for half of every 20 seconds: x2.25 from the focus, against x1.35-1.50
   for the other damage focuses.
2. **Dual Wield specialization was worth +45%, not +25%.** The spec bonus swings 25% faster and refills the power bar
   25% faster. The power bar's damage pays back the refill as if it took the bar's full level in seconds, so the faster
   refill added ~14% damage for time never spent. Other spec bonuses are worth ~5-10%.
3. **Sword and board had no offensive skill at all.** Two-handed and Dual Wield add up to +50% damage from their skill;
   Shield adds nothing, so a shield cost a third of the damage for ~13% less damage taken (shield reduction ~14% and ~8%
   block, against two-handed's ~10% parry). Two-handed parries about as often as a shield blocks.
4. **Spells were resisted far more often than weapons were evaded.** Weapons get +10-20% attack skill from the Medium
   attack height (missiles another +10% from the accuracy bar); spells get nothing, so at the tier's average skills a war
   mage landed 59% of their damage against a melee's 79% and an archer's 90%.
5. **Cloth was worse than heavy against both weapons and spells.** Heavy armor had the highest armor level *and* the
   highest ward (7 per slot, against cloth's 6 and light's 5), so the Self gear set had no defensive identity: robes took
   25% more physical and 10% more magic damage than plate, for a lower stamina/mana penalty.
6. **Heavy armor's defense roll granted two mods.** Every two-way roll in every weight class picks one (Perception or
   Deception, Shield or Two-handed, Thievery or Run); heavy's Physical Defense + Magic Defense roll gave both at full value.
7. **Workmanship scored armor ward against a stale table** (cloth 7, amuli 6, light/most heavy 5, covenant 4 per slot), so
   Covenant pieces scored ~170% on ward and cloth ~80%, inflating heavy armor's workmanship and deflating cloth's.

## Changes

### ACE

| Change | Where | Effect |
|---|---|---|
| **Dual Wield spec refill fix**: the power bar pays back the real refill time | `DamageFormulas.GetPowerMod`, `Player.GetPowerMod`, `Player.GetMeleeRefillTimeMod` | DW spec is +25% damage, as described; attacks still come 25% faster |
| **Shield combat bonus**: melee attacks with a shield gain up to +25% damage from Shield skill vs physical defense (half the 2H/DW bonus, same curve). Shield must be trained; thrown weapons with a shield don't get it | `DamageFormulas.GetShieldCombatDamageBonus`, `DamageEvent.GetShieldCombatDamageBonus`, `DamageModifiers.ShieldCombat` | Sword and board 0.59 -> 0.73 effective |
| **Spell accuracy**: a player's own War and Life spells count +10% magic skill against resists (+20% with the school specialized), matching the Medium attack height | `MagicFormulas.GetSpellAccuracyMod`, `WorldObject.GetEffectiveMagicSkill` | War magic 0.70 -> 0.93 effective; procs and Enchanted Blade are unchanged |
| **Multishot fallback**: bonus arrows that find no other enemy hit the main target for 25% (was 50%). Bonus arrows at other enemies are unchanged | `DamageFormulas.MultishotFallbackDamageMod`, `DamageEvent.GetCleaveDamageMod` | Archer focus x2.25 -> x1.88 single target; groups unchanged |
| **Ward by weight class**: per-slot base ward Cloth 6 -> **8**, Light 5 -> **6**, Heavy 7 (unchanged, the reference monsters are tuned to). Amuli/Chiran/Olthoi Amuli follow cloth. New loot, quest rolls and retiering use these; the one-time tier rework migration keeps the old ones | `LootTables.GetWeightClassBaseWardLevel`, `GetArmorStyleBaseWardLevel`, `GetArmorBaseLevels` | Cloth takes 8% *less* magic damage than heavy (was 10% more); light 10% more (was 22%) |
| **Heavy armor defense roll**: Physical Defense *or* Magic Defense, not both | `TryMutateArmorSkillMod` | Heavy's mod budget matches light and cloth |
| **Workmanship ward score** follows each piece's own base ward | `GetMaxWardLevel` | Same score for the same roll quality in every weight class (clothing unchanged) |
| **Bug: Enrage had no damage-taken drawback** - the stance's +25% damage taken read the meter, which Enrage empties | `GetCombatAbilityFuryDamageBonus` | Enraged players take up to +25% damage, at the level released |
| **Bug: Overload put Backstab on cooldown** instead of Battery when it cancelled Battery (wrong WCID) | `Player.TryUseOverload` | Battery goes on cooldown |
| **Bug: Backstab/Smokescreen errors named Spellsword**; they're Vagabond, Archer and Sorcerer abilities | `Player.VerifyCombatFocus` | Message only |

### world-db

- Multishot's description: fallback arrows deal 25%.
- Fury's description: the stance and Enrage raise damage taken by up to 25%, and Enraged attacks can't be evaded (both
  were already true of the stance and the no-evade, but not described).

Existing items keep their ward and mods; the armor changes reach players through new loot, quest rolls and retiering.

## After

| Build | Damage | Physical taken | Magic taken | Damage / physical taken |
|---|---:|---:|---:|---:|
| 2H heavy - Warrior | 1.23 | 1.01 | 1.00 | 1.22 |
| 2H heavy - Blademaster | 1.31 | 1.01 | 1.00 | 1.30 |
| 2H heavy - Spellsword | 0.96 | 0.90 | 1.00 | 1.07 |
| S&B heavy - Warrior | 1.03 | 0.89 | 1.00 | 1.16 |
| S&B heavy - Blademaster | 1.10 | 0.89 | 1.00 | 1.23 |
| S&B heavy - Spellsword | 0.80 | 0.79 | 1.00 | 1.02 |
| Thrown heavy - Warrior | 1.28 | 1.12 | 1.00 | 1.14 |
| Thrown heavy - Archer | 1.70 | 1.12 | 1.00 | 1.51 |
| DW light - Blademaster | 1.63 | 1.18 | 1.10 | 1.38 |
| DW light - Vagabond | 1.30 | 1.05 | 1.10 | 1.24 |
| Bow heavy - Archer | 1.97 | 1.12 | 1.00 | 1.75 |
| Bow heavy - Vagabond | 1.11 | 1.00 | 1.00 | 1.11 |
| War cloth - Sorcerer | 1.26 | 1.25 | **0.92** | 1.01 |
| War cloth - Vagabond | 0.93 | 1.25 | 0.92 | 0.75 |
| Life cloth - Sorcerer (harms) | 0.60 | 1.25 | 0.92 | 0.48 |

The spread of damage per damage taken narrows from 0.36-2.30 to 0.48-1.75, and the melee, thrown and war builds now sit
in a 1.0-1.4 band. Ranged builds (bow, thrown, war) also take less melee damage than the table shows, since they can keep
their distance; the table counts every build as taking every hit. Fury users' physical damage taken rises with the Enrage
fix (Warrior, Blademaster, Archer: +12% averaged over a Fury cycle, was +3%).

## Combat focuses and abilities

Each focus has one locked ability and three pairs shared around a ring (each pair goes to the focus it's centered on and
its two neighbours):

| Focus | Locked | Pairs | Single-target damage (avg) |
|---|---|---|---:|
| Warrior | Phalanx (30% less full-hit damage, +25-50% block/parry) | Provoke/Riposte, Fury/Relentless, Reflect/Aegis | x1.41 |
| Blademaster | Weapon Master (weapon-specific: sword +25% speed, bleed, daze...) | Provoke/Riposte, Fury/Relentless, Steady Strike/Evasive | x1.50 |
| Archer | Multishot | Fury/Relentless, Steady Strike/Evasive, Backstab/Smokescreen | x2.25 -> **x1.88** |
| Vagabond | Shadow Flurry (every hit a sneak attack, 10 s) | Steady Strike/Evasive, Backstab/Smokescreen, Battery/Mana Barrier | x1.20 |
| Sorcerer | Overload | Backstab/Smokescreen, Battery/Mana Barrier, Reflect/Aegis | x1.35 |
| Spellsword | Enchanted Blade | Provoke/Riposte, Battery/Mana Barrier, Reflect/Aegis | ~x1.10 |

Fury (Enrage x2 for 10 s on a 20 s cooldown, ~6 s to fill) and Overload (x2 for 10 s, ~4 casts to fill) are the two big
damage engines, and are close: x1.41 and x1.35 averaged. The locked abilities are where the focuses differ most: Multishot
and Overload are big, Weapon Master, Shadow Flurry and Enchanted Blade are worth ~5-12% on a single target, and Phalanx is
defensive. Warrior and Spellsword share the defensive pairs, but Warrior also gets Fury, so Spellsword trades ~30% damage
for Enchanted Blade, Battery and Mana Barrier.

## Skills and specialization bonuses

| Skill | Specialization bonus | Worth (single target) |
|---|---|---|
| Martial Weapons | Axe +5% crit, Mace +65% crit damage, Spear +10% armor penetration, Sword -10% stamina | ~8% (axe, mace, spear) |
| Dagger / Staff | +5% crit / +65% crit damage | ~8% |
| Unarmed | -10% stamina | utility |
| Two-handed Combat | 50% chance of +1 cleave target (sword, mace, axe) | groups only |
| Dual Wield | 25% faster swings and refill | +45% -> **+25%** |
| Shield | blocks from 225 degrees (and now the +25% shield bonus grows with skill) | defensive |
| Bow / Thrown | specialized weapon height bonuses (all weapons get these); Thrown also allows a shield | ~ |
| War Magic | advanced spells, plus scepter +5% crit, wand +50% crit damage or orb +10% ward penetration | ~6-8% |
| Life Magic | advanced spells (the caster subtype bonuses are War only) | ~0 damage |
| Physical / Magic Defense | 10-20% damage reduction (+ block/parry chance for Physical) | large |

Every weapon specialization also doubles the attack height bonuses (+20% instead of +10%), and specialized magic schools
now get the matching +20% spell accuracy.

## Recommended follow-ups (not changed here)

These need design calls or data changes beyond this pass:

1. **Archer is still the top single-target damage** (1.97x, 1.75 per damage taken). If that's more than intended,
   Multishot's fallback can go to 12.5% (archer focus x1.72, bow archer ~1.80) or be dropped entirely, making Multishot a
   group ability like cleave (x1.50, the same as Blademaster).
2. **Crossbows and atlatls deal ~13-15% less than bows.** All launchers share one damage-mod ladder, so the slower
   launchers fall behind on ammo damage per second (bow 1.17, crossbow 1.01, atlatl 0.99). Raising quarrel and dart damage
   ~15% (`TierPower` ammo tables), or giving their launchers a matching damage-mod factor, would level them.
3. **Life magic's damage is half of War's.** Harm Other is half a bolt at every level (crits x1.5 instead of x2), and
   Martyr's Hecatomb reaches bolt damage only by spending the caster's health. That suits a healer, but a solo Life mage
   kills at ~half the speed of everyone else. Options: raise Harm Other to ~75% of a bolt, or let Life specialization use
   the caster subtype bonuses War gets (`CheckForWarSpecCriticalChanceBonus` and friends check `WarMagic` only).
4. **Spellsword's locked ability is weak for its damage.** Enchanted Blade picks a spell level from half to all of the
   magic skill (often level III-V at tier 8) and adds ~5-10%. Scaling it like a proc (the weapon's spellcraft already
   multiplies it) at a higher spell level would give Spellsword a damage engine to stand next to Fury and Overload.
5. **Light armor has no defensive mod.** Its rolls are Attack, Dual Wield, Thievery/Run, Stamina Regen and
   Perception/Deception, so archers wear heavy armor for its Physical/Magic Defense rolls and higher armor and ward. A
   Physical Defense (evasion) roll on light armor would give the Coordination set a defensive identity and a real choice
   for archers.
6. **Two-handed specialization** only adds cleave, so it does nothing on a single target and nothing for two-handed spears
   (which don't cleave). A small single-target bonus (e.g. +10% critical damage) would put it beside the others.

## Assumptions and limits

- Skills are at the tier's averages (`LevelScaling.AvgPlayerAttackSkillPerTier`), 50 under monster defense. Higher player
  skill shrinks the hit-chance differences (at even skill: melee 95%, missile 98%, magic 75% -> 95%).
- Spell cast times are `WeaponAnimationLength`'s bolt/arc table (2.88 s at level VII); mana costs aren't modeled.
- Stamina is modeled only qualitatively. The armor penalty (heavy up to +45% stamina and mana) and the abilities' +25%
  each make heavy-armored ability users the most stamina-hungry.
- Ability averages assume using them on cooldown against one target. Groups favor cleave, Multishot, volleys and rings.
