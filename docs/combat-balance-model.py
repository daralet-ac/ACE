#!/usr/bin/env python3
"""
Combat balance model for the Daralet ACE fork.

Mirrors the server's damage formulas closely enough to compare builds against each other. Every number is relative to
the tier's target DPS (TierPower.WeaponTargetDpsTable), so 1.00 = "a bottom-roll weapon of the tier deals exactly the
target", and the reference player (bottom-roll Covenant, TierPower) takes 1.00 damage.

Run:  python3 docs/combat-balance-model.py            (before the 2026-11 balance pass)
      python3 docs/combat-balance-model.py --after    (with it; add --markdown for tables)

Sources (apps/server):
  melee power/refill .......... Player_Combat.GetPowerMod, Player_Melee (swing + refill timing)
  missile power/timing ........ Player_Missile.LaunchMissile (missileAttackLengthScale), Player_Combat.GetPowerMod
  weapon damage rolls ......... LootGenerationFactory_Melee.GetMeleeDamageRollRange, _Missile, _Caster, TierPower
  2H / DW / shield bonus ...... DamageFormulas.GetCombatSkillDamageBonus
  evade / resist .............. SkillCheck.GetSkillChance, DamageFormulas.GetEvasionType, MagicDefenseCheck
  monster skills .............. Creature_ArchetypeSystem (avg player skill + EnemySkillGap 50)
  armor / ward ................ SkillFormula.CalcArmorMod/CalcWardMod, LootGenerationFactory_Clothing.GetArmorBaseLevels
  abilities ................... Player_Ability, DamageEvent_Damage, Player_Missile.LaunchBonusProjectiles
"""
import math
import sys

AFTER = "--after" in sys.argv

# ---------------------------------------------------------------- tier 8 (level 100) reference
TIER = 8
D = 640.0                     # TierPower.WeaponTargetDpsTable[7]
QUICKNESS = 195               # LootGenerationFactory_Weapon.AvgQuicknessPerTier[7]
MONSTER_DEF = 300 + 50        # avgPlayerPhysicalMagicDefense[7] + EnemySkillGap
MULT_MIN = 9.6                # TierPower.MultiplierMinimum[7] (casters and launchers)
BOTTOM_COVENANT_AL = 100 * 64 # GetDoublingRange(100, 8).min
JEWELRY_AND_CLOTHES_WARD = 6208 - 9 * 7 * 64   # ReferenceWardTable[7] minus 9 Covenant pieces


def skill_chance(skill, difficulty, factor=0.03):
    return 1.0 - 1.0 / (1.0 + math.exp(factor * (skill - difficulty)))


def seconds_per_hit(weapon_time, quickness=QUICKNESS, ranged=False):
    effect = 0.13 if ranged else 1.0
    qmod = lambda q: 1.0 + effect * max(0.0, q) / 600.0
    return weapon_time / 60.0 * qmod(100) / qmod(quickness)


def power_f(p):
    return (p / 2.0) ** 2 + 0.5


# ---------------------------------------------------------------- weapon DPS (x target), full power, no misses
def melee_dps(weapon_time, power=1.0, swing_speed=1.0, refill_speed=1.0):
    """
    Each hit deals base * f(P) * (L + P) / L where L is the swing length (GetPowerMod), then waits P / refill_speed
    before the next swing. The bottom roll is sized so base / seconds_per_hit = target.
    The fix (AFTER) makes GetPowerMod use the real refill delay instead of P.
    """
    s = seconds_per_hit(weapon_time)
    L = s / swing_speed
    refill = power / refill_speed
    comp = refill if AFTER else power
    per_hit = s * power_f(power) * (L + comp) / L
    return per_hit / (L + refill)


def missile_dps(per_shot_expected, attack_time, power=1.0, scale=0.55):
    L = attack_time * scale
    per_shot = per_shot_expected * power_f(power) * (L + power) / L
    return per_shot / (attack_time + power) / D


def launcher_expected(ammo_max, ammo_variance):
    avg = ammo_max * (1 - ammo_variance / 2)
    return MULT_MIN * (avg * 0.9 + ammo_max * 0.2)       # 10% crits for 2x max


def combat_skill_bonus(skill, cap=0.5):
    return 1.0 + cap * skill_chance(int(skill * 1.5), MONSTER_DEF)


def spell_dps(min_dmg, max_dmg, cast_time, crit_mult=2.0, mod=MULT_MIN):
    avg = (min_dmg + max_dmg) / 2
    return mod * (avg * 0.9 + 0.1 * crit_mult * max_dmg) / cast_time / D


# ---------------------------------------------------------------- hit retention vs monster defense
def retained(attack_skill):
    """fraction of damage that lands: evade -> 1/3 full evade, 1/3 glancing (x0.5), 1/3 full hit"""
    evade = skill_chance(MONSTER_DEF, attack_skill)
    return 1.0 - evade * 0.5


