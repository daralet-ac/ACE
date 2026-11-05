#!/usr/bin/env python3
"""
Combat balance model for the Daralet ACE fork.

Mirrors the server's damage formulas closely enough to compare builds against each other. Every number is relative to
the tier's target DPS (TierPower.WeaponTargetDpsTable), so 1.00 = "a bottom-roll weapon of the tier deals exactly the
target", and the reference player (bottom-roll Covenant, TierPower) takes 1.00 damage.

Run:  python3 docs/combat-balance-model.py            (before the 2026-11 balance pass)
      python3 docs/combat-balance-model.py --after    (with it; add --markdown for tables)

Every Combat Focus ability is simulated over time (0.1 s steps over 1300 s), each used on cooldown against one
same-level monster, so overlapping windows, auto-hits and attack-skill bonuses all count. Abilities on the same 20 s
cooldown are used together.

Sources (apps/server):
  melee power/refill .......... Player_Combat.GetPowerMod, DamageFormulas.GetPowerMod, Player_Melee
  missile power/timing ........ Player_Missile.LaunchMissile (missileAttackLengthScale)
  weapon damage rolls ......... LootGenerationFactory_Melee.GetMeleeDamageRollRange, _Missile, _Caster, TierPower
  2H / DW bonus ............... DamageFormulas.GetCombatSkillDamageBonus
  evade / resist / block ...... SkillCheck, DamageFormulas, DamageEvent_Defense, WorldObject_Magic_Resist
  monster skills .............. Creature_ArchetypeSystem (avg player skill + EnemySkillGap 50)
  armor / ward ................ SkillFormula.CalcArmorMod/CalcWardMod, LootGenerationFactory_Clothing.GetArmorBaseLevels
  abilities ................... Player_Ability, DamageEvent_*, Player_Missile.LaunchBonusProjectiles, EnchantmentManager
"""
import math
import sys

AFTER = "--after" in sys.argv
MARKDOWN = "--markdown" in sys.argv

# ---------------------------------------------------------------- tier 8 (level 100) reference
D = 640.0                     # TierPower.WeaponTargetDpsTable[7]
QUICKNESS = 195               # LootGenerationFactory_Weapon.AvgQuicknessPerTier[7]
PLAYER_SKILL = 300            # LevelScaling.AvgPlayerAttackSkillPerTier / AvgPlayerDefenseSkillPerTier (L100)
MONSTER_SKILL = 300 + 50      # avgPlayerPhysicalMagicDefense[7] + EnemySkillGap (attack, defense and magic)
MULT_MIN = 9.6                # TierPower.MultiplierMinimum[7] (casters and launchers)
MONSTER_ATTACKS_PER_SECOND = 0.8   # ~1.3 s animations at ~2x speed plus 0.5 s (Creature.GetAnimSpeed); an estimate
JEWELRY_AND_CLOTHES_WARD = 6208 - 9 * 7 * 64  # TierPower.ReferenceWardTable[7] minus 9 Covenant pieces


def skill_chance(skill, difficulty, factor=0.03):
    return 1.0 - 1.0 / (1.0 + math.exp(factor * (skill - difficulty)))


def seconds_per_hit(weapon_time, quickness=QUICKNESS, ranged=False):
    effect = 0.13 if ranged else 1.0
    qmod = lambda q: 1.0 + effect * max(0.0, q) / 600.0
    return weapon_time / 60.0 * qmod(100) / qmod(quickness)


def power_curve(p):
    return (p / 2.0) ** 2 + 0.5


def hit_factor(variance):
    """the expected hit (10% crits for 2x max) over max damage: what loot sizes weapon damage by"""
    return (1 - variance / 2) * 0.9 + 0.2


# ---------------------------------------------------------------- weapons (x target DPS, full power, every hit lands)
def melee_dps(weapon_time, power=1.0, swing_speed=1.0, refill_speed=1.0):
    """
    Each hit deals base x f(P) x (L + refill) / L, then waits refill before the next swing (GetPowerMod). The bottom
    roll is sized so base / seconds_per_hit = target. Before the pass, the payback used P even when Dual Wield
    specialization made the real refill P / 1.25.
    """
    s = seconds_per_hit(weapon_time)
    L = s / swing_speed
    refill = power / refill_speed
    payback = refill if AFTER else power
    return s * power_curve(power) * (L + payback) / L / (L + refill)


