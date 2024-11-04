using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Physics.Managers;
using Serilog;

namespace ACE.Server.Commands.DeveloperCommands;

public class AuditObjectMaint
{
    private static readonly ILogger _log = Log.ForContext(typeof(AuditObjectMaint));

    [CommandHandler(
        "auditobjectmaint",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Iterates over physics objects to find leaks"
    )]
    public static void HandleAuditObjectMaint(Session session, params string[] parameters)
    {
        var serverObjects = ServerObjectManager.ServerObjects.Keys.ToHashSet();

        var objectTableErrors = 0;
        var visibleObjectTableErrors = 0;
        var voyeurTableErrors = 0;

        foreach (var value in ServerObjectManager.ServerObjects.Values)
        {
            {
                var kvps = value.ObjMaint.GetKnownObjectsWhere(kvp => !serverObjects.Contains(kvp.Key));
                foreach (var kvp in kvps)
                {
                    if (value.ObjMaint.RemoveKnownObject(kvp.Value, false))
                    {
                        _log.Debug(
                            $"AuditObjectMaint removed 0x{kvp.Value.ID:X8}:{kvp.Value.Name} (IsDestroyed:{kvp.Value.WeenieObj?.WorldObject?.IsDestroyed}, Position:{kvp.Value.Position}) from 0x{value.ID:X8}:{value.Name} (IsDestroyed:{value.WeenieObj?.WorldObject?.IsDestroyed}, Position:{value.Position}) [ObjectTable]"
                        );
                        objectTableErrors++;
                    }
                }
            }

            {
                var kvps = value.ObjMaint.GetVisibleObjectsWhere(kvp => !serverObjects.Contains(kvp.Key));
                foreach (var kvp in kvps)
                {
                    if (value.ObjMaint.RemoveVisibleObject(kvp.Value, false))
                    {
                        _log.Debug(
                            $"AuditObjectMaint removed 0x{kvp.Value.ID:X8}:{kvp.Value.Name} (IsDestroyed:{kvp.Value.WeenieObj?.WorldObject?.IsDestroyed}, Position:{kvp.Value.Position}) from 0x{value.ID:X8}:{value.Name} (IsDestroyed:{value.WeenieObj?.WorldObject?.IsDestroyed}, Position:{value.Position}) [VisibleObjectTable]"
                        );
                        visibleObjectTableErrors++;
                    }
                }
            }

            {
                var kvps = value.ObjMaint.GetKnownPlayersWhere(kvp => !serverObjects.Contains(kvp.Key));
                foreach (var kvp in kvps)
                {
                    if (value.ObjMaint.RemoveKnownPlayer(kvp.Value))
                    {
                        _log.Debug(
                            $"AuditObjectMaint removed 0x{kvp.Value.ID:X8}:{kvp.Value.Name} (IsDestroyed:{kvp.Value.WeenieObj?.WorldObject?.IsDestroyed}, Position:{kvp.Value.Position}) from 0x{value.ID:X8}:{value.Name} (IsDestroyed:{value.WeenieObj?.WorldObject?.IsDestroyed}, Position:{value.Position}) [VoyeurTable]"
                        );
                        voyeurTableErrors++;
                    }
                }
            }
        }

        if (session != null)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Physics ObjMaint Audit Completed. Errors - objectTable: {objectTableErrors}, visibleObjectTable: {visibleObjectTableErrors}, voyeurTable: {voyeurTableErrors}"
            );
        }

        _log.Information(
            "Physics ObjMaint Audit Completed. Errors - objectTable: {ObjectTableErrors}, visibleObjectTable: {VisibleObjectTableErrors}, voyeurTable: {VoyeurTableErrors}",
            objectTableErrors,
            visibleObjectTableErrors,
            voyeurTableErrors
        );
    }
}
