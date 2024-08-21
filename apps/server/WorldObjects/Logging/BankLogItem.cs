namespace ACE.Server.WorldObjects.Logging;

public class BankLogItem(string name, uint guid, int? stackSize, int? placementPosition)
{
    public string Name { get; } = name;
    public uint Guid { get; } = guid;
    public int? StackSize { get; } = stackSize;
    public int? PlacementPosition { get; } = placementPosition;
}
