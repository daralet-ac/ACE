using System.IO;
using ACE.Entity.Enum;

namespace ACE.Server.Network.Motion;

public class TurnToParameters
{
    public MovementParams MovementParams;
    public float Speed; // speed of the turn
    public float DesiredHeading; // the angle to turn to

    public TurnToParameters(ACE.Server.Entity.Motion motion)
    {
        MovementParams = motion.MoveToParameters.MovementParameters;
        Speed = motion.MoveToParameters.Speed;
        DesiredHeading = motion.DesiredHeading;
    }
}

public static class TurnToParametersExtensions
{
    public static void Write(this BinaryWriter writer, TurnToParameters turnTo)
    {
        writer.Write((uint)turnTo.MovementParams);
        writer.Write(turnTo.Speed);
        writer.Write(turnTo.DesiredHeading);
    }
}