MARKDOWN = "--markdown" in sys.argv


def show(title, rows, cols):
    if MARKDOWN:
        print(f"\n**{title}**\n")
        print("| | " + " | ".join(cols) + " |")
        print("|---|" + "---:|" * len(cols))
        for name, vals in rows:
            print(f"| {name} | " + " | ".join(f"{v:.2f}" for v in vals) + " |")
        return
    print(f"\n== {title} ==")
    print("  " + " | ".join(f"{c:>11}" for c in cols))
    for name, vals in rows:
        print(f"  {name:<34}" + " ".join(f"{v:11.3f}" for v in vals))


PLAYER_SKILL = 300            # LevelScaling.AvgPlayerAttackSkillPerTier[7] (L100)

# attack skill multipliers by family
MELEE_ATTACK = 1.2            # Medium height, specialized weapon (DamageEvent_Defense)
MISSILE_ATTACK = 1.1 * 1.2    # accuracy bar (GetAccuracySkillMod) x Medium height
SPELL_ATTACK = 1.2 if AFTER else 1.0

two_h = combat_skill_bonus(PLAYER_SKILL)
shield_bonus = combat_skill_bonus(PLAYER_SKILL, cap=0.25) if AFTER else 1.0

bow = missile_dps(launcher_expected(80, 0.6), seconds_per_hit(56, ranged=True))
xbow = missile_dps(launcher_expected(112, 0.4), seconds_per_hit(93, ranged=True))
atlatl = missile_dps(launcher_expected(140, 0.75), seconds_per_hit(98, ranged=True))
thrown_t = seconds_per_hit(85, ranged=True)
thrown = missile_dps(D * thrown_t, thrown_t)

war = spell_dps(130, 260, 2.88)                       # Flame Bolt VII, SpellBoltArcAnimLengthPerLevel[7]
harm = spell_dps(65, 130, 2.88, crit_mult=1.5)        # Harm Other VII (half a bolt), x1.5 boost crit

weapons = {
    "2H (Str)": (melee_dps(65) * two_h, MELEE_ATTACK),
    "Sword & board (Str)": (melee_dps(50) * shield_bonus, MELEE_ATTACK),
    "Thrown (Str)": (thrown, MISSILE_ATTACK),
    "Dual wield, spec (Coord)": (melee_dps(40, swing_speed=1.25, refill_speed=1.25) * two_h, MELEE_ATTACK),
    "Dual wield, trained (Coord)": (melee_dps(40) * two_h, MELEE_ATTACK),
    "Bow (Coord)": (bow, MISSILE_ATTACK),
    "Crossbow (Coord)": (xbow, MISSILE_ATTACK),
    "Atlatl (Coord)": (atlatl, MISSILE_ATTACK),
    "War magic (Self)": (war, SPELL_ATTACK),
    "Life magic, harms (Self)": (harm, SPELL_ATTACK),
}

rows = []
for name, (dps, atk) in weapons.items():
    rows.append((name, [dps, retained(PLAYER_SKILL * atk), dps * retained(PLAYER_SKILL * atk),
                        dps * retained((PLAYER_SKILL + 50) * atk)]))
show(("AFTER" if AFTER else "BEFORE") + " - weapon DPS at T8 (x target), full power",
     rows, ["raw", "hit@-50", "eff@-50", "eff@even"])

# ---------------------------------------------------------------- focus offense multipliers (single target, sustained)
# Fury: ~6 s to fill at full power, Enrage 10 s at +100% and no evade, 20 s cooldown from the Enrage
fury_cycle = 6 + 20
fury = (10 * 2.0 + 10 * 1.0 + 6 * 1.12) / fury_cycle
# Overload: 4 casts (0.288 charge each) to fill, 10 s discharge at +100%, 20 s cooldown
over_build = 4 * 2.88
overload = (10 * 2.0 + 10 * 1.0 + over_build * 1.10) / (20 + over_build)
# Steady Strike: 10 s of +25% damage at 0.9x swing speed per 20 s
steady = 1 + 0.5 * (1.25 * 0.9 - 1)
# Multishot: 10 s per 20 s, two bonus arrows; with no other target they hit the main target for 50% (25% after)
leftover = 0.25 if AFTER else 0.5
multishot = 1 + 0.5 * 2 * leftover
# Shadow Flurry: every hit a sneak attack (+25%) for 10 s per 20 s
shadow = 1 + 0.5 * 0.25

focus = [
    ("Warrior (Fury)", fury),
    ("Blademaster (Fury, Steady Strike)", fury * steady),
    ("Archer (Fury, Multishot, Steady)", fury * multishot * steady),
    ("Vagabond (Shadow Flurry, Steady)", shadow * steady),
    ("Sorcerer (Overload)", overload),
    ("Spellsword (Enchanted Blade ~+10%)", 1.10),
]
show("focus damage multipliers (single target, sustained)", [(n, [v]) for n, v in focus], ["x"])

