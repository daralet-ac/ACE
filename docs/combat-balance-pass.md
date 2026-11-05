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
| Hit chance | Medium height +10% attack (+20% specialized) | accuracy bar +10% and Medium height | nothing (now +10% specialized) |
| Combat skill damage | Two-handed / Dual Wield up to +50% | none | none |
| Crits | 10% for 2x max | 10% for 2x max | 10% for 2x max (harms x1.5) |

## What was out of line

**Before** (T8, single target, misses included):

| Build | Damage | Physical taken | Magic taken | Damage / physical taken |
|---|---:|---:|---:|---:|
| 2H heavy - Warrior | 1.23 | 0.92 | 1.00 | 1.34 |
| 2H heavy - Blademaster | 1.31 | 0.92 | 1.00 | 1.42 |
| 2H heavy - Spellsword | 0.98 | 0.90 | 1.00 | 1.10 |
| S&B heavy - Warrior | 0.83 | 0.81 | 1.00 | 1.02 |
| S&B heavy - Blademaster | 0.89 | 0.81 | 1.00 | 1.09 |
| S&B heavy - Spellsword | 0.66 | 0.79 | 1.00 | 0.84 |
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
3. **Sword and board pays a third of its damage for ~13% less damage taken.** Two-handed and Dual Wield add up to +50%
   damage from their skill; Shield adds none, by design: its strength is damage reduction. But that reduction is small
   next to what it costs: ~14% from the shield and ~8% block, against two-handed's ~10% parry (two-handed parries about as
   often as a shield blocks). Left as a follow-up: see the damage-reduction options below.
4. **Spells were resisted far more often than weapons were evaded.** Weapons get +10-20% attack skill from the Medium
   attack height (missiles another +10% from the accuracy bar); spells get nothing, so at the tier's average skills a war
   mage landed 59% of their damage against a melee's 79% and an archer's 90%.
5. **Heavy leads both defenses.** Heavy armor has the highest armor level *and* the highest ward (7 per slot, against
   cloth's 6 and light's 5), and its defense mod roll grants Physical *and* Magic Defense where every other two-way roll
   picks one. Cloth and light ward and heavy's defense roll are kept by design: heavy is the defensive armor, and pays
   for it with the largest stamina and mana penalty (up to +45%).
6. **Workmanship scored armor ward against a stale table** (cloth 7, amuli 6, light/most heavy 5, covenant 4 per slot), so
   Covenant pieces scored ~170% on ward and cloth ~80%, inflating heavy armor's workmanship and deflating cloth's.

## Changes

### ACE

| Change | Where | Effect |
|---|---|---|
| **Dual Wield spec refill fix**: the power bar pays back the real refill time | `DamageFormulas.GetPowerMod`, `Player.GetPowerMod`, `Player.GetMeleeRefillTimeMod` | DW spec is +25% damage, as described; attacks still come 25% faster |
| **Spell accuracy**: a player's own War and Life spells count +10% magic skill against resists with the school specialized (nothing when only trained) | `MagicFormulas.GetSpellAccuracyMod`, `WorldObject.GetEffectiveMagicSkill` | War magic 0.70 -> 0.80 effective; procs and Enchanted Blade are unchanged |
| **Multishot fallback**: bonus arrows that find no other enemy hit the main target for 25% (was 50%). Bonus arrows at other enemies are unchanged | `DamageFormulas.MultishotFallbackDamageMod`, `DamageEvent.GetCleaveDamageMod` | Archer focus x2.25 -> x1.88 single target; groups unchanged |
| **Workmanship ward score** follows each piece's own base ward | `GetMaxWardLevel` | Same score for the same roll quality in every weight class (clothing unchanged) |
| **Bug: Enrage had no damage-taken drawback** - the stance's +25% damage taken read the meter, which Enrage empties | `GetCombatAbilityFuryDamageBonus` | Enraged players take up to +25% damage, at the level released |
| **Bug: Overload put Backstab on cooldown** instead of Battery when it cancelled Battery (wrong WCID) | `Player.TryUseOverload` | Battery goes on cooldown |
| **Bug: Backstab/Smokescreen errors named Spellsword**; they're Vagabond, Archer and Sorcerer abilities | `Player.VerifyCombatFocus` | Message only |

