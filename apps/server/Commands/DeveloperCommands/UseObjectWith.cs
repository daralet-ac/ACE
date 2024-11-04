using System.Globalization;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class UseObjectWith
{
    [CommandHandler(
        "usewith",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Uses specified object on last appraised object",
        "guid"
    )]
    public static void HandleUseWithTarget(Session session, params string[] parameters)
    {
        uint guid;
        if (parameters[0].StartsWith("0x"))
        {
            var hex = parameters[0][2..];
            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out guid))
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Invalid guid {parameters[0]}");
                return;
            }
        }
        else if (!uint.TryParse(parameters[0], out guid))
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Invalid guid {parameters[0]}");
            return;
        }

        //var spell = new Spell(spellId);

        //if (spell.NotFound)
        //{
        //    CommandHandlerHelper.WriteOutputInfo(session, $"Spell {spellId} not found");
        //    return;
        //}

        var target = CommandHandlerHelper.GetLastAppraisedObject(session);

        if (target == null)
        {
            return;
        }

        //session.Player.TryCastSpell(spell, target, tryResist: false);

        session.Player.HandleActionUseWithTarget(guid, target.Guid.Full);
    }
}
