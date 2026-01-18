using System;
using System.Collections.Generic;

namespace ACE.Server.WorldObjects.Patrol;

public sealed class PatrolPath
{
    private readonly List<PatrolOffset> _offsets = new();

    public int Count => _offsets.Count;
    public PatrolOffset this[int idx] => _offsets[idx];

    /// <summary>
    /// 2D-only format:
    ///   dx,dy
    ///   dx,dy,pauseSeconds
    /// Waypoints separated by ';'
    /// </summary>
    public static PatrolPath Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var path = new PatrolPath();

        var parts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var tokens = part.Trim().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 2 && tokens.Length != 3)
            {
                continue;
            }

            if (!float.TryParse(tokens[0].Trim(), out var dx))
            {
                continue;
            }

            if (!float.TryParse(tokens[1].Trim(), out var dy))
            {
                continue;
            }

            float? pauseSeconds = null;

            if (tokens.Length == 3)
            {
                if (float.TryParse(tokens[2].Trim(), out var p))
                {
                    // Negative pauses don't make sense; clamp at 0.
                    if (p < 0f)
                    {
                        p = 0f;
                    }

                    pauseSeconds = p;
                }
            }

            path._offsets.Add(new PatrolOffset(dx, dy, pauseSeconds));
        }

        if (path.Count > 0)
        {
            return path;
        }

        return null;
    }
}
