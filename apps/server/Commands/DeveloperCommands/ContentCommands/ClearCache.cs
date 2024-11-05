using System;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands.ContentCommands;

public class ClearCache
{
    [CommandHandler(
        "clearcache",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        "Clears the various database caches. This enables live editing of the database information"
    )]
    public static void HandleClearCache(Session session, params string[] parameters)
    {
        var mode = ContentCommandUtilities.CacheType.All;
        if (parameters.Length > 0)
        {
            if (parameters[0].Contains("landblock", StringComparison.OrdinalIgnoreCase))
            {
                mode = ContentCommandUtilities.CacheType.Landblock;
            }

            if (parameters[0].Contains("recipe", StringComparison.OrdinalIgnoreCase))
            {
                mode = ContentCommandUtilities.CacheType.Recipe;
            }

            if (parameters[0].Contains("spell", StringComparison.OrdinalIgnoreCase))
            {
                mode = ContentCommandUtilities.CacheType.Spell;
            }

            if (parameters[0].Contains("weenie", StringComparison.OrdinalIgnoreCase))
            {
                mode = ContentCommandUtilities.CacheType.Weenie;
            }

            if (parameters[0].Contains("wield", StringComparison.OrdinalIgnoreCase))
            {
                mode = ContentCommandUtilities.CacheType.WieldedTreasure;
            }
        }

        if (mode.HasFlag(ContentCommandUtilities.CacheType.Landblock))
        {
            CommandHandlerHelper.WriteOutputInfo(session, "Clearing landblock instance cache");
            DatabaseManager.World.ClearCachedLandblockInstances();
        }

        if (mode.HasFlag(ContentCommandUtilities.CacheType.Recipe))
        {
            CommandHandlerHelper.WriteOutputInfo(session, "Clearing recipe cache");
            DatabaseManager.World.ClearCookbookCache();
        }

        if (mode.HasFlag(ContentCommandUtilities.CacheType.Spell))
        {
            CommandHandlerHelper.WriteOutputInfo(session, "Clearing spell cache");
            DatabaseManager.World.ClearSpellCache();
            WorldObject.ClearSpellCache();
        }

        if (mode.HasFlag(ContentCommandUtilities.CacheType.Weenie))
        {
            CommandHandlerHelper.WriteOutputInfo(session, "Clearing weenie cache");
            DatabaseManager.World.ClearWeenieCache();
        }

        if (mode.HasFlag(ContentCommandUtilities.CacheType.WieldedTreasure))
        {
            CommandHandlerHelper.WriteOutputInfo(session, "Clearing wielded treasure cache");
            DatabaseManager.World.ClearWieldedTreasureCache();
        }
    }
}
