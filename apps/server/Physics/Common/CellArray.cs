using System.Collections.Generic;

namespace ACE.Server.Physics.Common;

public class CellArray
{
    public bool AddedOutside;
    public bool LoadCells;
    public Dictionary<uint, ObjCell> Cells;
    public int NumCells;

    /// <summary>
    /// The instance the cells in this array belong to. The static helpers that collect cells (find_cell_list and friends)
    /// look up neighbouring cells by id, and use this to look them up in the same instance as whoever asked.
    /// 0 is the persistent world.
    /// </summary>
    public uint Instance;

    public CellArray()
    {
        Cells = new Dictionary<uint, ObjCell>();
    }

    public void SetStatic()
    {
        AddedOutside = false;
        LoadCells = false;
        NumCells = 0;
    }

    public void SetDynamic()
    {
        AddedOutside = false;
        LoadCells = true;
        NumCells = 0;
    }

    public void add_cell(uint cellID, ObjCell cell)
    {
        if (!Cells.ContainsKey(cellID))
        {
            Cells.Add(cellID, cell);
            NumCells++;
        }
    }

    public void remove_cell(ObjCell cell)
    {
        if (Cells.ContainsKey(cell.ID))
        {
            Cells.Remove(cell.ID);
            NumCells--;
        }
    }
}
