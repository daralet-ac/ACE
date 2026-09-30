using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using WeenieClassName = ACE.Server.Factories.Enum.WeenieClassName;

namespace ACE.Server.Entity;

/// <summary>
/// A piece of armor that shows what its style looks like at one coverage.
/// Variant separates looks within a style, such as the original and newer Leather sets.
/// </summary>
public record ArmorStylePiece(
    uint Wcid,
    ArmorStyle Style,
    int Variant,
    ArmorWeightClass WeightClass,
    uint ClothingBase,
    CoverageMask Coverage
);

/// <summary>
/// The armor styles the Armor Style Copier knows. A style is the look of an armor set (Leather, Studded Leather, Koujia...)
/// apart from the slots a piece covers, so a template taken from any piece of a style can restyle armor of any coverage
/// that some piece of that style has. Koujia has no bracers, so Koujia can't go on bracers.
///
/// Each style lists the loot pieces the server tags with that ArmorStyle and that drop as part of that set.
/// Pieces a set borrows from another style (Yoroi's leather gauntlets, Celdon's platemail gauntlets) are left out.
/// Where a style has several pieces with the same coverage, the first listed is used.
/// </summary>
public class ArmorStyleCatalogue
{
    private static readonly (
        ArmorStyle Style,
        ArmorWeightClass WeightClass,
        WeenieClassName[][] Variants
    )[] Definitions =
    [
        (
            ArmorStyle.Leather,
            ArmorWeightClass.Light,
            [
                [
                    WeenieClassName.capleather,
                    WeenieClassName.cowlleather,
                    WeenieClassName.basinetleather,
                    WeenieClassName.gauntletsleather,
                    WeenieClassName.bootsleather,
                    WeenieClassName.breastplateleather,
                    WeenieClassName.girthleather,
                    WeenieClassName.pauldronsleather,
                    WeenieClassName.bracersleather,
                    WeenieClassName.tassetsleather,
                    WeenieClassName.greavesleather,
                    WeenieClassName.cuirassleather,
                    WeenieClassName.shirtleather,
                    WeenieClassName.coatleather,
                    WeenieClassName.sleevesleather,
                    WeenieClassName.leggingsleather,
                ],
                [
                    WeenieClassName.basinetleathernew,
                    WeenieClassName.cowlleathernew,
                    WeenieClassName.gauntletsleathernew,
                    WeenieClassName.longgauntletsleathernew,
                    WeenieClassName.bootsleathernew,
                    WeenieClassName.breastplateleathernew,
                    WeenieClassName.girthleathernew,
                    WeenieClassName.pauldronsleathernew,
                    WeenieClassName.bracersleathernew,
                    WeenieClassName.tassetsleathernew,
                    WeenieClassName.greavesleathernew,
                    WeenieClassName.cuirassleathernew,
                    WeenieClassName.shirtleathernew,
                    WeenieClassName.coatleathernew,
                    WeenieClassName.sleevesleathernew,
                    WeenieClassName.leggingsleathernew,
                    WeenieClassName.pantsleathernew,
                    WeenieClassName.shortsleathernew,
                ],
            ]
        ),
        (
            ArmorStyle.StuddedLeather,
            ArmorWeightClass.Light,
            [
                [
                    WeenieClassName.basinetstuddedleather,
                    WeenieClassName.cowlstuddedleather,
                    WeenieClassName.gauntletsstuddedleather,
                    WeenieClassName.bootsreinforcedleather,
                    WeenieClassName.breastplatestuddedleather,
                    WeenieClassName.girthstuddedleather,
                    WeenieClassName.pauldronsstuddedleather,
                    WeenieClassName.bracersstuddedleather,
                    WeenieClassName.tassetsstuddedleather,
                    WeenieClassName.greavesstuddedleather,
                    WeenieClassName.cuirassstuddedleather,
                    WeenieClassName.shirtstuddedleather,
                    WeenieClassName.coatstuddedleather,
                    WeenieClassName.sleevesstuddedleather,
                    WeenieClassName.leggingsstuddedleather,
                ],
            ]
        ),
        (
            ArmorStyle.Yoroi,
            ArmorWeightClass.Light,
            [
                [
                    WeenieClassName.kabuton,
                    WeenieClassName.breastplateyoroi,
                    WeenieClassName.girthyoroi,
                    WeenieClassName.pauldronsyoroi,
                    WeenieClassName.kote,
                    WeenieClassName.tassetsyoroi,
                    WeenieClassName.greavesyoroi,
                    WeenieClassName.cuirassyoroi,
                    WeenieClassName.sleevesyoroi,
                    WeenieClassName.leggingsyoroi,
                ],
            ]
        ),
        (
            ArmorStyle.Koujia,
            ArmorWeightClass.Light,
            [
                [WeenieClassName.breastplatekoujia, WeenieClassName.sleeveskoujia, WeenieClassName.leggingskoujia]
            ]
        ),
        (
            ArmorStyle.Lorica,
            ArmorWeightClass.Light,
            [
                [
                    WeenieClassName.helmlorica,
                    WeenieClassName.gauntletslorica,
                    WeenieClassName.bootslorica,
                    WeenieClassName.breastplatelorica,
                    WeenieClassName.sleeveslorica,
                    WeenieClassName.leggingslorica,
                ],
            ]
        ),
        (
            ArmorStyle.OlthoiKoujia,
            ArmorWeightClass.Light,
            [
                [
                    WeenieClassName.ace37198_olthoikoujiakabuton,
                    WeenieClassName.ace37190_olthoikoujiagauntlets,
                    WeenieClassName.ace37210_olthoikoujiasollerets,
                    WeenieClassName.ace37215_olthoikoujiabreastplate,
                    WeenieClassName.ace37206_olthoikoujiasleeves,
                    WeenieClassName.ace37203_olthoikoujialeggings,
                ],
            ]
        ),
        (
            ArmorStyle.Chainmail,
            ArmorWeightClass.Heavy,
            [
                [
                    WeenieClassName.basinetchainmail,
                    WeenieClassName.mailcoif,
                    WeenieClassName.capmetal,
                    WeenieClassName.gauntletschainmail,
                    WeenieClassName.breastplatechainmail,
                    WeenieClassName.girthchainmail,
                    WeenieClassName.pauldronschainmail,
                    WeenieClassName.bracerschainmail,
                    WeenieClassName.tassetschainmail,
                    WeenieClassName.greaveschainmail,
                    WeenieClassName.shirtchainmail,
                    WeenieClassName.hauberkchainmail,
                    WeenieClassName.sleeveschainmail,
                    WeenieClassName.leggingschainmail,
                ],
            ]
        ),
        (
            ArmorStyle.Scalemail,
            ArmorWeightClass.Heavy,
            [
                [
                    WeenieClassName.basinetscalemail,
                    WeenieClassName.coifscale,
                    WeenieClassName.gauntletsscalemail,
                    WeenieClassName.breastplatescalemail,
                    WeenieClassName.girthscalemail,
                    WeenieClassName.pauldronsscalemail,
                    WeenieClassName.bracersscalemail,
                    WeenieClassName.tassetsscalemail,
                    WeenieClassName.greavesscalemail,
                    WeenieClassName.cuirassscalemail,
                    WeenieClassName.shirtscalemail,
                    WeenieClassName.hauberkscalemail,
                    WeenieClassName.sleevesscalemail,
                    WeenieClassName.leggingsscalemail,
                ],
            ]
        ),
        (
            ArmorStyle.Platemail,
            ArmorWeightClass.Heavy,
            [
                [
                    WeenieClassName.heaume,
                    WeenieClassName.heaumenew,
                    WeenieClassName.armet,
                    WeenieClassName.gauntletsplatemail,
                    WeenieClassName.sollerets,
                    WeenieClassName.breastplateplatemail,
                    WeenieClassName.girthplatemail,
                    WeenieClassName.pauldronsplatemail,
                    WeenieClassName.vambracesplatemail,
                    WeenieClassName.tassetsplatemail,
                    WeenieClassName.greavesplatemail,
                    WeenieClassName.cuirassplatemail,
                    WeenieClassName.hauberkplatemail,
                    WeenieClassName.sleevesplatemail,
                    WeenieClassName.leggingsplatemail,
                ],
            ]
        ),
        (
            ArmorStyle.Celdon,
            ArmorWeightClass.Heavy,
            [
                [
                    WeenieClassName.breastplateceldon,
                    WeenieClassName.girthceldon,
                    WeenieClassName.sleevesceldon,
                    WeenieClassName.leggingsceldon,
                ],
            ]
        ),
        (
            ArmorStyle.Covenant,
            ArmorWeightClass.Heavy,
            [
                [
                    WeenieClassName.helmcovenant,
                    WeenieClassName.gauntletscovenant,
                    WeenieClassName.bootscovenant,
                    WeenieClassName.breastplatecovenant,
                    WeenieClassName.girthcovenant,
                    WeenieClassName.pauldronscovenant,
                    WeenieClassName.bracerscovenant,
                    WeenieClassName.tassetscovenant,
                    WeenieClassName.greavescovenant,
                ],
            ]
        ),
        (
            ArmorStyle.Nariyid,
            ArmorWeightClass.Heavy,
            [
                [
                    WeenieClassName.helmnariyid,
                    WeenieClassName.gauntletsnariyid,
                    WeenieClassName.bootsnariyid,
                    WeenieClassName.breastplatenariyid,
                    WeenieClassName.girthnariyid,
                    WeenieClassName.sleevesnariyid,
                    WeenieClassName.leggingsnariyid,
                ],
            ]
        ),
        (
            ArmorStyle.OlthoiCeldon,
            ArmorWeightClass.Heavy,
            [
                [
                    WeenieClassName.ace37197_olthoiceldonhelm,
                    WeenieClassName.ace37189_olthoiceldongauntlets,
                    WeenieClassName.ace37209_olthoiceldonsollerets,
                    WeenieClassName.ace37214_olthoiceldonbreastplate,
                    WeenieClassName.ace37192_olthoiceldongirth,
                    WeenieClassName.ace37205_olthoiceldonsleeves,
                    WeenieClassName.ace37202_olthoiceldonleggings,
                ],
            ]
        ),
        (
            ArmorStyle.OlthoiArmor,
            ArmorWeightClass.Heavy,
            [
                [
                    WeenieClassName.ace37199_olthoihelm,
                    WeenieClassName.ace37191_olthoigauntlets,
                    WeenieClassName.ace37211_olthoisollerets,
                    WeenieClassName.ace37216_olthoibreastplate,
                    WeenieClassName.ace37193_olthoigirth,
                    WeenieClassName.ace37204_olthoipauldrons,
                    WeenieClassName.ace37213_olthoibracers,
                    WeenieClassName.ace37212_olthoitassets,
                    WeenieClassName.ace37194_olthoigreaves,
                ],
            ]
        ),
        (
            ArmorStyle.Amuli,
            ArmorWeightClass.Cloth,
            [
                [WeenieClassName.coatamullian, WeenieClassName.leggingsamullian]
            ]
        ),
        (
            ArmorStyle.Chiran,
            ArmorWeightClass.Cloth,
            [
                [
                    WeenieClassName.helmchiran,
                    WeenieClassName.gauntletschiran,
                    WeenieClassName.sandalschiran,
                    WeenieClassName.coatchiran,
                    WeenieClassName.leggingschiran,
                ],
            ]
        ),
        (
            ArmorStyle.OlthoiAmuli,
            ArmorWeightClass.Cloth,
            [
                [
                    WeenieClassName.ace37196_olthoiamulihelm,
                    WeenieClassName.ace37188_olthoiamuligauntlets,
                    WeenieClassName.ace37208_olthoiamulisollerets,
                    WeenieClassName.ace37299_olthoiamulicoat,
                    WeenieClassName.ace37201_olthoiamulileggings,
                ],
            ]
        ),
    ];

