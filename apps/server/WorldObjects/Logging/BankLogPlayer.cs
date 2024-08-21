namespace ACE.Server.WorldObjects.Logging;

public class BankLogPlayer(string name, uint accountId)
{
    public string Name { get; } = name;
    public uint AccountId { get; } = accountId;
}
