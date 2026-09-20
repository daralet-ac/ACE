using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using Serilog;

namespace ACE.Server.Physics.Util;

public class AdjustCell
{
    private static readonly ILogger _log = Log.ForContext<AdjustCell>();

    public List<Common.EnvCell> EnvCells;
    public static ConcurrentDictionary<uint, AdjustCell> AdjustCells = new ConcurrentDictionary<uint, AdjustCell>();

    public AdjustCell(uint dungeonID)
    {
        var blockInfoID = dungeonID << 16 | 0xFFFE;
        var blockinfo = DatManager.CellDat.ReadFromDat<LandblockInfo>(blockInfoID);
        var numCells = blockinfo.NumCells;

        BuildEnv(dungeonID, numCells);
    }

    public void BuildEnv(uint dungeonID, uint numCells)
    {
        EnvCells = new List<Common.EnvCell>();
        uint firstCellID = 0x100;
        var missingCells = 0;
        for (uint i = 0; i < numCells; i++)
        {
            var cellID = firstCellID + i;
            var blockCell = dungeonID << 16 | cellID;

            var objCell = Common.LScape.get_landcell(blockCell);
            var envCell = objCell as Common.EnvCell;

            // A cell that isn't in the cell dat still comes back as an EnvCell, just an empty one without a CellStructure.
            // It can never contain a point, so it is left out.
            if (envCell?.CellStructure != null)
            {
                EnvCells.Add(envCell);
            }
            else
            {
                missingCells++;
            }
        }

        if (missingCells > 0)
        {
            _log.Warning(
                "AdjustCell: landblock {Landblock:X4} lists {NumCells} interior cells in its LandblockInfo, but {MissingCells} of them are not in the cell dat (stale LandblockInfo.NumCells?). Those cells are ignored.",
                dungeonID,
                numCells,
                missingCells
            );
        }
    }

    public uint? GetCell(Vector3 point)
    {
        foreach (var envCell in EnvCells)
        {
            if (envCell.point_in_cell(point))
            {
                return envCell.ID;
            }
        }

        return null;
    }

    public static AdjustCell Get(uint dungeonID)
    {
        AdjustCell adjustCell = null;
        AdjustCells.TryGetValue(dungeonID, out adjustCell);
        if (adjustCell == null)
        {
            adjustCell = new AdjustCell(dungeonID);
            AdjustCells.TryAdd(dungeonID, adjustCell);
        }
        return adjustCell;
    }
}
