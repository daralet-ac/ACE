using System.Numerics;

namespace ACE.Server.WorldObjects.Patrol;

public readonly struct PatrolOffset
{
    public readonly float Dx;
    public readonly float Dy;
    public readonly float Dz;

    public PatrolOffset(float dx, float dy, float dz = 0f)
    {
        Dx = dx;
        Dy = dy;
        Dz = dz;
    }

    public Vector3 ToVector3() => new Vector3(Dx, Dy, Dz);
}
