using System.Collections.Generic;
using Discord;

namespace ACE.Server.Discord.Models;

public record ImportLandblockParameters : IImportParameters
{
    public ImportLandblockParameters(
        IAttachment landblock1,
        IAttachment landblock2 = null,
        IAttachment landblock3 = null,
        IAttachment landblock4 = null,
        IAttachment landblock5 = null,
        IAttachment landblock6 = null,
        IAttachment landblock7 = null,
        IAttachment landblock8 = null,
        IAttachment landblock9 = null,
        IAttachment landblock10 = null,
        IAttachment landblock11 = null,
        IAttachment landblock12 = null,
        IAttachment landblock13 = null,
        IAttachment landblock14 = null,
        IAttachment landblock15 = null,
        IAttachment landblock16 = null,
        IAttachment landblock17 = null,
        IAttachment landblock18 = null,
        IAttachment landblock19 = null,
        IAttachment landblock20 = null,
        IAttachment landblock21 = null,
        IAttachment landblock22 = null,
        IAttachment landblock23 = null,
        IAttachment landblock24 = null,
        bool ephemeral = true
    )
    {
        if (landblock1 != null)
        {
            Files.Add(landblock1);
        }
        if (landblock2 != null)
        {
            Files.Add(landblock2);
        }
        if (landblock3 != null)
        {
            Files.Add(landblock3);
        }
        if (landblock4 != null)
        {
            Files.Add(landblock4);
        }
        if (landblock5 != null)
        {
            Files.Add(landblock5);
        }
        if (landblock6 != null)
        {
            Files.Add(landblock6);
        }
        if (landblock7 != null)
        {
            Files.Add(landblock7);
        }
        if (landblock8 != null)
        {
            Files.Add(landblock8);
        }
        if (landblock9 != null)
        {
            Files.Add(landblock9);
        }
        if (landblock10 != null)
        {
            Files.Add(landblock10);
        }
        if (landblock11 != null)
        {
            Files.Add(landblock11);
        }
        if (landblock12 != null)
        {
            Files.Add(landblock12);
        }
        if (landblock13 != null)
        {
            Files.Add(landblock13);
        }
        if (landblock14 != null)
        {
            Files.Add(landblock14);
        }
        if (landblock15 != null)
        {
            Files.Add(landblock15);
        }
        if (landblock16 != null)
        {
            Files.Add(landblock16);
        }
        if (landblock17 != null)
        {
            Files.Add(landblock17);
        }
        if (landblock18 != null)
        {
            Files.Add(landblock18);
        }
        if (landblock19 != null)
        {
            Files.Add(landblock19);
        }
        if (landblock20 != null)
        {
            Files.Add(landblock20);
        }
        if (landblock21 != null)
        {
            Files.Add(landblock21);
        }
        if (landblock22 != null)
        {
            Files.Add(landblock22);
        }
        if (landblock23 != null)
        {
            Files.Add(landblock23);
        }
        if (landblock24 != null)
        {
            Files.Add(landblock24);
        }

        Ephemeral = ephemeral;
    }

    public IList<IAttachment> Files { get; } = new List<IAttachment>();
    public bool Ephemeral { get; }
}
