# Combat balance pass (2026-11)

A pass over the three attribute gear sets (Strength, Coordination, Self), the six Combat Focuses and all their abilities,
and the combat skills, built around the seven builds people play:

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
the tier's average (300, Physical Defense specialized), and monster attack and defense at that plus the archetype
system's 50-point gap. Every Combat Focus ability is simulated over time, used on cooldown against one target, so
overlapping windows, auto-hits and attack-skill bonuses all count. Damage is relative to the tier's target DPS
(`TierPower`), so 1.00 is what a bottom-roll weapon of the tier is sized to deal. Damage taken is relative to the
reference player (bottom-roll Covenant, no abilities), which monster damage is tuned against.

## How the three weapon families compare

Weapon damage is already normalized across families: melee and thrown weapons roll their Damage so the bottom roll deals
the tier's target DPS at the weapon's own speed; launchers and casters share one multiplier ladder sized so the tier's
ammo or spell level deals it. The differences are everything after that:

| | Melee | Missile (bows, thrown) | Magic |
|---|---|---|---|
| Power/accuracy bar | x0.75 at full, refill time paid back (`GetPowerMod`) | x0.75, paid back against part of the attack time, so it's worth more: 55% before (bow x1.17 of target, crossbow/atlatl/thrown x1.0), 65% now (bow x1.05, the others x0.91-0.93) | none (x1.0) |
| Hit chance | Medium height +10% attack (+20% specialized) | Medium height; the accuracy bar's +10% is removed | nothing before; now +10% specialized |
| Combat skill damage | Two-handed / Dual Wield up to +50% | none | none |
| Crits | 10% for 2x max | 10% for 2x max | 10% for 2x max (harms x1.5) |

**After** (weapons alone, no abilities):

| Weapon | Raw | Lands | Damage |
|---|---:|---:|---:|
| Two-handed sword | 1.11 | 0.79 | 0.87 |
| Sword and board | 0.75 | 0.79 | 0.59 |
| Dual wield daggers, spec | 1.38 | 0.79 | 1.09 |
| Thrown | 0.92 | 0.79 | 0.72 |
| Bow | 1.05 | 0.79 | 0.82 |
| Crossbow | 0.93 | 0.79 | 0.73 |
| Atlatl | 0.91 | 0.79 | 0.71 |
| War magic (bolts) | 1.18 | 0.68 | 0.80 |
| Life magic (harms) | 0.56 | 0.68 | 0.38 |

## Combat Focus abilities

Each focus has one locked ability and three pairs shared around a ring (each pair goes to the focus it's centered on and
its two neighbours):

| Focus | Locked | Pairs |
|---|---|---|
| Warrior | Phalanx | Provoke/Riposte, Fury/Relentless, Reflect/Aegis |
| Blademaster | Weapon Master | Provoke/Riposte, Fury/Relentless, Steady Strike/Evasive Stance |
| Archer | Multishot | Fury/Relentless, Steady Strike/Evasive Stance, Backstab/Smokescreen |
| Vagabond | Shadow Flurry | Steady Strike/Evasive Stance, Backstab/Smokescreen, Battery/Mana Barrier |
| Sorcerer | Overload | Backstab/Smokescreen, Battery/Mana Barrier, Reflect/Aegis |
| Spellsword | Enchanted Blade | Provoke/Riposte, Battery/Mana Barrier, Reflect/Aegis |

Each ability alone, used on cooldown (after this pass):

