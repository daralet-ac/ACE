using System;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity;

namespace ACE.Server.Lfg;

/// <summary>
/// The yes/no question a fellowship leader with a manual lfm gets about someone who wants to join. Like Confirmation_Arena, the answer
/// is passed on whatever it is, since a "no" or no answer has to free the question for the next player.
/// </summary>
public class Confirmation_Lfg : Confirmation
{
    private readonly Action<bool> onAnswer;

    /// <param name="onAnswer">Called with true for yes, and false for no or no answer</param>
    public Confirmation_Lfg(ObjectGuid playerGuid, Action<bool> onAnswer)
        : base(playerGuid, ConfirmationType.Yes_No)
    {
        this.onAnswer = onAnswer;
    }

    public override void ProcessConfirmation(bool response, bool timeout = false)
    {
        onAnswer(response && !timeout);
    }
}
