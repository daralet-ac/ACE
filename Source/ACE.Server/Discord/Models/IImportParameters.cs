using System.Collections.Generic;
using Discord;

namespace ACE.Server.Discord.Models;

public interface IImportParameters
{
    IList<IAttachment> Files { get; }
    bool Ephemeral { get; }
}
