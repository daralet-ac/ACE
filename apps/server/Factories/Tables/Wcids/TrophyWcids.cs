using System;
using System.Collections.Generic;
using ACE.Entity.Enum;

namespace ACE.Server.Factories.Tables.Wcids;

/// <summary>
/// Single source of truth for the trophy WCID layout: 65 trophy types, each with 10
/// per-quality WCIDs (base + quality - 1, quality 1-10 / Damaged-Peerless), stride 10.
/// Also carries the legacy pre-refactor (single-WCID-per-trophy, mutated-quality) WCIDs,
/// needed until the shard migration retires them - existing player-held trophies still
/// carry these.
/// </summary>
public static class TrophyWcids
{
    public const int MinQuality = 1;
    public const int MaxQuality = 10;
    private const uint Stride = 10;

    /// <summary>
    /// The 65 trophy base (quality 1 / Damaged) WCIDs, current scheme.
    /// </summary>
    public static readonly HashSet<uint> BaseWcids = new HashSet<uint>
    {
        (uint)WeenieClassName.W_ARMOREDILLOHIDETROPHY_CLASS,
        (uint)WeenieClassName.W_ARMOREDILLOSPINE_CLASS,
        (uint)WeenieClassName.W_AUROCHMEAT_CLASS,
        (uint)WeenieClassName.W_AUROCHHORNTROPHY_CLASS,
        (uint)WeenieClassName.W_BANDERLINGSCALPTROPHY_CLASS,
        (uint)WeenieClassName.W_BANDERLINGBLOOD_CLASS,
        (uint)WeenieClassName.W_CHITTICKSPINE_CLASS,
        (uint)WeenieClassName.W_CHITTICKHEAD_CLASS,
        (uint)WeenieClassName.W_DRUDGEGUTS_CLASS,
        (uint)WeenieClassName.W_DRUDGECHARMTROPHY_CLASS,
        (uint)WeenieClassName.W_ECTOPLASM_CLASS,
        (uint)WeenieClassName.W_DOLLMASK_CLASS,
        (uint)WeenieClassName.W_VIOLETENERGY_CLASS,
        (uint)WeenieClassName.W_GRIEVVERSILK_CLASS,
        (uint)WeenieClassName.W_GRIEVVERTIBIA_CLASS,
        (uint)WeenieClassName.W_GROMNIETOOTH_CLASS,
        (uint)WeenieClassName.W_GROMNIEWINGTROPHY_CLASS,
        (uint)WeenieClassName.W_BROWNLUMPTROPHY_CLASS,
        (uint)WeenieClassName.W_KNATHEGG_CLASS,
        (uint)WeenieClassName.W_LUGIANBLOOD_CLASS,
        (uint)WeenieClassName.W_LUGIANSINEW_CLASS,
        (uint)WeenieClassName.W_MATTEKARHIDETROPHY_CLASS,
        (uint)WeenieClassName.W_MATTEKARHORN_CLASS,
        (uint)WeenieClassName.W_MITEFUR_CLASS,
        (uint)WeenieClassName.W_MITEHEART_CLASS,
        (uint)WeenieClassName.W_MONOUGATTROPHY_CLASS,
        (uint)WeenieClassName.W_MONOUGASKULL_CLASS,
        (uint)WeenieClassName.W_MOSSWARTEGGS_CLASS,
        (uint)WeenieClassName.W_SWAMPSTONE_CLASS,
        (uint)WeenieClassName.W_MUMIYAHARM_CLASS,
        (uint)WeenieClassName.W_TOMBDUST_CLASS,
        (uint)WeenieClassName.W_NIFFISSHELL_CLASS,
        (uint)WeenieClassName.W_NIFFISPEARL_CLASS,
        (uint)WeenieClassName.W_OLTHOICLAWTROPHY_CLASS,
        (uint)WeenieClassName.W_OLTHOIICHOR_CLASS,
        (uint)WeenieClassName.W_WASPVENOM_CLASS,
        (uint)WeenieClassName.W_WASPWINGTRPHY_CLASS,
        (uint)WeenieClassName.W_RATTAIL_CLASS,
        (uint)WeenieClassName.W_RATSALIVA_CLASS,
        (uint)WeenieClassName.W_REEDSHARKFANG_CLASS,
        (uint)WeenieClassName.W_REEDSHARKHIDETROPHY_CLASS,
        (uint)WeenieClassName.W_SCLAVUSHIDETROPHY_CLASS,
        (uint)WeenieClassName.W_SCLAVUSTONGUE_CLASS,
        (uint)WeenieClassName.W_SHRETHTOOTH_CLASS,
        (uint)WeenieClassName.W_SHRETHHIDETROPHY_CLASS,
        (uint)WeenieClassName.W_OLDBONE_CLASS,
        (uint)WeenieClassName.W_SKULLTROPHY_CLASS,
        (uint)WeenieClassName.W_TUSKERPELT_CLASS,
        (uint)WeenieClassName.W_TUSKERTUSK_CLASS,
        (uint)WeenieClassName.W_UNDEADLEG_CLASS,
        (uint)WeenieClassName.W_MNEMOSYNETRPHY_CLASS,
        (uint)WeenieClassName.W_URSUINFANGTROPHY_CLASS,
        (uint)WeenieClassName.W_URSUINHIDE_CLASS,
        (uint)WeenieClassName.W_ZEFIRGOSSAMER_CLASS,
        (uint)WeenieClassName.W_ZEFIRWINGTRPHY_CLASS,
        (uint)WeenieClassName.W_WISPHEARTTROPHY_CLASS,
        (uint)WeenieClassName.W_WISPESSENCE_CLASS,
        (uint)WeenieClassName.W_TUMEROKINSIGNIATROPHY_CLASS,
        (uint)WeenieClassName.W_TUMEROKSALTEDMEATS_CLASS,
        (uint)WeenieClassName.W_MOARSMUCK_CLASS,
        (uint)WeenieClassName.W_MOARSMANHEAD_CLASS,
        (uint)WeenieClassName.W_CRYSTALIZEDFIRE_CLASS,
        (uint)WeenieClassName.W_CRYSTALIZEDFROST_CLASS,
        (uint)WeenieClassName.W_CRYSTALIZEDACID_CLASS,
        (uint)WeenieClassName.W_CRYSTALIZEDLIGHTNING_CLASS,
    };