def missile_dps(per_shot_expected, attack_time, power=1.0, scale=0.55):
    L = attack_time * scale
    return per_shot_expected * power_curve(power) * (L + power) / L / (attack_time + power) / D


def launcher_expected(ammo_max, ammo_variance):
    return MULT_MIN * ammo_max * hit_factor(ammo_variance)


def combat_skill_bonus(skill):
    return 1.0 + 0.5 * skill_chance(int(skill * 1.5), MONSTER_SKILL)


def spell_dps(min_dmg, max_dmg, cast_time, crit_mult=2.0, mod=MULT_MIN):
    avg = (min_dmg + max_dmg) / 2
    return mod * (avg * 0.9 + 0.1 * crit_mult * max_dmg) / cast_time / D


TWO_H = combat_skill_bonus(PLAYER_SKILL)
BOW_TIME = seconds_per_hit(56, ranged=True)
THROWN_TIME = seconds_per_hit(85, ranged=True)


class Weapon:
    def __init__(self, name, family, dps, attack_time, hit_damage, riposte=0.0, shield=False, parry=False):
        self.name = name
        self.family = family            # melee / missile / spell
        self.dps = dps                  # x target, full power, every hit lands
        self.attack_time = attack_time  # seconds per attack, including the refill
        self.hit_damage = hit_damage    # one hit, in target-DPS seconds
        self.riposte = riposte          # one riposte counter (full power 1.0, no crit, can't be evaded)
        self.shield = shield
        self.parry = parry


def melee_weapon(name, weapon_time, variance, bonus=1.0, swing_speed=1.0, refill_speed=1.0, shield=False, parry=False):
    s = seconds_per_hit(weapon_time)
    dps = melee_dps(weapon_time, swing_speed=swing_speed, refill_speed=refill_speed) * bonus
    attack_time = s / swing_speed + 1.0 / refill_speed
    riposte = s * (1 - variance / 2) / hit_factor(variance) * bonus
    return Weapon(name, "melee", dps, attack_time, dps * attack_time, riposte, shield, parry)


WEAPONS = {
    "2H": melee_weapon("Two-handed sword", 65, 0.5, TWO_H, parry=True),
    "S&B": melee_weapon("Sword and board", 50, 0.5, shield=True),
    "DW": melee_weapon("Dual wield daggers, spec", 40, 0.6, TWO_H, 1.25, 1.25, parry=True),
    "Thrown": Weapon("Thrown", "missile", missile_dps(D * THROWN_TIME, THROWN_TIME), THROWN_TIME + 1, 0),
    "Bow": Weapon("Bow", "missile", missile_dps(launcher_expected(80, 0.6), BOW_TIME), BOW_TIME + 1, 0),
    "Crossbow": Weapon("Crossbow", "missile",
                       missile_dps(launcher_expected(112, 0.4), seconds_per_hit(93, ranged=True)), 0, 0),
    "Atlatl": Weapon("Atlatl", "missile",
                     missile_dps(launcher_expected(140, 0.75), seconds_per_hit(98, ranged=True)), 0, 0),
    "War": Weapon("War magic (bolts)", "spell", spell_dps(130, 260, 2.88), 2.88, 0),
    "Life": Weapon("Life magic (harms)", "spell", spell_dps(65, 130, 2.88, crit_mult=1.5), 2.88, 0),
}
for w in WEAPONS.values():
    w.hit_damage = w.hit_damage or w.dps * w.attack_time

# attack skill multipliers: Medium attack height with the weapon specialized (+20%); before the pass missile weapons
# also had +10% from the accuracy bar, and spells nothing (now +10% with the school specialized)
ATTACK_MULT = {"melee": 1.2, "missile": 1.2 if AFTER else 1.32, "spell": 1.1 if AFTER else 1.0}


def retained(attack_skill, defense=MONSTER_SKILL):
    """the share of damage that lands: an evade is 1/3 a full evade, 1/3 a glancing blow (x0.5), 1/3 a full hit"""
    return 1.0 - skill_chance(defense, attack_skill) * 0.5