| Ability | How it works | Damage | Physical taken | Magic taken | Cost |
|---|---|---:|---:|---:|---|
| Fury (2H) | stance builds to +25% in ~6 s; Enrage +100% for 10 s, can't be evaded; 20 s cooldown; up to +25% damage taken | +62% | +12% | | +25% stamina in Enrage, self-damage |
| Fury (bow) | same | +62% | +12% | | |
| Overload (war) | 4 casts to +20%; discharge +100% and 2x magic skill for 10 s; 20 s cooldown | +64% | | | up to 2x mana while charging |
| Steady Strike (2H) | 10 s of +25% damage and +25% attack skill at 0.9x swing speed; 20 s cooldown | +20% | | | +25% stamina |
| Steady Strike (bow) | same (missiles keep their speed) | +28% | | | +25% stamina |
| Multishot (bow) | 10 s of two bonus arrows; on a lone target 25% each (was 50%) | +25% | | | +25% stamina |
| Weapon Master: axe (S&B) | next hit bleeds: 4 ticks of 100 x power x weapon Damage / 132 | +27% | | | |
| Weapon Master: greataxe (2H) | same | +23% | | | |
| Weapon Master: dagger (DW) | bleed, Damage / 95 per hit | +14% | | | |
| Weapon Master: sword (2H) | 10 s of 25% faster swings | +12% | | | |
| Weapon Master: unarmed (DW) | kick puts the target off balance (-100 defense) | +12% | | | |
| Weapon Master: mace (S&B) | daze (-100 attack skill) | | -18% | | |
| Weapon Master: spear | 10 s of puncturing (a second target) | groups only | | | |
| Riposte (2H) | 10 s of 2x block/parry chance; each block or parry counters for a full hit at x1.0 power | +10% | -5% | | +25% stamina |
| Enchanted Blade (2H) | stored spell on the next hit (see below) | +8 to +24% | | | mana |
| Shadow Flurry (DW) | 10 s of every hit a sneak attack (+25%) | +13% | | | +25% stamina |
| Backstab | next sneak attack x2 (x2-3 and unavoidable from stealth) | ~+5% (Vagabond) | | | +25% stamina |
| Phalanx | toggle, shield or two-handed: full hits (weapons and spells) -30%, block/parry +25-50%, all around | | -31% | -29% | +25% stamina |
| Aegis | 10 s: weapon damage -50%, can't evade; 10% back as stamina and mana | | -22% | | mana = level |
| Reflect | 10 s: +50% magic defense, and every resisted spell (full or partial) is reflected, not taken | | | -30% | |
| Provoke | 10 s: damage taken -15%, double threat, taunt | | -7% | | +25% stamina |
| Evasive Stance | toggle: 25% of attacks and spells fully evaded first | | -25% | -25% | +25-50% stamina |
| Mana Barrier | toggle: 25% of health damage taken as 1.5-2x mana | | -25% | -25% | mana |
| Smokescreen | 10 s: +10% of the chance to be hit becomes evade; threat drop, vanish | | -2% | | +25% stamina |
| Relentless, Battery | stamina / mana cost reduction; -10% damage while charging | sustain | | | |

Fury and Overload are the two damage engines and are close (+62%, +64%); both are worth far more than a flat +100% for
10 s suggests, because Enrage can't be evaded and Overload's discharge doubles magic skill. Steady Strike's attack skill
is worth as much as its damage. Weapon Master depends on the weapon: the bleed (axe, dagger and their thrown versions) is
strong, because it scales with the weapon's Damage over its type's top damage from before the tier rework, which tier 8
weapons are ~5x; sword and unarmed are about half of it; mace and staff are defensive; spear only helps against groups.

### Enchanted Blade, spellcraft and Arcane Lore

Enchanted Blade stores a spell and fires it on the next hit (10 s cooldown). Spellcraft counts twice, and specialized
Arcane Lore adds half its skill to the spellcraft both times:

- **Spell level**: a roll between half and all of (War or Life skill + 10% of spellcraft), rounded to the nearest of
  20/150/200/250/300/350/400 for levels I-VII.
- **Damage**: the spell's damage x spellcraft x 1% x (1 + power bar), as for weapon procs. The weapon's own attribute mod
  and the monster's ward apply as to the weapon's hits, so the comparison below is like for like.

On a two-handed Spellsword at T8 (300 War Magic, weapon damage including misses), per 10.5 s:

| Weapon spellcraft | Arcane Lore | Damage added |
|---|---|---:|
| 400 (bottom roll) | not specialized | +8% |
| 400 | 300, specialized (+150 spellcraft) | +13% |
| 600 (top roll) | 300, specialized | +21% |
| 600 | 400, specialized (+200) | +24% |

## The builds, with their focus kits

**Before:**

