using System.Linq;
using ACE.Database;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using static ACE.Server.Commands.DeveloperCommands.ContentCommands.ContentCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands.ContentCommands;

public class InstanceRemove
{
    [CommandHandler(
        "removeinst",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Removes the last appraised object from the current landblock instances"
    )]
    public static void HandleRemoveInst(Session session, params string[] parameters)
    {
        RemoveInstance(session);
    }

    public static void RemoveInstance(Session session, bool confirmed = false)
    {
        var wo = CommandHandlerHelper.GetLastAppraisedObject(session);

        if (wo?.Location == null)
        {
            return;
        }

        var landblock = (ushort)wo.Location.Landblock;

        // if generator child, try getting the "real" guid
        var guid = wo.Guid.Full;
        if (wo.Generator != null)
        {
            var staticGuid = wo.Generator.GetStaticGuid(guid);
            if (staticGuid != null)
            {
                guid = staticGuid.Value;
            }
        }

        var instances = DatabaseManager.World.GetCachedInstancesByLandblock(landblock);

        var instance = instances.FirstOrDefault(i => i.Guid == guid);

        if (instance == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"Couldn't find landblock_instance for {wo.WeenieClassId} - {wo.Name} (0x{guid:X8})",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        var numChilds = instance.LandblockInstanceLink.Count;

        if (numChilds > 0 && !confirmed)
        {
            // get total numChilds iteratively
            numChilds = 0;
            foreach (var link in instance.LandblockInstanceLink)
            {
                numChilds += GetNumChilds(session, link, instances);
            }

            // require confirmation for parent objects
            var msg =
                $"Are you sure you want to delete this parent object, and {numChilds} child object{(numChilds != 1 ? "s" : "")}?";
            if (
                !session.Player.ConfirmationManager.EnqueueSend(
                    new Confirmation_Custom(session.Player.Guid, () => RemoveInstance(session, true)),
                    msg
                )
            )
            {
                session.Player.SendWeenieError(WeenieError.ConfirmationInProgress);
            }

            return;
        }

        if (instance.IsLinkChild)
        {
            LandblockInstanceLink link = null;

            foreach (var parent in instances.Where(i => i.LandblockInstanceLink.Count > 0))
            {
                link = parent.LandblockInstanceLink.FirstOrDefault(i => i.ChildGuid == instance.Guid);

                if (link != null)
                {
                    parent.LandblockInstanceLink.Remove(link);
                    break;
                }
            }
            if (link == null)
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Couldn't find parent link for child {wo.WeenieClassId} - {wo.Name} (0x{guid:X8})",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }
        }

        wo.DeleteObject();

        foreach (var link in instance.LandblockInstanceLink)
        {
            RemoveChild(session, link, instances);
        }

        instances.Remove(instance);

        SyncInstances(session, landblock, instances);

        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Removed {(instance.IsLinkChild ? "child " : "")}{wo.WeenieClassId} - {wo.Name} (0x{guid:X8}) from landblock instances",
                ChatMessageType.Broadcast
            )
        );
    }
}
