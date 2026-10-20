using System;
using System.Collections.Concurrent;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Arena;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.PlayerUi;
using ACE.Server.Network;

namespace ACE.Server.Commands.PlayerCommands;

public class PlayerUi
{
    /// <summary>
    /// Every request @playerui answers, sent in the hello reply.
    /// </summary>
    internal static readonly string[] Kinds = ["hello", "fellowship", "targets", "position", "bank", "bankwindow", "lootwindow", "market", "broker", "arena", "corpse", "salvage", "describe"];

    /// <summary>
    /// The plugin asks for fellowship and targets once a second. Asking faster than this (typing the command by hand, say)
    /// gets no answer, so it can't be used to load the server.
    /// </summary>
    private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(400);

    private static readonly ConcurrentDictionary<(uint Player, string Kind), DateTime> LastRequest = new();

    // playerui
    [CommandHandler(
        "playerui",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Data feed for the Daralet Decal plugin.",
        "hello | fellowship | targets <guid> [guid] | position | bank | bankwindow <on|off> | lootwindow <on|off>"
            + " | market | broker <list|cancel|claim|expired> ... | arena | corpse <guid> | describe <guid>... | salvage <guid>...\n"
            + "The plugin sends these itself and hides the replies from chat. See PlayerUiProtocol for the reply format."
    )]
    public static void HandlePlayerui(Session session, params string[] parameters)
    {
        // Everything here is about the player who sent it: the only thing a request picks is which creatures near them to
        // describe.
        var player = session.Player;
        var kind = parameters[0].ToLowerInvariant();

        if (kind != "hello" && TooSoon(player.Guid.Full, kind))
        {
            return;
        }

        object payload;

        switch (kind)
        {
            case "hello":
                payload = new PlayerUiHello(2,player.Name, player.Guid.Full, Kinds);
                break;
            case "fellowship":
                payload = PlayerUiData.GetFellowship(player);
                break;
            case "targets":
                payload = PlayerUiData.GetTargets(player, PlayerUiData.ParseGuids(parameters.Skip(1)));
                break;
            case "position":
                payload = PlayerUiData.GetPosition(player);
                break;
            case "bank":
                payload = PlayerUiData.GetBank(player);
                break;
            case "bankwindow":
                // only changes which messages this player's own client gets when they open a bank
                player.PlayerUiBankWindow = OnOff(parameters, player.PlayerUiBankWindow);
                payload = new PlayerUiWindowSetting(player.PlayerUiBankWindow);
                break;
            case "lootwindow":
                // the same, for corpses and chests
                player.PlayerUiLootWindow = OnOff(parameters, player.PlayerUiLootWindow);
                payload = new PlayerUiWindowSetting(player.PlayerUiLootWindow);
                break;
            case "market":
                payload = PlayerUiMarketData.GetMarket(player);
                break;
            case "arena":
                payload = ArenaManager.GetPlayerUiReport(player);
                break;
            case "describe":
                payload = PlayerUiCorpseData.DescribeCarried(player, parameters.Skip(1));
                break;
            case "corpse":
                payload = PlayerUiCorpseData.GetCorpse(player, PlayerUiCorpseData.ParseGuid(parameters.Length > 1 ? parameters[1] : null));
                break;
            case "salvage":
            {
                // not a data request either: the salvaging says what it made
                var problem = PlayerUiCorpseData.Salvage(player, parameters.Skip(1));

                if (problem != null)
                {
                    player.SendMessage(problem, ChatMessageType.System);
                }

                return;
            }
            case "broker":
            {
                // not a data request: it acts through a nearby Market Broker, which answers in its own tells
                var problem = PlayerUiMarketData.BrokerAction(player, parameters.Skip(1).ToArray());

                if (problem != null)
                {
                    player.SendMessage(problem, ChatMessageType.System);
                }

                return;
            }
            default:
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    $"Unknown @playerui request: {parameters[0]}. Use {string.Join(", ", Kinds)}.",
                    ChatMessageType.Broadcast
                );
                return;
        }

        // a corpse's items are whole market listings, mostly empty fields
        foreach (var message in PlayerUiProtocol.BuildMessages(kind, payload, omitNulls: kind is "corpse" or "describe"))
        {
            CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.Broadcast);
        }
    }

    /// <summary>"on" or "off" after the request's name, or what it was when it's neither (asking without changing it).</summary>
    private static bool OnOff(string[] parameters, bool current)
    {
        return (parameters.Length > 1 ? parameters[1].ToLowerInvariant() : null) switch
        {
            "on" => true,
            "off" => false,
            _ => current,
        };
    }

    private static bool TooSoon(uint player, string kind)
    {
        var now = DateTime.UtcNow;
        var key = (player, kind);

        if (LastRequest.TryGetValue(key, out var last) && now - last < MinInterval)
        {
            return true;
        }

        LastRequest[key] = now;
        return false;
    }
}
