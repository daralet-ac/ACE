using System;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.AdminUi;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class OlthoiSupply
{
    [CommandHandler(
        "olthoisupply",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        2,
        "Sets an Olthoi North camp's supply level, and its Well / Greatly Supplied events to match.",
        "<north | south | west> <level>\n"
            + "Setting olthoi_north_camp_*_supply_level with /modifylong leaves the camp's events as they were, because the camp's "
            + "controllers only check the level when a quartermaster trade or the hourly reduction tells them to. This sets both."
    )]
    public static void HandleOlthoiSupply(Session session, params string[] parameters)
    {
        var camp = AdminUiRegions.OlthoiNorthCamps.FirstOrDefault(c =>
            c.Equals(parameters[0], StringComparison.OrdinalIgnoreCase)
        );

        if (camp == null)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "The camp is north, south or west.",
                ChatMessageType.Broadcast
            );
            return;
        }

        if (!long.TryParse(parameters[1], out var level) || level < 0)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "The level is a whole number, 0 or more.",
                ChatMessageType.Broadcast
            );
            return;
        }

        PropertyManager.ModifyLong(AdminUiRegions.SupplyProperty(camp), level);

        // the same rules as the camp's Well / Greatly Supplied controllers
        SetEvent(AdminUiRegions.WellSuppliedEvent(camp), level >= AdminUiRegions.WellSuppliedLevel);
        SetEvent(AdminUiRegions.GreatlySuppliedEvent(camp), level >= AdminUiRegions.GreatlySuppliedLevel);

        var summary =
            level >= AdminUiRegions.GreatlySuppliedLevel
                ? "Greatly Supplied"
                : level >= AdminUiRegions.WellSuppliedLevel
                    ? "Well Supplied"
                    : "not supplied";

        PlayerManager.BroadcastToAuditChannel(
            session?.Player,
            $"{session?.Player?.Name ?? "The console"} set the Olthoi North {camp} camp's supply level to {level} ({summary})."
        );

        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"The {camp} camp's supply level is now {level} ({summary}).",
            ChatMessageType.Broadcast
        );
    }

    private static void SetEvent(string name, bool on)
    {
        if (on)
        {
            EventManager.StartEvent(name, null, null);
        }
        else
        {
            EventManager.StopEvent(name, null, null);
        }
    }
}