| Build | Damage | Physical taken | Magic taken | Damage / physical taken |
|---|---:|---:|---:|---:|
| 2H heavy - Warrior | 1.53 | 0.44 | 0.50 | 3.45 |
| 2H heavy - Blademaster (greataxe) | 1.87 | 0.61 | 0.75 | 3.07 |
| 2H heavy - Spellsword | 1.08 | 0.48 | 0.53 | 2.26 |
| S&B heavy - Warrior | 1.01 | 0.39 | 0.50 | 2.60 |
| S&B heavy - Blademaster (axe) | 1.26 | 0.54 | 0.75 | 2.32 |
| S&B heavy - Spellsword | 0.74 | 0.42 | 0.53 | 1.74 |
| Thrown heavy - Warrior | 1.36 | 0.75 | 0.70 | 1.80 |
| Thrown heavy - Blademaster | 1.57 | 0.71 | 0.75 | 2.19 |
| Thrown heavy - Archer | 2.45 | 0.75 | 0.75 | 3.25 |
| DW light - Blademaster (daggers) | 2.49 | 0.71 | 0.92 | 3.48 |
| DW light - Vagabond | 1.79 | 0.58 | 0.69 | 3.10 |
| Bow heavy - Archer | **2.84** | 0.75 | 0.75 | **3.76** |
| Bow heavy - Vagabond | 1.25 | 0.55 | 0.56 | 2.27 |
| War cloth - Sorcerer | 1.25 | 0.72 | 0.58 | 1.73 |
| War cloth - Vagabond | 0.70 | 0.68 | 0.62 | 1.02 |
| Life cloth - Sorcerer (harms) | 0.59 | 0.72 | 0.58 | 0.82 |

**After:**

| Build | Damage | Physical taken | Magic taken | Damage / physical taken |
|---|---:|---:|---:|---:|
| 2H heavy - Warrior | 1.53 | 0.49 | 0.50 | 3.16 |
| 2H heavy - Blademaster (greataxe) | 1.87 | 0.66 | 0.75 | 2.81 |
| 2H heavy - Spellsword | 1.08 | 0.48 | 0.53 | 2.26 |
| S&B heavy - Warrior | 1.01 | 0.43 | 0.50 | 2.38 |
| S&B heavy - Blademaster (axe) | 1.26 | 0.59 | 0.75 | 2.12 |
| S&B heavy - Spellsword | 0.74 | 0.42 | 0.53 | 1.74 |
| Thrown heavy - Warrior | 1.17 | 0.83 | 0.70 | 1.42 |
| Thrown heavy - Blademaster | 1.39 | 0.78 | 0.75 | 1.78 |
| Thrown heavy - Archer | 1.79 | 0.82 | 0.75 | 2.17 |
| DW light - Blademaster (daggers) | 2.17 | 0.78 | 0.92 | 2.79 |
| DW light - Vagabond | 1.55 | 0.58 | 0.69 | 2.68 |
| Bow heavy - Archer | 2.04 | 0.82 | 0.75 | 2.47 |
| Bow heavy - Vagabond | 1.05 | 0.55 | 0.56 | 1.91 |
| War cloth - Sorcerer | 1.33 | 0.72 | 0.58 | 1.83 |
| War cloth - Vagabond | 0.80 | 0.68 | 0.62 | 1.17 |
| Life cloth - Sorcerer (harms) | 0.63 | 0.72 | 0.58 | 0.86 |

What the full kits show:

- **Warrior is the best-protected focus by far** (half the damage of a build with no abilities): Phalanx, Aegis,
  Reflect, Provoke and Riposte together, and it also has Fury. Sword and board Warrior takes the least physical damage of
  any build (0.43).
- **Dual-wield Blademaster and Archer lead on damage** (2.17, 2.04). Archer stacks the most damage abilities (Fury,
  Steady Strike, Multishot) on the fastest-paying weapon, at range, in heavy armor.
- **Spellsword is the weakest damage focus** (1.08 with a two-handed weapon), with Warrior-like defenses (0.48 / 0.53)
  but no Fury. Enchanted Blade is its only damage ability.
- **Sorcerer's Overload keeps up with Fury**, but cloth and no Phalanx/Evasive leave it taking 0.72 physical.
- **Vagabond is a defensive focus**: Evasive Stance, Mana Barrier and Smokescreen take its damage taken down to Warrior
  territory (0.58), at the cost of stamina and mana, with modest damage (Shadow Flurry, Steady Strike, Backstab).
