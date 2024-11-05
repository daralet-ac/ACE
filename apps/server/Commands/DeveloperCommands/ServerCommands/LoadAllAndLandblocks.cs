using System.Threading.Tasks;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.ServerCommands;

public class LoadAllAndLandblocks
{
    /// <summary>
    /// This is a VERY crude test. It should never be used on a live server.
    /// There isn't really much point to this command other than making sure landblocks can load and are semi-efficient.
    /// </summary>
    [CommandHandler(
        "loadalllandblocks",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        "Loads all Landblocks. This is VERY crude. Do NOT use it on a live server!!! It will likely crash the server.  Landblock resources will be loaded async and will continue to do work even after all landblocks have been loaded."
    )]
    public static void HandleLoadAllLandblocks(Session session, params string[] parameters)
    {
        CommandHandlerHelper.WriteOutputInfo(
            session,
            "Loading landblocks. This will likely crash the server. Landblock resources will be loaded async and will continue to do work even after all landblocks have been loaded."
        );

        Task.Run(() =>
        {
            for (var x = 0; x <= 0xFE; x++)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Loading landblocks, x = 0x{x:X2} of 0xFE....");

                for (var y = 0; y <= 0xFE; y++)
                {
                    var blockid = new LandblockId((byte)x, (byte)y);
                    LandblockManager.GetLandblock(blockid, false, false);
                }
            }

            CommandHandlerHelper.WriteOutputInfo(
                session,
                "Loading landblocks completed. Async landblock resources are likely still loading..."
            );
        });
    }
}
