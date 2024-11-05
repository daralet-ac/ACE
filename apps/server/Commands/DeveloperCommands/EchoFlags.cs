using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class EchoFlags
{
    /// <summary>
    /// echoflags [flagtype] [int]
    /// </summary>
    [CommandHandler(
        "echoflags",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        2,
        "Echo flags back to you",
        "[type to test] [int]\n"
    )]
    public static void HandleDebugEchoFlags(Session session, params string[] parameters)
    {
        try
        {
            if (parameters?.Length == 2)
            {
                string debugOutput;

                switch (parameters[0].ToLower())
                {
                    case "descriptionflags":
                        var objectDescFlag = (ObjectDescriptionFlag)Convert.ToUInt32(parameters[1]);

                        debugOutput =
                            $"{objectDescFlag.GetType().Name} = {objectDescFlag.ToString()}"
                            + " ("
                            + (uint)objectDescFlag
                            + ")";
                        break;
                    case "weenieflags":
                        var weenieHdr = (WeenieHeaderFlag)Convert.ToUInt32(parameters[1]);

                        debugOutput =
                            $"{weenieHdr.GetType().Name} = {weenieHdr.ToString()}" + " (" + (uint)weenieHdr + ")";
                        break;
                    case "weenieflags2":
                        var weenieHdr2 = (WeenieHeaderFlag2)Convert.ToUInt32(parameters[1]);

                        debugOutput =
                            $"{weenieHdr2.GetType().Name} = {weenieHdr2.ToString()}" + " (" + (uint)weenieHdr2 + ")";
                        break;
                    case "positionflag":
                        var posFlag = (PositionFlags)Convert.ToUInt32(parameters[1]);

                        debugOutput = $"{posFlag.GetType().Name} = {posFlag.ToString()}" + " (" + (uint)posFlag + ")";
                        break;
                    case "type":
                        var objectType = (ItemType)Convert.ToUInt32(parameters[1]);

                        debugOutput =
                            $"{objectType.GetType().Name} = {objectType.ToString()}" + " (" + (uint)objectType + ")";
                        break;
                    case "containertype":
                        var contType = (ContainerType)Convert.ToUInt32(parameters[1]);

                        debugOutput =
                            $"{contType.GetType().Name} = {contType.ToString()}" + " (" + (uint)contType + ")";
                        break;
                    case "usable":
                        var usableType = (Usable)Convert.ToInt64(parameters[1]);

                        debugOutput =
                            $"{usableType.GetType().Name} = {usableType.ToString()}" + " (" + (Int64)usableType + ")";
                        break;
                    case "radarbehavior":
                        var radarBeh = (RadarBehavior)Convert.ToUInt32(parameters[1]);

                        debugOutput =
                            $"{radarBeh.GetType().Name} = {radarBeh.ToString()}" + " (" + (uint)radarBeh + ")";
                        break;
                    case "physicsdescriptionflags":
                        var physicsDescFlag = (PhysicsDescriptionFlag)Convert.ToUInt32(parameters[1]);

                        debugOutput =
                            $"{physicsDescFlag.GetType().Name} = {physicsDescFlag.ToString()}"
                            + " ("
                            + (uint)physicsDescFlag
                            + ")";
                        break;
                    case "physicsstate":
                        var physState = (PhysicsState)Convert.ToUInt32(parameters[1]);

                        debugOutput =
                            $"{physState.GetType().Name} = {physState.ToString()}" + " (" + (uint)physState + ")";
                        break;
                    case "validlocations":
                        var locFlags = (EquipMask)Convert.ToUInt32(parameters[1]);

                        debugOutput =
                            $"{locFlags.GetType().Name} = {locFlags.ToString()}" + " (" + (uint)locFlags + ")";
                        break;
                    case "currentwieldedlocation":
                        var locFlags2 = (EquipMask)Convert.ToUInt32(parameters[1]);

                        debugOutput =
                            $"{locFlags2.GetType().Name} = {locFlags2.ToString()}" + " (" + (uint)locFlags2 + ")";
                        break;
                    case "priority":
                        var covMask = (CoverageMask)Convert.ToUInt32(parameters[1]);

                        debugOutput = $"{covMask.GetType().Name} = {covMask.ToString()}" + " (" + (uint)covMask + ")";
                        break;
                    case "radarcolor":
                        var radarBlipColor = (RadarColor)Convert.ToUInt32(parameters[1]);

                        debugOutput =
                            $"{radarBlipColor.GetType().Name} = {radarBlipColor.ToString()}"
                            + " ("
                            + (uint)radarBlipColor
                            + ")";
                        break;
                    default:
                        debugOutput = "No valid type to test";
                        break;
                }

                CommandHandlerHelper.WriteOutputInfo(session, debugOutput);
            }
        }
        catch (Exception)
        {
            var debugOutput = "Exception Error, check input and try again";

            CommandHandlerHelper.WriteOutputInfo(session, debugOutput);
        }
    }
}