- **Thrown** is the least protected Strength build (no parry, block or Phalanx without a shield).
- **Life magic** stays at about half of War's damage.

## Changes

### ACE

| Change | Where | Effect |
|---|---|---|
| **Dual Wield spec refill fix**: the power bar pays back the real refill time | `DamageFormulas.GetPowerMod`, `Player.GetPowerMod`, `Player.GetMeleeRefillTimeMod` | DW spec is +25% damage, as described (was ~+45%); attacks still come 25% faster |
| **Missile power payback**: the accuracy bar's damage pays back its refill against 65% of the attack time (was 55%, shared with stamina). Missile stamina keeps the 55% it was tuned on | `Player.MissilePowerAttackLengthScale`, `MissileStaminaAttackLengthScale` | A full accuracy bar is worth +28% over a full power bar for a bow (was +43%), +21-23% for crossbows, atlatls and thrown weapons (was +32-34%): bows -10%, the others -8 to -9% |
| **Accuracy bar attack bonus removed**: the bar no longer adds +10% attack skill to missile attacks | `Player.GetEffectiveAttackSkill` | Missile weapons land as often as other weapons (79% instead of 90% at the tier's average skills) |
| **Spell accuracy**: a player's own War and Life spells count +10% magic skill against resists with the school specialized (nothing when only trained) | `MagicFormulas.GetSpellAccuracyMod`, `WorldObject.GetEffectiveMagicSkill` | War magic lands 68% instead of 59%; procs and Enchanted Blade are unchanged |
| **Multishot fallback**: bonus arrows that find no other enemy hit the main target for 25% (was 50%). Bonus arrows at other enemies are unchanged | `DamageFormulas.MultishotFallbackDamageMod`, `DamageEvent.GetCleaveDamageMod` | Multishot +50% -> +25% on a lone target; groups unchanged |
| **Workmanship ward score** follows each piece's own base ward | `GetMaxWardLevel` | Covenant scored ~170% on ward and cloth ~80% against a stale table; now the same roll quality scores the same in every weight class (clothing unchanged) |
| **Bug: Enrage had no damage-taken drawback** - the stance's +25% damage taken read the meter, which Enrage empties | `GetCombatAbilityFuryDamageBonus` | Enraged players take up to +25% damage, at the level released (Fury +3% -> +12% damage taken, averaged) |
| **Bug: Overload put Backstab on cooldown** instead of Battery when it cancelled Battery (wrong WCID) | `Player.TryUseOverload` | Battery goes on cooldown |
| **Bug: Backstab/Smokescreen errors named Spellsword**; they're Vagabond, Archer and Sorcerer abilities | `Player.VerifyCombatFocus` | Message only |

Kept as they are by design: cloth, light and heavy ward (6, 5, 7 per slot), heavy armor's defense roll granting both
Physical and Magic Defense, and no damage bonus for shields (a shield's strength is damage reduction).

### world-db

- Multishot's description: fallback arrows deal 25%.
- Fury's description: the stance and Enrage raise damage taken by up to 25%, and Enraged attacks can't be evaded.

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
| War Magic | advanced spells, +10% spell accuracy (new), plus scepter +5% crit, wand +50% crit damage or orb +10% ward penetration | ~6-8%, plus ~15% from accuracy |
| Life Magic | advanced spells, +10% spell accuracy (new); the caster subtype bonuses are War only | accuracy only |
| Arcane Lore | half the skill added to item spellcraft: procs and Enchanted Blade | see Enchanted Blade |
| Physical / Magic Defense | 10-20% damage reduction (+ block/parry chance for Physical) | large |

Every weapon specialization also doubles the attack height bonuses (+20% instead of +10%).

## Recommended follow-ups (not changed here)

1. **Archer still stacks the most damage abilities** (2.04 with a bow, 1.79 thrown; dual-wield Blademaster 2.17).
   Multishot's fallback at 12.5% would bring a bow Archer to 1.81, or at 0 (a group-only ability, like cleave) to 1.58,
   between two-handed Warrior and Blademaster.
2. **Spellsword's damage.** Enchanted Blade (+8-24%) is its only damage ability, against Fury (+62%) for Warrior with the
   same defensive pairs. Rolling Enchanted Blade's spell level from the full skill instead of half to all of it (level
   V-VII instead of III-VI at T8) would add roughly half again.
3. **Weapon Master by weapon.** Axe and dagger bleeds are worth twice sword and unarmed's bonus, and spear's is only for
   groups. Either the bleed comes down (3 ticks instead of 4: axe +27% -> +20%), or sword/unarmed/spear come up.
4. **Crossbows and atlatls deal ~12-14% less than bows.** All launchers share one damage-mod ladder, so the slower
   launchers fall behind on ammo damage per second, and the power payback still favors the fastest weapons. Raising quarrel and dart damage ~15% (`TierPower` ammo tables), or
   giving their launchers a matching damage-mod factor, would level them.
5. **Life magic's damage is half of War's.** Harm Other is half a bolt at every level (crits x1.5 instead of x2), and
   Martyr's Hecatomb reaches bolt damage only by spending the caster's health. Options: raise Harm Other to ~75% of a
   bolt, or let Life specialization use the caster subtype bonuses War gets.
6. **Light armor has no defensive mod, and the lowest ward**, so archers wear heavy armor for its Physical/Magic Defense
   rolls and higher armor and ward (light takes 17% more physical and 22% more magic damage than heavy).
7. **Two-handed specialization** only adds cleave, so it does nothing on a single target and nothing for two-handed spears
   (which don't cleave).
8. **Sword and board's damage reduction.** With the full Warrior kit, sword and board takes 12% less physical damage than
   two-handed but deals a third less. If that trade should be closer, the shield's damage reduction (up to 25%,
   `SkillFormula.CalcShieldMod`) or base block chance (5%, `DamageFormulas.GetBlockChance`) are the levers.

## Where the missile attack length scale came from

It's Daralet's, not upstream ACE's. Upstream ACE's power and accuracy bars don't depend on the attack's length at all.
The history (`git log -S`):

1. **Melee/missile damage rework (#61, Jan 2024)** added the payback: the power and accuracy bars' damage pays back
   the refill against the attack's length. Missiles used their full attack time (launch, reload and link).
2. **#598 (Dec 2024)**, a missile stamina fix, set the missile length to the link animation alone.
3. **#934 (Nov 2025)**, the missile stamina formula update, set it to (link + launch + reload) / animation speed. The
   launch and reload times were already at that speed, so dividing again took about half the attack time.
4. **The attack speed rework (#1288, Oct 2026)** kept that value as a flat x0.55, noting stamina and the accuracy bar's
   damage were tuned against about half the attack.

So the scale was set for missile stamina, and the damage payback inherited it. Paying back a full bar against about half
the attack is what made a full accuracy bar worth so much more than a full power bar. This pass splits the two: stamina
keeps 0.55, the damage payback uses 0.65.

## Assumptions and limits

- Skills are at the tier's averages (`LevelScaling.AvgPlayerAttackSkillPerTier`), 50 under monster defense. Higher player
  skill shrinks the hit-chance differences, and with them what auto-hits (Enrage) and attack skill (Steady Strike,
  Overload's discharge) are worth.
- Abilities on the same 20 s cooldown are used together; Fury (26 s cycle) and Overload (31.5 s) drift against them.
  Toggles (Phalanx, Evasive Stance, Mana Barrier) are always on: their stamina and mana costs aren't modeled.
- Monsters attack ~0.8 times a second (for Riposte's counters). Weapon Master's Daze and Off Balance last 10 s (their
  duration is in the dat). The bleed ticks 4 times.
- Spell cast times are `WeaponAnimationLength`'s bolt/arc table (2.88 s at level VII); mana costs aren't modeled.
- Shield damage reduction assumes the shield's level is a little above the attacker's (`CalcShieldMod` at ~55% of its
  25%). Physical Defense is specialized (its +up to 50% block/parry chance).
- Enchanted Blade uses Medium (bolt) attacks; High stores a blast and Low a volley.
- Groups favor cleave, Multishot, spear Weapon Master, volleys and rings; this is single target only.
