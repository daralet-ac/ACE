using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Serilog;

namespace ACE.Server.Entity;

/// <summary>
/// How fast rested experience builds up at each level (rested_xp_rate_curve): pieces of "level:start:less", in percent of
/// the level's cost an hour. Each piece is for the levels above its own, up to the next piece's: start, less "less" for
/// each level past the piece's own. The default, "0:10:0, 10:10:0.225, 50:1:0.02", is 10% to level 10, then 10% less
/// 0.225% for each level past 10 (9.775% at 11, 5.5% at 30, 1% at 50), then 1% less 0.02% for each level past 50 (0.98%
/// at 51, nothing from 100): a line from 10% at level 10 to 1% at 50, and another from there to nothing at 100.
/// </summary>
public sealed class RestedXpCurve
{
    public const string Default = "0:10:0, 10:10:0.225, 50:1:0.02";

    private static readonly ILogger _log = Log.ForContext(typeof(RestedXpCurve));

    /// <summary>
    /// The last property text read and its curve, swapped as one: heartbeats on every landblock's thread read it, and the
    /// text is parsed only when it changes.
    /// </summary>
    private static RestedXpCurve cached;

    /// <summary>The pieces, by the level each comes after.</summary>
    public IReadOnlyList<Piece> Pieces { get; }

    private readonly string text;

    /// <param name="AboveLevel">The piece is for levels above this one</param>
    /// <param name="StartPercent">An hour's rest the piece starts from, in percent of the level's cost</param>
    /// <param name="LessPercentPerLevel">What each level past AboveLevel takes off it, in percent</param>
    public readonly record struct Piece(int AboveLevel, double StartPercent, double LessPercentPerLevel);

    private RestedXpCurve(string text, IReadOnlyList<Piece> pieces)
    {
        this.text = text;
        Pieces = pieces;
    }

    /// <summary>The curve rested_xp_rate_curve's text says, or the default when it can't be read (logged).</summary>
    public static RestedXpCurve FromText(string text)
    {
        var last = cached;

        if (last != null && last.text == text)
        {
            return last;
        }

        var pieces = Parse(text);

        if (pieces == null)
        {
            _log.Warning(
                "[RESTED] rested_xp_rate_curve \"{Curve}\" can't be read, so the default \"{Default}\" is used. "
                    + "It's pieces of level:start:less, in percent, such as \"0:10:0, 10:10:0.225\"",
                text,
                Default
            );
            pieces = Parse(Default);
        }

        var curve = new RestedXpCurve(text, pieces);
        cached = curve;
        return curve;
    }

    /// <summary>
    /// The pieces in a curve's text, by level, or null when it can't be read: each piece "level:start" or
    /// "level:start:less", split by commas or semicolons, no level twice, nothing below 0.
    /// </summary>
    public static List<Piece> Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var pieces = new List<Piece>();

        var parts = text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var numbers = part.Split(':', StringSplitOptions.TrimEntries);

            if (numbers.Length is < 2 or > 3)
            {
                return null;
            }

            if (
                !int.TryParse(numbers[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var level)
                || !double.TryParse(numbers[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var start)
            )
            {
                return null;
            }

            var less = 0.0;

            if (
                numbers.Length == 3
                && !double.TryParse(numbers[2], NumberStyles.Float, CultureInfo.InvariantCulture, out less)
            )
            {
                return null;
            }

            if (level < 0 || start < 0 || less < 0 || double.IsNaN(start) || double.IsNaN(less))
            {
                return null;
            }

            pieces.Add(new Piece(level, start, less));
        }

        if (pieces.Count == 0 || pieces.Select(p => p.AboveLevel).Distinct().Count() != pieces.Count)
        {
            return null;
        }

        return pieces.OrderBy(p => p.AboveLevel).ToList();
    }

    /// <summary>
    /// An hour's rest at this level, as a fraction of the level's cost (0.10 for 10%): the last piece whose level it's
    /// above, or the first piece's start for a level at or below every piece's. Never below 0.
    /// </summary>
    public double LevelsPerHour(int level)
    {
        var piece = Pieces[0];
        var found = false;

        foreach (var candidate in Pieces)
        {
            if (level > candidate.AboveLevel)
            {
                piece = candidate;
                found = true;
            }
        }

        var percent = found
            ? piece.StartPercent - piece.LessPercentPerLevel * (level - piece.AboveLevel)
            : piece.StartPercent;

        // 1 less 50 times 0.02 needn't come to exactly 0 in floating point, and level 100 must rest no more
        return percent < 1e-7 ? 0 : percent / 100;
    }
}
