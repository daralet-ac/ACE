using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands.WorldObjectCommands;

public class SetProperty
{
    /// <summary>
    /// Sets a property for the last appraised object
    /// </summary>
    [CommandHandler(
        "setproperty",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        2,
        "Sets a property for the last appraised object",
        "<property> <value>"
    )]
    public static void HandleSetProperty(Session session, params string[] parameters)
    {
        var obj = CommandHandlerHelper.GetLastAppraisedObject(session);
        if (obj == null)
        {
            return;
        }

        if (parameters.Length < 2)
        {
            return;
        }

        var prop = parameters[0];
        var value = parameters[1];

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

        if (value == "null")
        {
            if (propType.Equals("PropertyInt", StringComparison.OrdinalIgnoreCase))
            {
                obj.RemoveProperty((PropertyInt)result);
            }
            else if (propType.Equals("PropertyInt64", StringComparison.OrdinalIgnoreCase))
            {
                obj.RemoveProperty((PropertyInt64)result);
            }
            else if (propType.Equals("PropertyBool", StringComparison.OrdinalIgnoreCase))
            {
                obj.RemoveProperty((PropertyBool)result);
            }
            else if (propType.Equals("PropertyFloat", StringComparison.OrdinalIgnoreCase))
            {
                obj.RemoveProperty((PropertyFloat)result);
            }
            else if (propType.Equals("PropertyString", StringComparison.OrdinalIgnoreCase))
            {
                obj.RemoveProperty((PropertyString)result);
            }
            else if (propType.Equals("PropertyInstanceId", StringComparison.OrdinalIgnoreCase))
            {
                obj.RemoveProperty((PropertyInstanceId)result);
            }
            else if (propType.Equals("PropertyDataId", StringComparison.OrdinalIgnoreCase))
            {
                obj.RemoveProperty((PropertyDataId)result);
            }
        }
        else
        {
            try
            {
                if (propType.Equals("PropertyInt", StringComparison.OrdinalIgnoreCase))
                {
                    var intValue = Convert.ToInt32(
                        value,
                        value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 16 : 10
                    );

                    session.Player.UpdateProperty(obj, (PropertyInt)result, intValue, true);
                }
                else if (propType.Equals("PropertyInt64", StringComparison.OrdinalIgnoreCase))
                {
                    var int64Value = Convert.ToInt64(
                        value,
                        value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 16 : 10
                    );

                    session.Player.UpdateProperty(obj, (PropertyInt64)result, int64Value, true);
                }
                else if (propType.Equals("PropertyBool", StringComparison.OrdinalIgnoreCase))
                {
                    var boolValue = Convert.ToBoolean(value);

                    session.Player.UpdateProperty(obj, (PropertyBool)result, boolValue, true);
                }
                else if (propType.Equals("PropertyFloat", StringComparison.OrdinalIgnoreCase))
                {
                    var floatValue = Convert.ToDouble(value);

                    session.Player.UpdateProperty(obj, (PropertyFloat)result, floatValue, true);
                }
                else if (propType.Equals("PropertyString", StringComparison.OrdinalIgnoreCase))
                {
                    session.Player.UpdateProperty(obj, (PropertyString)result, value, true);
                }
                else if (propType.Equals("PropertyInstanceId", StringComparison.OrdinalIgnoreCase))
                {
                    var iidValue = Convert.ToUInt32(
                        value,
                        value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 16 : 10
                    );

                    session.Player.UpdateProperty(obj, (PropertyInstanceId)result, iidValue, true);
                }
                else if (propType.Equals("PropertyDataId", StringComparison.OrdinalIgnoreCase))
                {
                    var didValue = Convert.ToUInt32(
                        value,
                        value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 16 : 10
                    );

                    session.Player.UpdateProperty(obj, (PropertyDataId)result, didValue, true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }
        session.Network.EnqueueSend(
            new GameMessageSystemChat($"{obj.Name} ({obj.Guid}): {prop} = {value}", ChatMessageType.Broadcast)
        );
        PlayerManager.BroadcastToAuditChannel(
            session.Player,
            $"{session.Player.Name} changed a property for {obj.Name} ({obj.Guid}): {prop} = {value}"
        );
    }
}