    private static readonly (CoverageMask Part, string Name)[] PartNames =
    [
        (CoverageMask.Head, "head"),
        (CoverageMask.OuterwearChest, "chest"),
        (CoverageMask.OuterwearAbdomen, "abdomen"),
        (CoverageMask.OuterwearUpperArms, "upper arms"),
        (CoverageMask.OuterwearLowerArms, "lower arms"),
        (CoverageMask.Hands, "hands"),
        (CoverageMask.OuterwearUpperLegs, "upper legs"),
        (CoverageMask.OuterwearLowerLegs, "lower legs"),
        (CoverageMask.Feet, "feet"),
    ];

    private readonly List<ArmorStylePiece> pieces;
    private readonly Dictionary<uint, ArmorStylePiece> byWcid;

    public ArmorStyleCatalogue(IEnumerable<ArmorStylePiece> pieces)
    {
        this.pieces = pieces.ToList();
        byWcid = new Dictionary<uint, ArmorStylePiece>();
        foreach (var piece in this.pieces)
        {
            byWcid.TryAdd(piece.Wcid, piece);
        }
    }

    /// <summary>
    /// Builds the catalogue from the style definitions, looking up each piece's ClothingBase and ClothingPriority.
    /// Pieces missing either are left out.
    /// </summary>
    public static ArmorStyleCatalogue Build(Func<uint, (uint? ClothingBase, int? ClothingPriority)> lookup)
    {
        var pieces = new List<ArmorStylePiece>();

        foreach (var (style, weightClass, variants) in Definitions)
        {
            for (var variant = 0; variant < variants.Length; variant++)
            {
                foreach (var wcid in variants[variant])
                {
                    var (clothingBase, clothingPriority) = lookup((uint)wcid);
                    if (clothingBase == null || clothingPriority == null)
                    {
                        continue;
                    }

                    pieces.Add(
                        new ArmorStylePiece(
                            (uint)wcid,
                            style,
                            variant,
                            weightClass,
                            clothingBase.Value,
                            (CoverageMask)clothingPriority.Value
                        )
                    );
                }
            }
        }

        return new ArmorStyleCatalogue(pieces);
    }

