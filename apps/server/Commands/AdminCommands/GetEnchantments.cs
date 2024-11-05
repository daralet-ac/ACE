using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;

namespace ACE.Server.Commands.AdminCommands;

public class GetEnchantments
{
    [CommandHandler(
        "getenchantments",
        AccessLevel.Admin,
        CommandHandlerFlag.RequiresWorld,
        "Shows the enchantments for the last appraised item"
    )]
    public static void HandleGetEnchantments(Session session, params string[] parameters)
    {
        var item = CommandHandlerHelper.GetLastAppraisedObject(session);
        if (item == null)
        {
            return;
        }

        var enchantments = item.Biota.PropertiesEnchantmentRegistry.GetEnchantmentsTopLayer(
            item.BiotaDatabaseLock,
            SpellSet.SetSpells
        );

        foreach (var enchantment in enchantments)
        {
            var e = new Enchantment(item, enchantment);
            var info = e.GetInfo();
            session.Network.EnqueueSend(new GameMessageSystemChat(info, ChatMessageType.Broadcast));
        }
    }
}
