using System.IO;
using ACE.Entity;
using ACE.Server.Network.Structure;

namespace ACE.Server.Network.Motion;

public class MoveToObject
{
    public ObjectGuid Target; // target guid to move to
    public Origin Origin; // the location of the target
    public MoveToParameters MoveToParams; // set of movement parameters
    public float RunRate; // run speed of the moving object

    public MoveToObject(ACE.Server.Entity.Motion motion)
    {
        Target = motion.TargetGuid;
        Origin = new Origin(motion.Position);
        MoveToParams = motion.MoveToParameters;
        RunRate = motion.RunRate;
    }
}

public static class MoveToObjectExtensions
{
    public static void Write(this BinaryWriter writer, MoveToObject moveTo)
    {
        writer.WriteGuid(moveTo.Target);
        writer.Write(moveTo.Origin);
        writer.Write(moveTo.MoveToParams);
        writer.Write(moveTo.RunRate);
    }
}
