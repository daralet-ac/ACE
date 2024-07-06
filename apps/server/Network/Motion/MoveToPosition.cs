using System.IO;
using ACE.Server.Network.Structure;

namespace ACE.Server.Network.Motion;

public class MoveToPosition
{
    public Origin Origin; // the location of the target
    public MoveToParameters MoveToParams; // set of movement parameters
    public float RunRate; // run speed of the moving object

    public MoveToPosition(ACE.Server.Entity.Motion motion)
    {
        Origin = new Origin(motion.Position);
        MoveToParams = motion.MoveToParameters;
        RunRate = motion.RunRate;
    }
}

public static class MoveToPositionExtensions
{
    public static void Write(this BinaryWriter writer, MoveToPosition moveTo)
    {
        writer.Write(moveTo.Origin);
        writer.Write(moveTo.MoveToParams);
        writer.Write(moveTo.RunRate);
    }
}