    /// <summary>
    /// Every wcid the style definitions name, for checking the definitions.
    /// </summary>
    public static IEnumerable<uint> DefinedWcids =>
        Definitions.SelectMany(d => d.Variants).SelectMany(v => v).Select(wcid => (uint)wcid);

    public ArmorStylePiece Get(uint wcid)
    {
        return byWcid.GetValueOrDefault(wcid);
    }

    /// <summary>
    /// The piece an item's current look comes from: its own weenie's piece if it still wears that weenie's ClothingBase,
    /// otherwise the first piece of its weight class with the same ClothingBase. Null if the look is not part of a style.
    /// </summary>
    public ArmorStylePiece Identify(uint wcid, uint? clothingBase, ArmorWeightClass weightClass)
    {
        if (clothingBase is not { } look)
        {
            return null;
        }

        if (byWcid.TryGetValue(wcid, out var own) && own.ClothingBase == look)
        {
            return own;
        }

        return pieces.FirstOrDefault(p => p.ClothingBase == look && p.WeightClass == weightClass);
    }

    /// <summary>
    /// The piece of the template's style with exactly this coverage, preferring the template's own variant.
    /// Null if the style has no piece with this coverage.
    /// </summary>
    public ArmorStylePiece Find(ArmorStylePiece template, CoverageMask coverage)
    {
        var candidates = pieces.Where(p => p.Style == template.Style && p.Coverage == coverage).ToList();

        return candidates.FirstOrDefault(p => p.Variant == template.Variant) ?? candidates.FirstOrDefault();
    }

