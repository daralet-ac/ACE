using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class ReadDat
{
    /// <summary>
    /// Debug console command for testing reading the client_portal.dat
    /// </summary>
    [CommandHandler(
        "readdat",
        AccessLevel.Developer,
        CommandHandlerFlag.ConsoleInvoke,
        0,
        "Tests reading the client_portal.dat"
    )]
    public static void HandleReadDat(Session session, params string[] parameters)
    {
        //int total = 0;
        //uint min = 0x0E010000;
        //uint max = 0x0E01FFFF;

        var test = DatManager.PortalDat.SkillTable;
        return;
        /*
        foreach (KeyValuePair<uint, DatFile> entry in DatManager.PortalDat.AllFiles)
        {
            if (entry.Value.ObjectId >= min && entry.Value.ObjectId <= max)
            {
                // Console.WriteLine("Reading " + entry.Value.ObjectId.ToString("X8"));
                QualityFilter item = DatManager.PortalDat.ReadFromDat<QualityFilter>(entry.Value.ObjectId);
                total++;
            }
        }
        if (DatManager.HighResDat != null)
        {
            foreach (KeyValuePair<uint, DatFile> entry in DatManager.HighResDat.AllFiles)
            {
                if (entry.Value.ObjectId >= min && entry.Value.ObjectId <= max)
                {
                    // Console.WriteLine("Reading " + entry.Value.ObjectId.ToString("X8"));
                    QualityFilter item = DatManager.PortalDat.ReadFromDat<QualityFilter>(entry.Value.ObjectId);
                    total++;
                }
            }
        }
        if(DatManager.LanguageDat != null)
        {
            foreach (KeyValuePair<uint, DatFile> entry in DatManager.LanguageDat.AllFiles)
            {
                if (entry.Value.ObjectId >= min && entry.Value.ObjectId <= max)
                {
                    // Console.WriteLine("Reading " + entry.Value.ObjectId.ToString("X8"));
                    QualityFilter item = DatManager.PortalDat.ReadFromDat<QualityFilter>(entry.Value.ObjectId);
                    total++;
                }
            }
        }

        Console.WriteLine(total.ToString() + " files read.");
        */
    }
}
