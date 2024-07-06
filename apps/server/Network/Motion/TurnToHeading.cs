using System.IO;

namespace ACE.Server.Network.Motion;

public class TurnToHeading
{
    public TurnToParameters TurnToParameters; // set of turning parameters

    public TurnToHeading(ACE.Server.Entity.Motion motion)
    {
        TurnToParameters = new TurnToParameters(motion);
    }
}

public static class TurnToHeadingExtensions
{
    public static void Write(this BinaryWriter writer, TurnToHeading turnTo)
    {
        writer.Write(turnTo.TurnToParameters);
    }
}
