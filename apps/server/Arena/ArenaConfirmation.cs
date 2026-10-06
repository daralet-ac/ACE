using System;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity;

namespace ACE.Server.Arena;

/// <summary>
/// A yes/no question about a duel. Unlike Confirmation_Custom, the answer is passed on whatever it is:
/// a duel has to know when someone says no, or the client closes the question because it was not answered in time.
/// </summary>
public class Confirmation_Arena : Confirmation
{
    private readonly Action<bool> onAnswer;

    /// <param name="onAnswer">Called with true for yes, and false for no or no answer. It can be called after the duel was called off.</param>
    public Confirmation_Arena(ObjectGuid playerGuid, Action<bool> onAnswer)
        : base(playerGuid, ConfirmationType.Yes_No)
    {
        this.onAnswer = onAnswer;
    }

    public override void ProcessConfirmation(bool response, bool timeout = false)
    {
        onAnswer(response && !timeout);
    }
}