    /// <summary>
    /// Legacy (pre-refactor) single WCID per trophy -> current base WCID.
    /// </summary>
    private static readonly Dictionary<uint, WeenieClassName> LegacyBaseWcids = new Dictionary<uint, WeenieClassName>
    {
        { 1054100, WeenieClassName.W_ARMOREDILLOHIDETROPHY_CLASS }, // Armoredillo Hide
        { 1054101, WeenieClassName.W_ARMOREDILLOSPINE_CLASS }, // Armoredillo Spine
        { 1054102, WeenieClassName.W_AUROCHMEAT_CLASS }, // Auroch Meat
        { 1054103, WeenieClassName.W_AUROCHHORNTROPHY_CLASS }, // Auroch Horn
        { 1054104, WeenieClassName.W_BANDERLINGSCALPTROPHY_CLASS }, // Banderling Scalp
        { 1054105, WeenieClassName.W_BANDERLINGBLOOD_CLASS }, // Banderling Blood
        { 1054106, WeenieClassName.W_CHITTICKSPINE_CLASS }, // Chittick Spine
        { 1054107, WeenieClassName.W_CHITTICKHEAD_CLASS }, // Chittick Head
        { 1054108, WeenieClassName.W_DRUDGEGUTS_CLASS }, // Drudge Guts
        { 1054109, WeenieClassName.W_DRUDGECHARMTROPHY_CLASS }, // Drudge Charm
        { 1054110, WeenieClassName.W_ECTOPLASM_CLASS }, // Ectoplasm
        { 1054111, WeenieClassName.W_DOLLMASK_CLASS }, // Doll Mask
        { 1054112, WeenieClassName.W_VIOLETENERGY_CLASS }, // Violet Energy
        { 1054113, WeenieClassName.W_GRIEVVERSILK_CLASS }, // Grievver Silk
        { 1054114, WeenieClassName.W_GRIEVVERTIBIA_CLASS }, // Grievver Tibia
        { 1054115, WeenieClassName.W_GROMNIETOOTH_CLASS }, // Gromnie Tooth
        { 1054116, WeenieClassName.W_GROMNIEWINGTROPHY_CLASS }, // Gromnie Wing
        { 1054117, WeenieClassName.W_BROWNLUMPTROPHY_CLASS }, // Brown Lump
        { 1054118, WeenieClassName.W_KNATHEGG_CLASS }, // K'nath Egg
        { 1054119, WeenieClassName.W_LUGIANBLOOD_CLASS }, // Lugian Blood
        { 1054120, WeenieClassName.W_LUGIANSINEW_CLASS }, // Lugian Sinew
        { 1054121, WeenieClassName.W_MATTEKARHIDETROPHY_CLASS }, // Mattekar Hide
        { 1054122, WeenieClassName.W_MATTEKARHORN_CLASS }, // Mattekar Horn
        { 1054123, WeenieClassName.W_MITEFUR_CLASS }, // Mite Fur
        { 1054124, WeenieClassName.W_MITEHEART_CLASS }, // Mite Heart
        { 1054125, WeenieClassName.W_MONOUGATTROPHY_CLASS }, // Monougat
        { 1054126, WeenieClassName.W_MONOUGASKULL_CLASS }, // Monouga Skull
        { 1054127, WeenieClassName.W_MOSSWARTEGGS_CLASS }, // Mosswart Eggs
        { 1054128, WeenieClassName.W_SWAMPSTONE_CLASS }, // Swamp Stone
        { 1054129, WeenieClassName.W_MUMIYAHARM_CLASS }, // Mu-miyah Arm
        { 1054130, WeenieClassName.W_TOMBDUST_CLASS }, // Tomb Dust
        { 1054131, WeenieClassName.W_NIFFISSHELL_CLASS }, // Niffis Shell
        { 1054132, WeenieClassName.W_NIFFISPEARL_CLASS }, // Niffis Pearl
        { 1054133, WeenieClassName.W_OLTHOICLAWTROPHY_CLASS }, // Olthoi Claw
        { 1054134, WeenieClassName.W_OLTHOIICHOR_CLASS }, // Olthoi Ichor
        { 1054135, WeenieClassName.W_WASPVENOM_CLASS }, // Wasp Venom
        { 1054136, WeenieClassName.W_WASPWINGTRPHY_CLASS }, // Wasp Wing
        { 1054137, WeenieClassName.W_RATTAIL_CLASS }, // Rat Tail
        { 1054138, WeenieClassName.W_RATSALIVA_CLASS }, // Rat Saliva
        { 1054139, WeenieClassName.W_REEDSHARKFANG_CLASS }, // Reedshark Fang
        { 1054140, WeenieClassName.W_REEDSHARKHIDETROPHY_CLASS }, // Reedshark Hide
        { 1054141, WeenieClassName.W_SCLAVUSHIDETROPHY_CLASS }, // Sclavus Hide
        { 1054142, WeenieClassName.W_SCLAVUSTONGUE_CLASS }, // Sclavus Tongue
        { 1054143, WeenieClassName.W_SHRETHTOOTH_CLASS }, // Shreth Tooth
        { 1054144, WeenieClassName.W_SHRETHHIDETROPHY_CLASS }, // Shreth Hide
        { 1054145, WeenieClassName.W_OLDBONE_CLASS }, // Old Bone
        { 1054146, WeenieClassName.W_SKULLTROPHY_CLASS }, // Skull
        { 1054147, WeenieClassName.W_TUSKERPELT_CLASS }, // Tusker Pelt
        { 1054148, WeenieClassName.W_TUSKERTUSK_CLASS }, // Tusker Tusk
        { 1054149, WeenieClassName.W_UNDEADLEG_CLASS }, // Undead Leg
        { 1054150, WeenieClassName.W_MNEMOSYNETRPHY_CLASS }, // Mnemosyne
        { 1054151, WeenieClassName.W_URSUINFANGTROPHY_CLASS }, // Ursuin Fang
        { 1054152, WeenieClassName.W_URSUINHIDE_CLASS }, // Ursuin Hide
        { 1054153, WeenieClassName.W_ZEFIRGOSSAMER_CLASS }, // Zefir Gossamer
        { 1054154, WeenieClassName.W_ZEFIRWINGTRPHY_CLASS }, // Zefir Wing
        { 1054155, WeenieClassName.W_WISPHEARTTROPHY_CLASS }, // Wisp Heart
        { 1054156, WeenieClassName.W_WISPESSENCE_CLASS }, // Wisp Essence
        { 1054157, WeenieClassName.W_TUMEROKINSIGNIATROPHY_CLASS }, // Tumerok Insignia
        { 1054158, WeenieClassName.W_TUMEROKSALTEDMEATS_CLASS }, // Tumerok Salted Meats
        { 1054159, WeenieClassName.W_MOARSMUCK_CLASS }, // Moarsmuck
        { 1054160, WeenieClassName.W_MOARSMANHEAD_CLASS }, // Moarsman Head
        { 1054161, WeenieClassName.W_CRYSTALIZEDFIRE_CLASS }, // Crystalized Fire
        { 1054162, WeenieClassName.W_CRYSTALIZEDFROST_CLASS }, // Crystalized Frost
        { 1054163, WeenieClassName.W_CRYSTALIZEDACID_CLASS }, // Crystalized Acid
        { 1054164, WeenieClassName.W_CRYSTALIZEDLIGHTNING_CLASS }, // Crystalized Lightning
    };