    /// <summary>
    /// Each coverage the style has a piece for, in the order the style lists them.
    /// </summary>
    public List<CoverageMask> Coverages(ArmorStyle style)
    {
        return pieces.Where(p => p.Style == style).Select(p => p.Coverage).Distinct().ToList();
    }

    /// <summary>
    /// The colors a restyled piece takes: the template's palette if the piece's clothing table has it,
    /// else the piece's own, with the icon the clothing table gives that palette.
    /// iconsByPalette is the clothing table's icon for each palette template it supports.
    /// </summary>
    public static (int? PaletteTemplate, uint Icon) ChooseColors(
        int? templatePalette,
        int? piecePalette,
        uint pieceIcon,
        IReadOnlyDictionary<uint, uint> iconsByPalette
    )
    {
        foreach (var palette in new[] { templatePalette, piecePalette })
        {
            if (palette is { } p && iconsByPalette.TryGetValue((uint)p, out var icon) && icon != 0)
            {
                return (p, icon);
            }
        }

        return (piecePalette, pieceIcon);
    }

    /// <summary>
    /// Names the body parts a coverage covers, e.g. "upper arms + lower arms".
    /// </summary>
    public static string DescribeCoverage(CoverageMask coverage)
    {
        return string.Join(" + ", PartNames.Where(p => coverage.HasFlag(p.Part)).Select(p => p.Name));
    }

    /// <summary>
    /// How many of the nine armor slots (head, chest, abdomen, upper and lower arms, hands, upper and lower legs, feet)
    /// a coverage takes up, which is how many uses restyling it costs. At least 1.
    /// </summary>
    public static int SlotCount(CoverageMask coverage)
    {
        return Math.Max(1, PartNames.Count(p => coverage.HasFlag(p.Part)));
    }

    /// <summary>
    /// The style's name as players read it, e.g. "Studded Leather".
    /// </summary>
    public static string StyleName(ArmorStyle style)
    {
        return style switch
        {
            ArmorStyle.StuddedLeather => "Studded Leather",
            ArmorStyle.OlthoiArmor => "Olthoi",
            ArmorStyle.OlthoiCeldon => "Olthoi Celdon",
            ArmorStyle.OlthoiAmuli => "Olthoi Amuli",
            ArmorStyle.OlthoiKoujia => "Olthoi Koujia",
            _ => style.ToString(),
        };
    }
}
