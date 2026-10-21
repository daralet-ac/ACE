using System;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Arena;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.PlayerUi;
using ACE.Server.Factories;
using ACE.Server.Network;

namespace ACE.Server.Commands.PlayerCommands;

public class PlayerUi
{
    /// <summary>
    /// Every request @playerui answers, sent in the hello reply.
    /// </summary>
    internal static readonly string[] Kinds = ["hello", "fellowship", "targets", "position", "bank", "bankwindow", "lootwindow", "market", "broker", "arena", "corpse", "salvage", "describe", "character", "quests", "abandon", "rollranges"];

    /// <summary>
    /// How often each player may ask (the playerui_ server properties). Asking faster (typing or scripting the command, say)
    /// gets no answer, so it can't be used to load the server.
    /// </summary>
    private static readonly PlayerUiLimits Limits = new();

    // playerui
    [CommandHandler(
        "playerui",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Data feed for the Daralet Decal plugin.",
        "hello | fellowship | targets <guid> [guid] | position | bank | bankwindow <on|off> | lootwindow <on|off>"
            + " | market | broker <list|cancel|claim|expired> ... | arena | corpse <guid> | describe <guid>... | salvage <guid>... | character | quests | abandon <contract id> | rollranges\n"
            + "The plugin sends these itself and hides the replies from chat. See PlayerUiProtocol for the reply format."
    )]
    public static void HandlePlayerui(Session session, params string[] parameters)
    {
        // Everything here is about the player who sent it: the only thing a request picks is which creatures near them to
        // describe.
        var player = session.Player;
        var kind = parameters[0].ToLowerInvariant();

        // every word that isn't a kind counts as one, so made-up kinds can't each get a limit of their own
        var limitKind = Kinds.Contains(kind) ? kind : "unknown";

        if (!Limits.Allow(player.Guid.Full, limitKind, DateTime.UtcNow, PlayerUiLimits.Settings.FromProperties()))
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
                // it reads the database, so it answers from another thread, or from the rows it read a moment ago
                PlayerUiMarketData.RequestMarket(player);
                return;
            case "arena":
                payload = ArenaManager.GetPlayerUiReport(player);
                break;
            case "describe":
                payload = PlayerUiCorpseData.DescribeCarried(player, parameters.Skip(1));
                break;
            case "character":
                // the Combat Focus appraisal's Character Stats, without needing a Combat Focus
                payload = new PlayerUiCharacter(player.GetStatsSheetSections());
                break;
            case "rollranges":
                // the same table for everyone, worked out once (LootGenerationFactory.GetLootFilterRanges)
                payload = new PlayerUiRollRanges(LootGenerationFactory.GetLootFilterRanges());
                break;
            case "corpse":
                payload = PlayerUiCorpseData.GetCorpse(player, PlayerUiCorpseData.ParseGuid(parameters.Length > 1 ? parameters[1] : null));
                break;
            case "quests":
                payload = PlayerUiQuestData.GetQuests(player);
                break;
            case "abandon":
            {
                // not a data request: the game says it's abandoned, or why it can't be, as the Contracts panel's Abandon does
                var problem = PlayerUiQuestData.Abandon(player, parameters.Length > 1 ? parameters[1] : null);

                if (problem != null)
                {
                    player.SendMessage(problem, ChatMessageType.System);
                }

                return;
            }
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
        foreach (var message in PlayerUiProtocol.BuildMessages(kind, payload, omitNulls: kind is "corpse" or "describe" or "quests"))
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

    /// <summary>At logout: when the player last asked for each kind matters only while they're online.</summary>
    public static void Forget(uint player)
    {
        Limits.Forget(player);
    }
}
