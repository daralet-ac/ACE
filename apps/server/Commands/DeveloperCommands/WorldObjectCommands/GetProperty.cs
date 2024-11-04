using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands.WorldObjectCommands;

public class GetProperty
{
    /// <summary>
    /// Gets a property for the last appraised object
    /// </summary>
    [CommandHandler(
        "getproperty",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Gets a property for the last appraised object",
        "<property>"
    )]
    public static void HandleGetProperty(Session session, params string[] parameters)
    {
        var obj = CommandHandlerHelper.GetLastAppraisedObject(session);
        if (obj == null)
        {
            return;
        }

        if (parameters.Length < 1)
        {
            return;
        }

        var prop = parameters[0];

        var props = prop.Split('.');
        if (props.Length != 2)
        {
            session.Network.EnqueueSend(new GameMessageSystemChat($"Unknown {prop}", ChatMessageType.Broadcast));
            return;
        }

        var propType = props[0];
        var propName = props[1];

        Type pType;
        if (propType.Equals("PropertyInt", StringComparison.OrdinalIgnoreCase))
        {
            pType = typeof(PropertyInt);
        }
        else if (propType.Equals("PropertyInt64", StringComparison.OrdinalIgnoreCase))
        {
            pType = typeof(PropertyInt64);
        }
        else if (propType.Equals("PropertyBool", StringComparison.OrdinalIgnoreCase))
        {
            pType = typeof(PropertyBool);
        }
        else if (propType.Equals("PropertyFloat", StringComparison.OrdinalIgnoreCase))
        {
            pType = typeof(PropertyFloat);
        }
        else if (propType.Equals("PropertyString", StringComparison.OrdinalIgnoreCase))
        {
            pType = typeof(PropertyString);
        }
        else if (propType.Equals("PropertyInstanceId", StringComparison.OrdinalIgnoreCase))
        {
            pType = typeof(PropertyInstanceId);
        }
        else if (propType.Equals("PropertyDataId", StringComparison.OrdinalIgnoreCase))
        {
            pType = typeof(PropertyDataId);
        }
        else
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Unknown property type: {propType}", ChatMessageType.Broadcast)
            );
            return;
        }

        if (!Enum.TryParse(pType, propName, true, out var result))
        {
            session.Network.EnqueueSend(new GameMessageSystemChat($"Couldn't find {prop}", ChatMessageType.Broadcast));
            return;
        }

        var value = "";
        if (propType.Equals("PropertyInt", StringComparison.OrdinalIgnoreCase))
        {
            value = Convert.ToString(obj.GetProperty((PropertyInt)result));
        }
        else if (propType.Equals("PropertyInt64", StringComparison.OrdinalIgnoreCase))
        {
            value = Convert.ToString(obj.GetProperty((PropertyInt64)result));
        }
        else if (propType.Equals("PropertyBool", StringComparison.OrdinalIgnoreCase))
        {
            value = Convert.ToString(obj.GetProperty((PropertyBool)result));
        }
        else if (propType.Equals("PropertyFloat", StringComparison.OrdinalIgnoreCase))
        {
            value = Convert.ToString(obj.GetProperty((PropertyFloat)result));
        }
        else if (propType.Equals("PropertyString", StringComparison.OrdinalIgnoreCase))
        {
            value = Convert.ToString(obj.GetProperty((PropertyString)result));
        }
        else if (propType.Equals("PropertyInstanceId", StringComparison.OrdinalIgnoreCase))
        {
            value = Convert.ToString(obj.GetProperty((PropertyInstanceId)result));
        }
        else if (propType.Equals("PropertyDataId", StringComparison.OrdinalIgnoreCase))
        {
            value = Convert.ToString(obj.GetProperty((PropertyDataId)result));
        }

        session.Network.EnqueueSend(
            new GameMessageSystemChat($"{obj.Name} ({obj.Guid}): {prop} = {value}", ChatMessageType.Broadcast)
        );
    }
}
