namespace ACE.Server.WorldObjects.Logging;

public class BankLogContainer(string name, uint? guid, bool bankPack = false)
{
    public string Name { get; } = name;
    public uint? Guid { get; } = guid;
    public bool BankPack { get; set; } = bankPack;
}