# ---------------------------------------------------------------- the seven builds
def eff(weapon):
    dps, atk = weapons[weapon]
    return dps * retained(PLAYER_SKILL * atk)

F = dict(focus)

# Fury's drawback: damage taken +25% x meter in the stance, and (AFTER) +25% x the released level while Enraged
fury_taken = (10 * (1.25 if AFTER else 1.0) + 6 * 1.12 + 10 * 1.0) / fury_cycle
FURY_USERS = ("Warrior", "Blademaster", "Archer")
builds = [
    ("2H heavy - Warrior", eff("2H (Str)") * F["Warrior (Fury)"]),
    ("2H heavy - Blademaster", eff("2H (Str)") * F["Blademaster (Fury, Steady Strike)"]),
    ("2H heavy - Spellsword", eff("2H (Str)") * F["Spellsword (Enchanted Blade ~+10%)"]),
    ("S&B heavy - Warrior", eff("Sword & board (Str)") * F["Warrior (Fury)"]),
    ("S&B heavy - Blademaster", eff("Sword & board (Str)") * F["Blademaster (Fury, Steady Strike)"]),
    ("S&B heavy - Spellsword", eff("Sword & board (Str)") * F["Spellsword (Enchanted Blade ~+10%)"]),
    ("Thrown heavy - Warrior", eff("Thrown (Str)") * F["Warrior (Fury)"]),
    ("Thrown heavy - Blademaster", eff("Thrown (Str)") * F["Blademaster (Fury, Steady Strike)"]),
    ("Thrown heavy - Archer", eff("Thrown (Str)") * F["Archer (Fury, Multishot, Steady)"]),
    ("DW light - Blademaster", eff("Dual wield, spec (Coord)") * F["Blademaster (Fury, Steady Strike)"]),
    ("DW light - Vagabond", eff("Dual wield, spec (Coord)") * F["Vagabond (Shadow Flurry, Steady)"]),
    ("Bow heavy - Archer", eff("Bow (Coord)") * F["Archer (Fury, Multishot, Steady)"]),
    ("Bow heavy - Vagabond", eff("Bow (Coord)") * F["Vagabond (Shadow Flurry, Steady)"] / shadow),
    ("War cloth - Sorcerer", eff("War magic (Self)") * F["Sorcerer (Overload)"]),
    ("War cloth - Vagabond (Battery)", eff("War magic (Self)")),
    ("Life cloth - Sorcerer (harms)", eff("Life magic, harms (Self)") * F["Sorcerer (Overload)"]),
]

# ---------------------------------------------------------------- defense
def ward_total(base_per_slot):
    return 9 * base_per_slot * 64 + JEWELRY_AND_CLOTHES_WARD

ward_base = {"heavy": 7, "light": 6 if AFTER else 5, "cloth": 8 if AFTER else 6}
al_base = {"heavy": 100, "light": 85, "cloth": 80}
ref_phys = BOTTOM_COVENANT_AL + 100
ref_magic = ward_total(7) + 100
phys = {k: ref_phys / (v * 64 + 100) for k, v in al_base.items()}
magic = {k: ref_magic / (ward_total(v) + 100) for k, v in ward_base.items()}
show("armor - damage taken vs the reference (bottom-roll Covenant)",
     [(k, [phys[k], magic[k]]) for k in al_base], ["physical", "magic"])

# melee damage avoidance: parry ~10% (2H / DW), shield block ~8% and shield mod ~14%
parry = 0.1 * skill_chance(int(PLAYER_SKILL * 1.5), MONSTER_DEF) * (1 + 0.5 * skill_chance(PLAYER_SKILL, MONSTER_DEF))
block = 0.05 * (1 + skill_chance(700, 700 * 1.0)) * (1 + 0.5 * skill_chance(PLAYER_SKILL, MONSTER_DEF))
shield_mod = 1 - 0.25 * 0.55
armor_of = {"2H": "heavy", "S&B": "heavy", "Thrown": "heavy", "DW": "light", "Bow": "heavy", "War": "cloth",
            "Life": "cloth"}
def taken(name):
    kind = name.split()[0]
    p = phys[armor_of[kind]]
    if any(f in name for f in FURY_USERS):
        p *= fury_taken
    if kind in ("2H", "DW"):
        p *= 1 - parry
    if kind == "S&B":
        p *= (1 - block) * shield_mod
    return p, magic[armor_of[kind]]

rows = []
for name, dps in builds:
    p, m = taken(name)
    rows.append((name, [dps, p, m, dps / p]))
show("builds - single target (x target DPS, misses included at 50 under monster defense)",
     rows, ["dps", "phys taken", "magic taken", "dps/phys"])
