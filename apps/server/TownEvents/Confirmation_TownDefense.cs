using System;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity;

namespace ACE.Server.TownEvents;

/// <summary>
/// The yes/no question asking a player to join a town's Town Defense. Like Confirmation_Lfg, the answer is passed on
/// whatever it is, since a "no" or no answer frees the question so the player can be asked again.
/// </summary>
public class Confirmation_TownDefense : Confirmation
{
    private readonly Action<bool> onAnswer;

    /// <param name="onAnswer">Called with true for yes, and false for no or no answer</param>
    public Confirmation_TownDefense(ObjectGuid playerGuid, Action<bool> onAnswer)
        : base(playerGuid, ConfirmationType.Yes_No)
    {
        this.onAnswer = onAnswer;
    }

    public override void ProcessConfirmation(bool response, bool timeout = false)
    {
        onAnswer(response && !timeout);
    }
}
