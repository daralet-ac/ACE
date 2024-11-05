using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.EnvoyCommands;

public class Regen
{
    // regen
    [CommandHandler(
        "regen",
        AccessLevel.Envoy,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Sends the selected generator a regeneration message.",
        ""
    )]
    public static void HandleRegen(Session session, params string[] parameters)
    {
        // @regen - Sends the selected generator a regeneration message.

        var objectId = new ObjectGuid();

        if (
            session.Player.HealthQueryTarget.HasValue
            || session.Player.ManaQueryTarget.HasValue
            || session.Player.CurrentAppraisalTarget.HasValue
        )
        {
            if (session.Player.HealthQueryTarget.HasValue)
            {
                objectId = new ObjectGuid((uint)session.Player.HealthQueryTarget);
            }
            else if (session.Player.ManaQueryTarget.HasValue)
            {
                objectId = new ObjectGuid((uint)session.Player.ManaQueryTarget);
            }
            else
            {
                if (session.Player.CurrentAppraisalTarget != null)
                {
                    objectId = new ObjectGuid((uint)session.Player.CurrentAppraisalTarget);
                }
            }

            var wo = session.Player.CurrentLandblock?.GetObject(objectId);

            if (objectId.IsPlayer())
            {
                return;
            }

            if (wo != null && wo.IsGenerator)
            {
                if (wo is Container container)
                {
                    container.Reset();
                }

                wo.ResetGenerator();
                wo.GeneratorEnteredWorld = false;
                wo.GeneratorRegeneration(Common.Time.GetUnixTime());
            }
        }
    }
}
