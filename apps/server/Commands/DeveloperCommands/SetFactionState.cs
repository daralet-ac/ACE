using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class SetFactionState
{
    // faction
    [CommandHandler(
        "faction",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "sets your own faction state.",
        "< none / ch / ew / rb > (rank)\n"
            + "This command sets your current faction state\n"
            + "< none > No Faction\n"
            + "< ch > Celestial Hand\n"
            + "< ew > Eldrytch Web\n"
            + "< rb > Radiant Blood\n"
            + "(rank) 1 = Initiate | 2 = Adept | 3 = Knight | 4 = Lord | 5 = Master"
    )]
    public static void HandleFaction(Session session, params string[] parameters)
    {
        var rankStr = "Initiate";
        if (parameters.Length == 0)
        {
            var message =
                $"Your current Faction state is: {session.Player.Society.ToSentence()}\n"
                + "You can change it to the following:\n"
                + "NONE      = No Faction\n"
                + "CH        = Celestial Hand\n"
                + "EW        = Eldrytch Web\n"
                + "RB        = Radiant Blood\n"
                + "Optionally you can also include a rank, otherwise rank will be set to Initiate\n1 = Initiate | 2 = Adept | 3 = Knight | 4 = Lord | 5 = Master";
            CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.Broadcast);
        }
        else
        {
            switch (parameters[0].ToLower())
            {
                case "none":
                    session.Player.Faction1Bits = null;
                    session.Player.SocietyRankCelhan = null;
                    session.Player.SocietyRankEldweb = null;
                    session.Player.SocietyRankRadblo = null;
                    session.Player.QuestManager.Erase("SocietyMember");
                    session.Player.QuestManager.Erase("SocietyFlag");
                    session.Player.QuestManager.Erase("CelestialHandMember");
                    session.Player.QuestManager.Erase("EldrytchWebMember");
                    session.Player.QuestManager.Erase("RadiantBloodMember");
                    break;
                case "ch":
                    session.Player.Faction1Bits = FactionBits.CelestialHand;
                    session.Player.SocietyRankCelhan = 1;
                    session.Player.SocietyRankEldweb = null;
                    session.Player.SocietyRankRadblo = null;
                    session.Player.QuestManager.SetQuestBits("SocietyMember", (int)FactionBits.CelestialHand, true);
                    session.Player.QuestManager.SetQuestBits("SocietyFlag", (int)FactionBits.CelestialHand, true);
                    session.Player.QuestManager.SetQuestBits(
                        "SocietyMember",
                        (int)(FactionBits.EldrytchWeb | FactionBits.RadiantBlood),
                        false
                    );
                    session.Player.QuestManager.SetQuestBits(
                        "SocietyFlag",
                        (int)(FactionBits.EldrytchWeb | FactionBits.RadiantBlood),
                        false
                    );
                    session.Player.QuestManager.Stamp("CelestialHandMember");
                    session.Player.QuestManager.Erase("EldrytchWebMember");
                    session.Player.QuestManager.Erase("RadiantBloodMember");
                    if (parameters.Length == 2 && int.TryParse(parameters[1], out var rank))
                    {
                        switch (rank)
                        {
                            case 1:
                                session.Player.SocietyRankCelhan = 1;
                                rankStr = "Initiate";
                                break;
                            case 2:
                                session.Player.SocietyRankCelhan = 101;
                                rankStr = "Adept";
                                break;
                            case 3:
                                session.Player.SocietyRankCelhan = 301;
                                rankStr = "Knight";
                                break;
                            case 4:
                                session.Player.SocietyRankCelhan = 601;
                                rankStr = "Lord";
                                break;
                            case 5:
                                session.Player.SocietyRankCelhan = 1001;
                                rankStr = "Master";
                                break;
                        }
                    }
                    break;
                case "ew":
                    session.Player.Faction1Bits = FactionBits.EldrytchWeb;
                    session.Player.SocietyRankCelhan = null;
                    session.Player.SocietyRankEldweb = 1;
                    session.Player.SocietyRankRadblo = null;
                    session.Player.QuestManager.SetQuestBits("SocietyMember", (int)FactionBits.EldrytchWeb, true);
                    session.Player.QuestManager.SetQuestBits("SocietyFlag", (int)FactionBits.EldrytchWeb, true);
                    session.Player.QuestManager.SetQuestBits(
                        "SocietyMember",
                        (int)(FactionBits.CelestialHand | FactionBits.RadiantBlood),
                        false
                    );
                    session.Player.QuestManager.SetQuestBits(
                        "SocietyFlag",
                        (int)(FactionBits.CelestialHand | FactionBits.RadiantBlood),
                        false
                    );
                    session.Player.QuestManager.Erase("CelestialHandMember");
                    session.Player.QuestManager.Stamp("EldrytchWebMember");
                    session.Player.QuestManager.Erase("RadiantBloodMember");
                    if (parameters.Length == 2 && int.TryParse(parameters[1], out rank))
                    {
                        switch (rank)
                        {
                            case 1:
                                session.Player.SocietyRankEldweb = 1;
                                rankStr = "Initiate";
                                break;
                            case 2:
                                session.Player.SocietyRankEldweb = 101;
                                rankStr = "Adept";
                                break;
                            case 3:
                                session.Player.SocietyRankEldweb = 301;
                                rankStr = "Knight";
                                break;
                            case 4:
                                session.Player.SocietyRankEldweb = 601;
                                rankStr = "Lord";
                                break;
                            case 5:
                                session.Player.SocietyRankEldweb = 1001;
                                rankStr = "Master";
                                break;
                        }
                    }
                    break;
                case "rb":
                    session.Player.Faction1Bits = FactionBits.RadiantBlood;
                    session.Player.SocietyRankCelhan = null;
                    session.Player.SocietyRankEldweb = null;
                    session.Player.SocietyRankRadblo = 1;
                    session.Player.QuestManager.SetQuestBits("SocietyMember", (int)FactionBits.RadiantBlood, true);
                    session.Player.QuestManager.SetQuestBits("SocietyFlag", (int)FactionBits.RadiantBlood, true);
                    session.Player.QuestManager.SetQuestBits(
                        "SocietyMember",
                        (int)(FactionBits.CelestialHand | FactionBits.EldrytchWeb),
                        false
                    );
                    session.Player.QuestManager.SetQuestBits(
                        "SocietyFlag",
                        (int)(FactionBits.CelestialHand | FactionBits.EldrytchWeb),
                        false
                    );
                    session.Player.QuestManager.Erase("CelestialHandMember");
                    session.Player.QuestManager.Erase("EldrytchWebMember");
                    session.Player.QuestManager.Stamp("RadiantBloodMember");
                    if (parameters.Length == 2 && int.TryParse(parameters[1], out rank))
                    {
                        switch (rank)
                        {
                            case 1:
                                session.Player.SocietyRankRadblo = 1;
                                rankStr = "Initiate";
                                break;
                            case 2:
                                session.Player.SocietyRankRadblo = 101;
                                rankStr = "Adept";
                                break;
                            case 3:
                                session.Player.SocietyRankRadblo = 301;
                                rankStr = "Knight";
                                break;
                            case 4:
                                session.Player.SocietyRankRadblo = 601;
                                rankStr = "Lord";
                                break;
                            case 5:
                                session.Player.SocietyRankRadblo = 1001;
                                rankStr = "Master";
                                break;
                        }
                    }
                    break;
            }
            session.Player.EnqueueBroadcast(
                new GameMessagePrivateUpdatePropertyInt(
                    session.Player,
                    PropertyInt.Faction1Bits,
                    (int)(session.Player.Faction1Bits ?? 0)
                )
            );
            session.Player.EnqueueBroadcast(
                new GameMessagePrivateUpdatePropertyInt(
                    session.Player,
                    PropertyInt.SocietyRankCelhan,
                    session.Player.SocietyRankCelhan ?? 0
                )
            );
            session.Player.EnqueueBroadcast(
                new GameMessagePrivateUpdatePropertyInt(
                    session.Player,
                    PropertyInt.SocietyRankEldweb,
                    session.Player.SocietyRankEldweb ?? 0
                )
            );
            session.Player.EnqueueBroadcast(
                new GameMessagePrivateUpdatePropertyInt(
                    session.Player,
                    PropertyInt.SocietyRankRadblo,
                    session.Player.SocietyRankRadblo ?? 0
                )
            );
            session.Player.SendTurbineChatChannels();
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Your current Faction state is now set to: {session.Player.Society.ToSentence()}{(session.Player.Society != FactionBits.None ? $" with a rank of {rankStr}" : "")}",
                ChatMessageType.Broadcast
            );

            PlayerManager.BroadcastToAuditChannel(
                session.Player,
                $"{session.Player.Name} changed their Faction state to {session.Player.Society.ToSentence()}{(session.Player.Society != FactionBits.None ? $" with a rank of {rankStr}" : "")}."
            );
        }
    }
}
