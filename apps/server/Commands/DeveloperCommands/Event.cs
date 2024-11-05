using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class Event
{
    // event
    [CommandHandler(
        "event",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        2,
        "Maniuplates the state of an event",
        "[ start | stop | disable | enable | clear | status ] (name)\n"
            + "@event clear < name > - clears event with name <name> or all events if you put in 'all' (All clears registered generators, <name> does not)\n"
            + "@event status <eventSubstring> - get the status of all registered events or get all of the registered events that have <eventSubstring> in the name."
    )]
    public static void HandleEvent(Session session, params string[] parameters)
    {
        // usage: @event start| stop | disable | enable name
        // @event clear < name > -clears event with name <name> or all events if you put in 'all' (All clears registered generators, <name> does not).
        // @event status<eventSubstring> - get the status of all registered events or get all of the registered events that have <eventSubstring> in the name.
        // @event - Maniuplates the state of an event.

        // TODO: output

        var eventCmd = parameters?[0].ToLower();

        var eventName = parameters?[1];

        switch (eventCmd)
        {
            case "start":
                if (EventManager.StartEvent(eventName, session?.Player, null))
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"Event {eventName} started successfully.",
                        ChatMessageType.Broadcast
                    );
                    PlayerManager.BroadcastToAuditChannel(
                        session?.Player,
                        $"{(session != null ? session.Player.Name : "CONSOLE")} has started event {eventName}."
                    );
                }
                else
                {
                    session?.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Unable to start event named {eventName} .",
                            ChatMessageType.Broadcast
                        )
                    );
                }

                break;
            case "stop":
                if (EventManager.StopEvent(eventName, session?.Player, null))
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"Event {eventName} stopped successfully.",
                        ChatMessageType.Broadcast
                    );
                    PlayerManager.BroadcastToAuditChannel(
                        session?.Player,
                        $"{(session != null ? session.Player.Name : "CONSOLE")} has stopped event {eventName}."
                    );
                }
                else
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"Unable to stop event named {eventName} .",
                        ChatMessageType.Broadcast
                    );
                }

                break;
            case "disable":
                break;
            case "enable":
                break;
            case "clear":
                break;
            case "status":
                if (eventName != "all" && eventName != "")
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"Event {eventName} - GameEventState.{EventManager.GetEventStatus(eventName)}",
                        ChatMessageType.Broadcast
                    );
                }
                break;
            default:
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    "That is not a valid event command",
                    ChatMessageType.Broadcast
                );
                break;
        }
    }
}
