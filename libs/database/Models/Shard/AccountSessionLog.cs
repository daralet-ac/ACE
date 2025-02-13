using System;

namespace ACE.Database.Models.Shard;

public class AccountSessionLog
{
    public uint Id { get; set; }
    public uint AccountId { get; set; }
    public string AccountName { get; set; }
    public string SessionIp { get; set; }
    public DateTime LoginDateTime { get; set; }
}