# ---------------------------------------------------------------- armor
AL_BASE = {"heavy": 100, "light": 85, "cloth": 80}
WARD_BASE = {"heavy": 7, "light": 5, "cloth": 6}
PHYS_ARMOR = {k: (100 * 64 + 100) / (v * 64 + 100) for k, v in AL_BASE.items()}
MAGIC_ARMOR = {k: (9 * 7 * 64 + JEWELRY_AND_CLOTHES_WARD + 100) / (9 * v * 64 + JEWELRY_AND_CLOTHES_WARD + 100)
               for k, v in WARD_BASE.items()}

# ---------------------------------------------------------------- abilities
FURY_CYCLE, FURY_BUILD, FURY_ENRAGE = 26.0, 6.0, 10.0   # ~6 s to fill, Enrage 10 s, 20 s cooldown from the Enrage
OVERLOAD_BUILD = 4 * 2.88                                 # 0.288 charge per level VII cast
OVERLOAD_CYCLE = OVERLOAD_BUILD + 20.0
MULTISHOT_FALLBACK = 0.25 if AFTER else 0.5
WEAPON_MASTER_DEBUFF_SECONDS = 10.0                       # Dazed / Unbalanced duration is in the dat: assumed
BLEED_TICKS = 4                                           # dot_Duration 20 s at the 5 s heartbeat
PHYSICAL_DEFENSE_SPEC = True


def window(t, cycle, start, length):
    return start <= t % cycle < start + length


def fury_state(t):
    """(damage dealt, damage taken, auto-hit) for Fury used on cooldown"""
    x = t % FURY_CYCLE
    if x < FURY_BUILD:
        meter = x / FURY_BUILD
        return 1 + 0.25 * meter, 1 + 0.25 * meter, False
    if x < FURY_BUILD + FURY_ENRAGE:
        return 2.0, 1.25 if AFTER else 1.0, True
    return 1.0, 1.0, False


def overload_state(t):
    """(damage, magic skill multiplier) for Overload used on cooldown"""
    x = t % OVERLOAD_CYCLE
    if x < OVERLOAD_BUILD:
        return 1 + 0.2 * x / OVERLOAD_BUILD, 1.0
    if x < OVERLOAD_BUILD + 10:
        return 2.0, 2.0
    return 1.0, 1.0


def bleed_damage(weapon_time, variance, range_max, power=1.0):
    """
    Weapon Master bleed: 100 x power x (weapon Damage / its type's old top damage) per tick
    (DamageFormulas.GetBleedDamageScale), in target-DPS seconds. Monster ward and the attribute mod apply as they do to
    the weapon's hits, so they cancel.
    """
    s = seconds_per_hit(weapon_time)
    max_damage = D * s / hit_factor(variance)
    return BLEED_TICKS * 100 * power * max_damage / range_max / D


def enchanted_blade_damage(weapon_spellcraft=400, arcane_lore=300, arcane_lore_spec=True, magic_skill=PLAYER_SKILL,
                           power=1.0):
    """
    Enchanted Blade: a spell rolled from half to all of (magic skill + 10% of spellcraft), nearest of 20/150/.../400,
    fired on the next hit for (spellcraft x 1%) x (1 + power). Specialized Arcane Lore adds half its skill to spellcraft.
    In target-DPS seconds, with its own resist chance (no spell accuracy: it's cast with the weapon's spellcraft).
    """
    bolt = {1: (6, 12), 2: (12, 24), 3: (24, 48), 4: (40, 80), 5: (60, 120), 6: (90, 180), 7: (130, 260)}
    diff = [20, 150, 200, 250, 300, 350, 400]
    spellcraft = weapon_spellcraft + (0.5 * arcane_lore if arcane_lore_spec else 0)
    skill = magic_skill + 0.1 * spellcraft
    steps = 200
    expected = 0.0
    for i in range(steps):
        roll = skill * (0.5 + 0.5 * (i + 0.5) / steps)
        level = min(range(len(diff)), key=lambda j: abs(diff[j] - roll)) + 1
        lo, hi = bolt[level]
        expected += ((lo + hi) / 2 * 0.9 + hi * 0.2) / steps
    return expected * spellcraft * 0.01 * (1 + power) * retained(skill) / D


