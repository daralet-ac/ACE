using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects.Entity;

namespace ACE.Server.Commands.DeveloperCommands;

public class SetVital
{
    [CommandHandler(
        "setvital",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        2,
        "Sets the specified vital to a specified value",
        "Usage: @setvital <vital> <value>\n"
            + "<vital> is one of the following strings:\n"
            + "    health, hp\n"
            + "    stamina, stam, sp\n"
            + "    mana, mp\n"
            + "<value> is an integral value [0-9]+, or a relative value [-+][0-9]+"
    )]
    public static void HandleSetVital(Session session, params string[] parameters)
    {
        var paramVital = parameters[0].ToLower();
        var paramValue = parameters[1];

        var relValue = paramValue[0] == '+' || paramValue[0] == '-';

        if (!int.TryParse(paramValue, out var value))
        {
            ChatPacket.SendServerMessage(session, "setvital Error: Invalid set value", ChatMessageType.Broadcast);
            return;
        }

        // Parse args...
        CreatureVital vital;

        if (paramVital == "health" || paramVital == "hp")
        {
            vital = session.Player.Health;
        }
        else if (paramVital == "stamina" || paramVital == "stam" || paramVital == "sp")
        {
            vital = session.Player.Stamina;
        }
        else if (paramVital == "mana" || paramVital == "mp")
        {
            vital = session.Player.Mana;
        }
        else
        {
            ChatPacket.SendServerMessage(session, "setvital Error: Invalid vital", ChatMessageType.Broadcast);
            return;
        }

        var delta = 0;

        if (!relValue)
        {
            delta = session.Player.UpdateVital(vital, (uint)value);
        }
        else
        {
            delta = session.Player.UpdateVitalDelta(vital, value);
        }

        if (vital == session.Player.Health)
        {
            if (delta > 0)
            {
                session.Player.DamageHistory.OnHeal((uint)delta);
            }
            else
            {
                session.Player.DamageHistory.Add(session.Player, DamageType.Health, (uint)-delta);
            }
        }
    }
}
