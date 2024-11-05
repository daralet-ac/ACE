using System;
using ACE.Common;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.PlayerCommands;

public class ReportBug
{
    // reportbug < code | content > < description >
    [CommandHandler(
        "reportbug",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        2,
        "Generate a Bug Report",
        "<category> <description>\n"
            + "This command generates a URL for you to copy and paste into your web browser to submit for review by server operators and developers.\n"
            + "Category can be the following:\n"
            + "Creature\n"
            + "NPC\n"
            + "Item\n"
            + "Quest\n"
            + "Recipe\n"
            + "Landblock\n"
            + "Mechanic\n"
            + "Code\n"
            + "Other\n"
            + "For the first three options, the bug report will include identifiers for what you currently have selected/targeted.\n"
            + "After category, please include a brief description of the issue, which you can further detail in the report on the website.\n"
            + "Examples:\n"
            + "/reportbug creature Drudge Prowler is over powered\n"
            + "/reportbug npc Ulgrim doesn't know what to do with Sake\n"
            + "/reportbug quest I can't enter the portal to the Lost City of Frore\n"
            + "/reportbug recipe I cannot combine Bundle of Arrowheads with Bundle of Arrowshafts\n"
            + "/reportbug code I was killed by a Non-Player Killer\n"
    )]
    public static void HandleReportbug(Session session, params string[] parameters)
    {
        if (!PropertyManager.GetBool("reportbug_enabled").Item)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "The command \"reportbug\" is not currently enabled on this server.",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        var category = parameters[0];
        var description = "";

        for (var i = 1; i < parameters.Length; i++)
        {
            description += parameters[i] + " ";
        }

        description.Trim();

        switch (category.ToLower())
        {
            case "creature":
            case "npc":
            case "quest":
            case "item":
            case "recipe":
            case "landblock":
            case "mechanic":
            case "code":
            case "other":
                break;
            default:
                category = "Other";
                break;
        }

        var sn = ConfigManager.Config.Server.WorldName;
        var c = session.Player.Name;

        var st = "ACE";

        //var versions = ServerBuildInfo.GetVersionInfo();
        var databaseVersion = DatabaseManager.World.GetVersion();
        var sv = ServerBuildInfo.FullVersion;
        var pv = databaseVersion.PatchVersion;

        //var ct = PropertyManager.GetString("reportbug_content_type").Item;
        var cg = category.ToLower();

        var w = "";
        var g = "";

        if (cg == "creature" || cg == "npc" || cg == "item" || cg == "item")
        {
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
                    objectId = new ObjectGuid((uint)session.Player.CurrentAppraisalTarget);
                }

                //var wo = session.Player.CurrentLandblock?.GetObject(objectId);

                var wo = session.Player.FindObject(objectId.Full, Player.SearchLocations.Everywhere);

                if (wo != null)
                {
                    w = $"{wo.WeenieClassId}";
                    g = $"0x{wo.Guid:X8}";
                }
            }
        }

        var l = session.Player.Location.ToLOCString();

        var issue = description;

        var urlbase = $"https://www.accpp.net/bug?";

        var url = urlbase;
        if (sn.Length > 0)
        {
            url += $"sn={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sn))}";
        }

        if (c.Length > 0)
        {
            url += $"&c={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(c))}";
        }

        if (st.Length > 0)
        {
            url += $"&st={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(st))}";
        }

        if (sv.Length > 0)
        {
            url += $"&sv={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sv))}";
        }

        if (pv.Length > 0)
        {
            url += $"&pv={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(pv))}";
        }
        //if (ct.Length > 0)
        //    url += $"&ct={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ct))}";
        if (cg.Length > 0)
        {
            if (cg == "npc")
            {
                cg = cg.ToUpper();
            }
            else
            {
                cg = char.ToUpper(cg[0]) + cg.Substring(1);
            }

            url += $"&cg={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(cg))}";
        }
        if (w.Length > 0)
        {
            url += $"&w={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(w))}";
        }

        if (g.Length > 0)
        {
            url += $"&g={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(g))}";
        }

        if (l.Length > 0)
        {
            url += $"&l={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(l))}";
        }

        if (issue.Length > 0)
        {
            url += $"&i={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(issue))}";
        }

        var msg = "\n\n\n\n";
        msg += "Bug Report - Copy and Paste the following URL into your browser to submit a bug report\n";
        msg += "-=-\n";
        msg += $"{url}\n";
        msg += "-=-\n";
        msg += "\n\n\n\n";

        session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.AdminTell));
    }
}
