namespace ACE.Server.Discord.Models;

public class ImportResult
{
    public bool Success { get; set; }
    public string FailureReason { get; set; }
    public string FileName { get; set; }
}