# Combat Focus kits: the abilities each focus uses for damage and defense (Relentless and Battery are sustain only)
FOCUS = {
    "Warrior": {"Phalanx", "Provoke", "Riposte", "Fury", "Reflect", "Aegis"},
    "Blademaster": {"Weapon Master", "Provoke", "Riposte", "Fury", "Steady Strike", "Evasive Stance"},
    "Archer": {"Multishot", "Fury", "Steady Strike", "Evasive Stance", "Smokescreen"},
    "Vagabond": {"Shadow Flurry", "Steady Strike", "Evasive Stance", "Backstab", "Smokescreen", "Mana Barrier"},
    "Sorcerer": {"Overload", "Smokescreen", "Mana Barrier", "Reflect", "Aegis"},
    "Spellsword": {"Enchanted Blade", "Provoke", "Riposte", "Mana Barrier", "Reflect", "Aegis"},
    "None": set(),
}


def simulate(weapon_key, kit, armor, weapon_master=None, seconds=1300.0, dt=0.1):
    """Returns (damage x target, physical taken, magic taken) averaged over time"""
    w = WEAPONS[weapon_key]
    total_damage = phys_taken = magic_taken = 0.0
    steps = int(seconds / dt)
    for i in range(steps):
        t = i * dt
        on20 = window(t, 20, 0, 10)
        dmg, speed, atk, auto_hit, monster_def = 1.0, 1.0, 1.0, False, MONSTER_SKILL
        taken_phys = 1.0
        monster_atk = MONSTER_SKILL

        # ---- offense
        if "Fury" in kit and w.family != "spell":
            fd, ft, fa = fury_state(t)
            dmg *= fd
            taken_phys *= ft
            auto_hit = fa
        if "Steady Strike" in kit and w.family != "spell" and on20:
            dmg *= 1.25
            atk *= 1.25
            if w.family == "melee":
                speed *= 0.9
        if "Multishot" in kit and w.family == "missile" and on20:
            dmg *= 1 + 2 * MULTISHOT_FALLBACK
        if "Shadow Flurry" in kit and w.family == "melee" and on20:
            dmg *= 1.25
        if "Overload" in kit and w.family == "spell":
            od, om = overload_state(t)
            dmg *= od
            atk *= om
        if weapon_master == "sword" and on20:
            speed *= 1.25
        if weapon_master == "unarmed" and window(t, 20, 0, WEAPON_MASTER_DEBUFF_SECONDS):
            monster_def -= 100
        if weapon_master == "mace" and window(t, 20, 0, WEAPON_MASTER_DEBUFF_SECONDS):
            monster_atk -= 100

        hit = 1.0 if auto_hit else retained(PLAYER_SKILL * ATTACK_MULT[w.family] * atk, monster_def)
        total_damage += w.dps * dmg * speed * hit * dt

        # ---- physical defense: evade, block, parry, then damage reduction
        evade = 0.0 if ("Aegis" in kit and on20) else skill_chance(PLAYER_SKILL, monster_atk)
        if "Smokescreen" in kit and on20 and evade > 0:
            evade += (1 - evade) * 0.1
        full_hit, glancing = 1 - 2 * evade / 3, evade / 3
        if "Evasive Stance" in kit and not ("Aegis" in kit and on20):
            full_hit, glancing = 0.75 * full_hit, 0.75 * glancing

        spec_bonus = 0.5 * skill_chance(PLAYER_SKILL, monster_atk) if PHYSICAL_DEFENSE_SPEC else 0.0
        riposte = 1.0 if ("Riposte" in kit and on20) else 0.0
        phalanx = "Phalanx" in kit and (w.shield or w.name.startswith("Two"))
        block = parry = 0.0
        if w.shield:
            block = 0.05 * 1.5 * (1 + spec_bonus + riposte) * (1.45 if phalanx else 1.0)   # tower shield
        if w.parry:
            parry = 0.1 * skill_chance(int(PLAYER_SKILL * 1.5), monster_atk) * (1 + spec_bonus + riposte) \
                * (1.25 if phalanx else 1.0)
        stopped = 1 - (1 - block) * (1 - parry)
        reaching = (full_hit * (0.7 if phalanx else 1.0) + glancing * 0.5) * (1 - stopped)
        reduction = 1.0
        if w.shield:
            reduction *= 1 - 0.25 * 0.55                     # CalcShieldMod, shield a little above the attacker
        if "Provoke" in kit and on20:
            reduction *= 0.85
        if "Aegis" in kit and on20:
            reduction *= 0.5
        if "Mana Barrier" in kit:
            reduction *= 0.75
        no_kit = (1 - 2 * skill_chance(PLAYER_SKILL, MONSTER_SKILL) / 3) + skill_chance(PLAYER_SKILL, MONSTER_SKILL) / 6
        phys_taken += PHYS_ARMOR[armor] * taken_phys * reaching * reduction / no_kit * dt

        # riposte counters: every parry or block while Riposte is up, within 10 m
        if riposte and w.family == "melee":
            counters = MONSTER_ATTACKS_PER_SECOND * (1 - evade / 3) * stopped
            total_damage += counters * w.riposte * dt

        # ---- magic defense: resist (Reflect: +50% magic defense, and any resisted spell is reflected)
        reflect = "Reflect" in kit and on20
        resist = skill_chance(PLAYER_SKILL * (1.5 if reflect else 1.0), MONSTER_SKILL)
        m_full, m_partial = 1 - 2 * resist / 3, resist / 3
        if reflect:
            m_partial = 0.0
        if "Evasive Stance" in kit:
            m_full, m_partial = 0.75 * m_full, 0.75 * m_partial
        m_reaching = m_full * (0.7 if phalanx else 1.0) + m_partial * 0.5
        m_no_kit = (1 - 2 * skill_chance(PLAYER_SKILL, MONSTER_SKILL) / 3) + skill_chance(PLAYER_SKILL, MONSTER_SKILL) / 6
        m_reduction = 0.75 if "Mana Barrier" in kit else 1.0
        magic_taken += MAGIC_ARMOR[armor] * m_reaching * m_reduction / m_no_kit * dt

    # once-per-cooldown damage
    if weapon_master in ("axe", "dagger", "greataxe"):
        wt, var, rng = {"axe": (59, 0.6, 132), "dagger": (40, 0.6, 95), "greataxe": (68, 0.75, 132)}[weapon_master]
        total_damage += bleed_damage(wt, var, rng) * seconds / 20
    if "Enchanted Blade" in kit and w.family == "melee":
        total_damage += enchanted_blade_damage() * seconds / 10.5
    if "Backstab" in kit and w.family == "melee":
        # one sneak hit x2 instead of x1.25 per cycle, as the last hit of Shadow Flurry (which it ends)
        total_damage += 0.75 * w.hit_damage * retained(PLAYER_SKILL * ATTACK_MULT[w.family]) * seconds / 20

    return total_damage / seconds, phys_taken / seconds, magic_taken / seconds