### world-db

- Multishot's description: fallback arrows deal 25%.
- Fury's description: the stance and Enrage raise damage taken by up to 25%, and Enraged attacks can't be evaded (both
  were already true of the stance and the no-evade, but not described).

Workmanship applies to newly rolled armor (and armor that's restabilized); existing items keep theirs.

## After

| Build | Damage | Physical taken | Magic taken | Damage / physical taken |
|---|---:|---:|---:|---:|
| 2H heavy - Warrior | 1.23 | 1.01 | 1.00 | 1.22 |
| 2H heavy - Blademaster | 1.31 | 1.01 | 1.00 | 1.30 |
| 2H heavy - Spellsword | 0.98 | 0.90 | 1.00 | 1.10 |
| S&B heavy - Warrior | 0.83 | 0.89 | 1.00 | 0.94 |
| S&B heavy - Blademaster | 0.89 | 0.89 | 1.00 | 1.00 |
| S&B heavy - Spellsword | 0.66 | 0.79 | 1.00 | 0.84 |
| Thrown heavy - Warrior | 1.28 | 1.12 | 1.00 | 1.14 |
| Thrown heavy - Archer | 1.70 | 1.12 | 1.00 | 1.51 |
| DW light - Blademaster | 1.63 | 1.18 | 1.22 | 1.38 |
| DW light - Vagabond | 1.30 | 1.05 | 1.22 | 1.24 |
| Bow heavy - Archer | 1.97 | 1.12 | 1.00 | 1.75 |
| Bow heavy - Vagabond | 1.11 | 1.00 | 1.00 | 1.11 |
| War cloth - Sorcerer | 1.09 | 1.25 | 1.10 | 0.87 |
| War cloth - Vagabond | 0.80 | 1.25 | 1.10 | 0.64 |
| Life cloth - Sorcerer (harms) | 0.51 | 1.25 | 1.10 | 0.41 |

The spread of damage per damage taken narrows from 0.36-2.30 to 0.41-1.75, and the two-handed, thrown and dual wield
builds sit in a 1.1-1.4 band. Sword and board (0.84-1.00) and war magic (0.87) stay below it. Ranged builds (bow, thrown, war) also take less melee damage than the table shows, since they can keep
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
| Spellsword | Enchanted Blade | Provoke/Riposte, Battery/Mana Barrier, Reflect/Aegis | x1.08-1.24 (x1.13 typical) |

Fury (Enrage x2 for 10 s on a 20 s cooldown, ~6 s to fill) and Overload (x2 for 10 s, ~4 casts to fill) are the two big
damage engines, and are close: x1.41 and x1.35 averaged. The locked abilities are where the focuses differ most: Multishot
and Overload are big, Weapon Master and Shadow Flurry are worth ~5-12% on a single target, Enchanted Blade 8-24%
depending on spellcraft and Arcane Lore (below), and Phalanx is defensive. Warrior and Spellsword share the defensive
pairs, but Warrior also gets Fury, so Spellsword trades ~20-25% damage for Enchanted Blade, Battery and Mana Barrier.

### Enchanted Blade, spellcraft and Arcane Lore

Enchanted Blade stores a spell and fires it on the next hit (10 s cooldown). Spellcraft counts twice, and specialized
Arcane Lore adds half its skill to the spellcraft both times:

- **Spell level**: a roll between half and all of (War or Life skill + 10% of spellcraft), rounded to the nearest of
  20/150/200/250/300/350/400 for levels I-VII.
- **Damage**: the spell's damage x spellcraft x 1% x (1 + power bar), as for weapon procs. The weapon's own attribute mod
  and the monster's ward apply as to the weapon's hits, so the comparison below is like for like.

On a two-handed Spellsword at T8 (300 War Magic, weapon damage including misses), per 10.5 s:

| Weapon spellcraft | Arcane Lore | Proc (x target DPS) | Damage added |
|---|---|---:|---:|
| 400 (bottom roll) | not specialized | 0.70 | +8% |
| 400 | 300, specialized (+150 spellcraft) | 1.15 | +13% |
| 600 (top roll) | 300, specialized | 1.91 | +21% |
| 600 | 400, specialized (+200) | 2.17 | +24% |

So Arcane Lore specialization is worth about +5 points of Enchanted Blade's bonus, and a top spellcraft roll another +8.
Even at the top it stays below Fury (+41%) and Overload (+35%).

## Skills and specialization bonuses

| Skill | Specialization bonus | Worth (single target) |
|---|---|---|
| Martial Weapons | Axe +5% crit, Mace +65% crit damage, Spear +10% armor penetration, Sword -10% stamina | ~8% (axe, mace, spear) |
| Dagger / Staff | +5% crit / +65% crit damage | ~8% |
| Unarmed | -10% stamina | utility |
| Two-handed Combat | 50% chance of +1 cleave target (sword, mace, axe) | groups only |
| Dual Wield | 25% faster swings and refill | +45% -> **+25%** |
| Shield | blocks from 225 degrees | defensive |
| Bow / Thrown | specialized weapon height bonuses (all weapons get these); Thrown also allows a shield | ~ |
| War Magic | advanced spells, +10% spell accuracy (new), plus scepter +5% crit, wand +50% crit damage or orb +10% ward penetration | ~6-8%, plus ~13% from accuracy |
| Life Magic | advanced spells, +10% spell accuracy (new); the caster subtype bonuses are War only | accuracy only |
| Arcane Lore | half the skill added to item spellcraft: procs and Enchanted Blade | see Enchanted Blade |
| Physical / Magic Defense | 10-20% damage reduction (+ block/parry chance for Physical) | large |

Every weapon specialization also doubles the attack height bonuses (+20% instead of +10%).

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
4. **Spellsword's locked ability is the smallest damage engine.** Enchanted Blade adds 8-24% (see above), against Fury's
   41% and Overload's 35%, so a two-handed Spellsword deals ~20% less than a two-handed Warrior with the same defensive
   pairs. Rolling the spell level from the full skill instead of half to all of it (level V-VII instead of III-VI at
   T8) would add roughly half again.
7. **Sword and board's damage reduction.** At 0.94 damage per damage taken (Warrior), against two-handed's 1.22,
   the shield needs about a third less damage taken to match. Options, modeled for a sword and board Warrior:
   - shield damage reduction up to 40% instead of 25% (`SkillFormula.CalcShieldMod`): 1.04
   - base block chance 10% instead of 5% (`DamageFormulas.GetBlockChance`): 1.03
   - both: 1.14
5. **Light armor has no defensive mod, and the lowest ward.** Its rolls are Attack, Dual Wield, Thievery/Run, Stamina
   Regen and Perception/Deception, so archers wear heavy armor for its Physical/Magic Defense rolls and higher armor and
   ward (light takes 17% more physical and 22% more magic damage than heavy). A
   Physical Defense (evasion) roll on light armor would give the Coordination set a defensive identity and a real choice
   for archers.
6. **Two-handed specialization** only adds cleave, so it does nothing on a single target and nothing for two-handed spears
   (which don't cleave). A small single-target bonus (e.g. +10% critical damage) would put it beside the others.

## Assumptions and limits

- Skills are at the tier's averages (`LevelScaling.AvgPlayerAttackSkillPerTier`), 50 under monster defense. Higher player
  skill shrinks the hit-chance differences (at even skill: melee 95%, missile 98%, magic 75% -> 87%).
- Spell cast times are `WeaponAnimationLength`'s bolt/arc table (2.88 s at level VII); mana costs aren't modeled.
- Shield damage reduction assumes the shield's level is a little above the attacker's (`CalcShieldMod` at ~55% of its 25%).
- Enchanted Blade uses Medium (bolt) attacks; High stores a blast and Low a volley.
- Stamina is modeled only qualitatively. The armor penalty (heavy up to +45% stamina and mana) and the abilities' +25%
  each make heavy-armored ability users the most stamina-hungry.
- Ability averages assume using them on cooldown against one target. Groups favor cleave, Multishot, volleys and rings.