    // The new-range base WCIDs are a single contiguous stride-10 block (no gaps),
    // so a base can be resolved by arithmetic instead of scanning BaseWcids.
    private static readonly uint NewRangeMin;
    private static readonly uint NewRangeMax;

    static TrophyWcids()
    {
        NewRangeMin = uint.MaxValue;
        NewRangeMax = uint.MinValue;

        foreach (var baseWcid in BaseWcids)
        {
            if (baseWcid < NewRangeMin)
            {
                NewRangeMin = baseWcid;
            }

            if (baseWcid > NewRangeMax)
            {
                NewRangeMax = baseWcid;
            }
        }
    }

    /// <summary>
    /// Resolves any trophy WCID - a legacy pre-refactor WCID, or a new-range WCID at
    /// any quality - down to its base (quality 1) WCID. Returns 0 if <paramref name="wcid"/>
    /// isn't a trophy WCID at all.
    /// </summary>
    public static uint ToBaseTrophyWcid(uint wcid)
    {
        if (LegacyBaseWcids.TryGetValue(wcid, out var legacyBase))
        {
            return (uint)legacyBase;
        }

        if (wcid >= NewRangeMin && wcid < NewRangeMax + Stride)
        {
            var candidateBase = NewRangeMin + (wcid - NewRangeMin) / Stride * Stride;
            if (BaseWcids.Contains(candidateBase))
            {
                return candidateBase;
            }
        }

        return 0;
    }

    /// <summary>
    /// Returns the WCID for a specific quality (1-10) of the trophy identified by
    /// <paramref name="baseWcid"/> (its quality-1 / Damaged WCID).
    /// </summary>
    public static uint GetTrophyWcid(uint baseWcid, int quality)
    {
        quality = Math.Clamp(quality, MinQuality, MaxQuality);
        return baseWcid + (uint)(quality - MinQuality);
    }

    /// <summary>
    /// Returns the quality (1-10) encoded by a new-range trophy WCID, given its base.
    /// </summary>
    public static int GetTrophyQuality(uint wcid, uint baseWcid)
    {
        return (int)(wcid - baseWcid) + MinQuality;
    }

    /// <summary>
    /// True if <paramref name="wcid"/> is one of the legacy (pre-refactor) single-WCID-
    /// per-trophy values, which don't encode a quality - existing held items only.
    /// </summary>
    public static bool IsLegacyTrophyWcid(uint wcid)
    {
        return LegacyBaseWcids.ContainsKey(wcid);
    }
}
