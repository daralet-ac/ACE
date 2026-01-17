using System;
using System.Collections.Generic;

namespace ACE.Server.WorldObjects.Patrol;

public sealed class PatrolPath
{
    private readonly List<PatrolOffset> _offsets = new();

    public int Count => _offsets.Count;
    public PatrolOffset this[int idx] => _offsets[idx];

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
            var xyz = part.Trim().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (xyz.Length < 2)
            {
                continue;
            }

            if (!float.TryParse(xyz[0].Trim(), out var dx))
            {
                continue;
            }

            if (!float.TryParse(xyz[1].Trim(), out var dy))
            {
                continue;
            }

            var dz = 0f;
            if (xyz.Length >= 3)
            {
                float.TryParse(xyz[2].Trim(), out dz);
            }

            path._offsets.Add(new PatrolOffset(dx, dy, dz));
        }

        if (path.Count > 0)
        {
            return path;
        }

        return null;
    }
}
