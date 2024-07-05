using System.Collections.Generic;
using Discord;

namespace ACE.Server.Discord.Models;

public record ImportWeenieParameters : IImportParameters
{
    public ImportWeenieParameters(
        IAttachment weenie1,
        IAttachment weenie2 = null,
        IAttachment weenie3 = null,
        IAttachment weenie4 = null,
        IAttachment weenie5 = null,
        IAttachment weenie6 = null,
        IAttachment weenie7 = null,
        IAttachment weenie8 = null,
        IAttachment weenie9 = null,
        IAttachment weenie10 = null,
        IAttachment weenie11 = null,
        IAttachment weenie12 = null,
        IAttachment weenie13 = null,
        IAttachment weenie14 = null,
        IAttachment weenie15 = null,
        IAttachment weenie16 = null,
        IAttachment weenie17 = null,
        IAttachment weenie18 = null,
        IAttachment weenie19 = null,
        IAttachment weenie20 = null,
        IAttachment weenie21 = null,
        IAttachment weenie22 = null,
        IAttachment weenie23 = null,
        IAttachment weenie24 = null,
        bool ephemeral = true
    )
    {
        if (weenie1 != null)
        {
            Files.Add(weenie1);
        }
        if (weenie2 != null)
        {
            Files.Add(weenie2);
        }
        if (weenie3 != null)
        {
            Files.Add(weenie3);
        }
        if (weenie4 != null)
        {
            Files.Add(weenie4);
        }
        if (weenie5 != null)
        {
            Files.Add(weenie5);
        }
        if (weenie6 != null)
        {
            Files.Add(weenie6);
        }
        if (weenie7 != null)
        {
            Files.Add(weenie7);
        }
        if (weenie8 != null)
        {
            Files.Add(weenie8);
        }
        if (weenie9 != null)
        {
            Files.Add(weenie9);
        }
        if (weenie10 != null)
        {
            Files.Add(weenie10);
        }
        if (weenie11 != null)
        {
            Files.Add(weenie11);
        }
        if (weenie12 != null)
        {
            Files.Add(weenie12);
        }
        if (weenie13 != null)
        {
            Files.Add(weenie13);
        }
        if (weenie14 != null)
        {
            Files.Add(weenie14);
        }
        if (weenie15 != null)
        {
            Files.Add(weenie15);
        }
        if (weenie16 != null)
        {
            Files.Add(weenie16);
        }
        if (weenie17 != null)
        {
            Files.Add(weenie17);
        }
        if (weenie18 != null)
        {
            Files.Add(weenie18);
        }
        if (weenie19 != null)
        {
            Files.Add(weenie19);
        }
        if (weenie20 != null)
        {
            Files.Add(weenie20);
        }
        if (weenie21 != null)
        {
            Files.Add(weenie21);
        }
        if (weenie22 != null)
        {
            Files.Add(weenie22);
        }
        if (weenie23 != null)
        {
            Files.Add(weenie23);
        }
        if (weenie24 != null)
        {
            Files.Add(weenie24);
        }

        Ephemeral = ephemeral;
    }

    public IList<IAttachment> Files { get; } = new List<IAttachment>();
    public bool Ephemeral { get; }
}
