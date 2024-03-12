using System.IO;
using Serilog;

namespace ACE.DatLoader
{
    public static class DatManager
    {
        private static readonly ILogger _log = Log.ForContext(typeof(DatManager));

        private static string datFile;

        private static int count;

        private static int ITERATION_CELL = 30000;
        private static int ITERATION_PORTAL = 30013;
        private static int ITERATION_HIRES = 497;
        private static int ITERATION_LANGUAGE = 30005;
        public static CellDatDatabase CellDat { get; private set; }

        public static PortalDatDatabase PortalDat { get; private set; }
        public static DatDatabase HighResDat { get; private set; }
        public static LanguageDatDatabase LanguageDat { get; private set; }

        public static void Initialize(string datFileDirectory, bool keepOpen = false, bool loadCell = true)
        {
            var datDir = Path.GetFullPath(Path.Combine(datFileDirectory));

            if (loadCell)
            {
                try
                {
                    datFile = Path.Combine(datDir, "client_cell_1.dat");
                    CellDat = new CellDatDatabase(datFile, keepOpen);
                    count = CellDat.AllFiles.Count;
                    _log.Information($"Successfully opened {datFile} file, containing {count} records, iteration {CellDat.Iteration}");
                    if (CellDat.Iteration != ITERATION_CELL)
                        _log.Warning($"{datFile} iteration {CellDat.Iteration} does not match expected version of {ITERATION_CELL}.");
                }
                catch (FileNotFoundException ex)
                {
                    _log.Error($"An exception occured while attempting to open {datFile} file!  This needs to be corrected in order for Landblocks to load!");
                    _log.Error($"Exception: {ex.Message}");
                }
            }

            try
            {
                datFile = Path.Combine(datDir, "client_portal.dat");
                PortalDat = new PortalDatDatabase(datFile, keepOpen);
                PortalDat.SkillTable.AddRetiredSkills();
                count = PortalDat.AllFiles.Count;
                _log.Information($"Successfully opened {datFile} file, containing {count} records, iteration {PortalDat.Iteration}");
                if (PortalDat.Iteration != ITERATION_PORTAL)
                    _log.Warning($"{datFile} iteration {PortalDat.Iteration} does not match expected version of {ITERATION_PORTAL}.");
            }
            catch (FileNotFoundException ex)
            {
                _log.Error($"An exception occured while attempting to open {datFile} file!\n\n *** Please check your 'DatFilesDirectory' setting in the config.js file. ***\n *** ACE will not run properly without this properly configured! ***\n");
                _log.Error($"Exception: {ex.Message}");
            }

            // Load the client_highres.dat file. This is not required for ACE operation, so no exception needs to be generated.
            datFile = Path.Combine(datDir, "client_highres.dat");
            if (File.Exists(datFile))
            {
                HighResDat = new DatDatabase(datFile, keepOpen);
                count = HighResDat.AllFiles.Count;
                _log.Information($"Successfully opened {datFile} file, containing {count} records, iteration {HighResDat.Iteration}");
                if (HighResDat.Iteration != ITERATION_HIRES)
                    _log.Warning($"{datFile} iteration {HighResDat.Iteration} does not match expected iteration version of {ITERATION_HIRES}.");
            }

            try
            {
                datFile = Path.Combine(datDir, "client_local_English.dat");
                LanguageDat = new LanguageDatDatabase(datFile, keepOpen);
                count = LanguageDat.AllFiles.Count;
                _log.Information($"Successfully opened {datFile} file, containing {count} records, iteration {LanguageDat.Iteration}");
                if(LanguageDat.Iteration != ITERATION_LANGUAGE)
                    _log.Warning($"{datFile} iteration {LanguageDat.Iteration} does not match expected version of {ITERATION_LANGUAGE}.");
            }
            catch (FileNotFoundException ex)
            {
                _log.Error($"An exception occured while attempting to open {datFile} file!\n\n *** Please check your 'DatFilesDirectory' setting in the config.json file. ***\n *** ACE will not run properly without this properly configured! ***\n");
                _log.Error($"Exception: {ex.Message}");
            }
        }
    }
}