def show(title, rows, cols, fmt="{:.2f}"):
    if MARKDOWN:
        print(f"\n**{title}**\n")
        print("| | " + " | ".join(cols) + " |")
        print("|---|" + "---:|" * len(cols))
        for name, vals in rows:
            print(f"| {name} | " + " | ".join(v if isinstance(v, str) else fmt.format(v) for v in vals) + " |")
        return
    print(f"\n== {title} ==")
    print(" " * 38 + " ".join(f"{c:>12}" for c in cols))
    for name, vals in rows:
        print(f"  {name:<36}" + " ".join(f"{v:>12}" if isinstance(v, str) else f"{v:12.3f}" for v in vals))


LABEL = "AFTER" if AFTER else "BEFORE"

if __name__ == "__main__":
    # weapons
    rows = []
    for key, w in WEAPONS.items():
        hit = retained(PLAYER_SKILL * ATTACK_MULT[w.family])
        rows.append((w.name, [w.dps, hit, w.dps * hit]))
    show(f"{LABEL} - weapons at T8 (x target), full power bar", rows, ["raw", "lands", "damage"])

    # each ability alone, on the weapon it's used with
    single = [
        ("Fury", "2H", None), ("Steady Strike", "2H", None), ("Riposte", "2H", None), ("Provoke", "2H", None),
        ("Phalanx", "2H", None), ("Aegis", "2H", None), ("Reflect", "2H", None), ("Evasive Stance", "2H", None),
        ("Smokescreen", "Bow", None), ("Mana Barrier", "War", None), ("Multishot", "Bow", None),
        ("Fury", "Bow", None), ("Steady Strike", "Bow", None), ("Shadow Flurry", "DW", None),
        ("Overload", "War", None), ("Enchanted Blade", "2H", None),
    ]
    rows = []
    for ability, weapon, _ in single:
        base = simulate(weapon, set(), "heavy", seconds=260)
        with_it = simulate(weapon, {ability}, "heavy", seconds=260)
        rows.append((f"{ability} ({weapon})", [f"{(with_it[0] / base[0] - 1) * 100:+.0f}%",
                                                f"{(with_it[1] / base[1] - 1) * 100:+.0f}%",
                                                f"{(with_it[2] / base[2] - 1) * 100:+.0f}%"]))
    for wm, weapon in (("sword", "2H"), ("greataxe", "2H"), ("axe", "S&B"), ("dagger", "DW"), ("unarmed", "DW"),
                       ("mace", "S&B")):
        base = simulate(weapon, set(), "heavy", seconds=260)
        with_it = simulate(weapon, set(), "heavy", weapon_master=wm, seconds=260)
        rows.append((f"Weapon Master - {wm} ({weapon})", [f"{(with_it[0] / base[0] - 1) * 100:+.0f}%",
                                                          f"{(with_it[1] / base[1] - 1) * 100:+.0f}%",
                                                          f"{(with_it[2] / base[2] - 1) * 100:+.0f}%"]))
    show(f"{LABEL} - each ability alone, used on cooldown", rows, ["damage", "phys taken", "magic taken"])

    # Enchanted Blade by spellcraft and Arcane Lore
    base = simulate("2H", set(), "heavy", seconds=260)[0]
    rows = []
    for name, kwargs in [
        ("400 spellcraft, Arcane Lore not spec", dict(arcane_lore_spec=False)),
        ("400 spellcraft, Arcane Lore 300 spec", dict()),
        ("600 spellcraft, Arcane Lore 300 spec", dict(weapon_spellcraft=600)),
        ("600 spellcraft, Arcane Lore 400 spec", dict(weapon_spellcraft=600, arcane_lore=400)),
    ]:
        proc = enchanted_blade_damage(**kwargs)
        rows.append((name, [proc, f"{proc / 10.5 / base * 100:+.0f}%"]))
    show("Enchanted Blade on a two-handed Spellsword (per 10.5 s)", rows, ["proc x D-s", "damage"])

    # the builds, with their focus kits
    builds = [
        ("2H heavy - Warrior", "2H", "Warrior", "heavy", None),
        ("2H heavy - Blademaster (greataxe)", "2H", "Blademaster", "heavy", "greataxe"),
        ("2H heavy - Spellsword", "2H", "Spellsword", "heavy", None),
        ("S&B heavy - Warrior", "S&B", "Warrior", "heavy", None),
        ("S&B heavy - Blademaster (axe)", "S&B", "Blademaster", "heavy", "axe"),
        ("S&B heavy - Spellsword", "S&B", "Spellsword", "heavy", None),
        ("Thrown heavy - Warrior", "Thrown", "Warrior", "heavy", None),
        ("Thrown heavy - Blademaster", "Thrown", "Blademaster", "heavy", None),
        ("Thrown heavy - Archer", "Thrown", "Archer", "heavy", None),
        ("DW light - Blademaster (daggers)", "DW", "Blademaster", "light", "dagger"),
        ("DW light - Vagabond", "DW", "Vagabond", "light", None),
        ("Bow heavy - Archer", "Bow", "Archer", "heavy", None),
        ("Bow heavy - Vagabond", "Bow", "Vagabond", "heavy", None),
        ("War cloth - Sorcerer", "War", "Sorcerer", "cloth", None),
        ("War cloth - Vagabond", "War", "Vagabond", "cloth", None),
        ("Life cloth - Sorcerer (harms)", "Life", "Sorcerer", "cloth", None),
    ]
    rows = []
    for name, weapon, focus, armor, wm in builds:
        dmg, phys, magic = simulate(weapon, FOCUS[focus], armor, weapon_master=wm)
        rows.append((name, [dmg, phys, magic, dmg / phys]))
    show(f"{LABEL} - builds with their focus kits (single target, misses included)", rows,
         ["damage", "phys taken", "magic taken", "dmg/phys"])
