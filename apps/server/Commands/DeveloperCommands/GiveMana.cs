using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class GiveMana
{
    [CommandHandler(
        "givemana",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Gives mana to the last appraised object",
        "<amount>"
    )]
    public static void HandleGiveMana(Session session, params string[] parameters)
    {
        if (parameters.Length == 0)
        {
            return;
        }

        var amount = Int32.Parse(parameters[0]);

        var obj = CommandHandlerHelper.GetLastAppraisedObject(session);
        if (obj == null)
        {
            return;
        }

        amount = Math.Min(amount, (obj.ItemMaxMana ?? 0) - (obj.ItemCurMana ?? 0));
        obj.ItemCurMana += amount;
        session.Network.EnqueueSend(
            new GameMessageSystemChat($"You give {amount} points of mana to the {obj.Name}.", ChatMessageType.Magic)
        );
    }
}
