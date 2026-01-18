namespace ACE.Server.WorldObjects.Patrol;

public readonly struct PatrolOffset
{
    public readonly float Dx;
    public readonly float Dy;

    /// <summary>
    /// Optional fixed pause override (seconds) applied when arriving at this waypoint.
    /// If null, PatrolComponent uses the weenie's default pause range (if configured),
    /// otherwise no pause.
    /// </summary>
    public readonly float? PauseSeconds;

    public PatrolOffset(float dx, float dy, float? pauseSeconds = null)
    {
        Dx = dx;
        Dy = dy;
        PauseSeconds = pauseSeconds;
    }
}
