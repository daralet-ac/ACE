using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Scribing phase of Scroll Writing: appending spell components onto a Blank Scroll, one click at a time.
/// Validation of the resulting formula is deferred entirely to <see cref="ScrollFinalizer"/> — any component
/// can be scribed in any order here. Tapers are rejected outright: Scroll Writing ignores tapers entirely
/// (see SpellFormulaMatcher), so scribing one would only ever be a wasted step.
/// </summary>
public static class ScrollWritingService
{
    public static void TryScribeComponent(Player player, BlankScroll scroll, WorldObject target)
    {
        if (player.IsBusy)
        {
            player.SendUseDoneEvent(WeenieError.YoureTooBusy);
            return;
        }

        if (!ScribingTableService.IsNearActiveTable(player))
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat("You must be near a summoned Scribing Table to do this.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        if (!RecipeManager.VerifyUse(player, scroll, target, true))
        {
            player.SendTransientError(
                "Either you or one of the items involved does not pass the requirements for this craft interaction."
            );
            player.SendUseDoneEvent();
            return;
        }

        if (!SpellComponent.IsValid(target.WeenieClassId))
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat($"The {target.NameWithMaterial} is not a spell component.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        var componentId = SpellComponent.SpellComponentWCIDs[target.WeenieClassId];

        if (SpellFormula.IsTaper(componentId))
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "Tapers are not needed for Scroll Writing.",
                    ChatMessageType.Craft
                )
            );
            player.SendUseDoneEvent();
            return;
        }

        var maxComponents = PropertyManager.GetLong("scroll_writing_max_components").Item;

        var scribed = scroll.ScribedComponents;

        if (scribed.Count >= maxComponents)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"The {scroll.NameWithMaterial} cannot hold any more components.",
                    ChatMessageType.Craft
                )
            );
            player.SendUseDoneEvent();
            return;
        }

        scribed.Add(componentId);
        scroll.ScribedComponents = scribed;

        player.EnqueueBroadcast(new GameMessageUpdateObject(scroll));

        player.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"You scribe {target.NameWithMaterial} onto the {scroll.NameWithMaterial}. ({scribed.Count} component{(scribed.Count == 1 ? "" : "s")} scribed)",
                ChatMessageType.Craft
            )
        );

        player.SendUseDoneEvent();
    }
}
