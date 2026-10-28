using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Player
{
    /// <summary>
    ///  The fellowship that this player belongs to
    /// </summary>
    public Fellowship Fellowship;

    public bool FellowVitalUpdate;

    // todo: Figure out if this is the best place to do this, and whether there are concurrency issues associated with it.
    public void FellowshipCreate(string fellowshipName, bool shareXP)
    {
        // An Olthoi player cannot create a fellowship
        if (IsOlthoiPlayer)
        {
            Session.Network.EnqueueSend(new GameEventWeenieError(Session, WeenieError.OlthoiCannotJoinFellowship));
            return;
        }

        Fellowship = new Fellowship(this, fellowshipName, shareXP);
        Session.Network.EnqueueSend(new GameEventFellowshipFullUpdate(Session));
        Session.Network.EnqueueSend(new GameEventFellowshipFellowUpdateDone(Session));
    }

    public void HandleActionFellowshipChangeOpenness(bool openness)
    {
        if (RefuseTownDefenseAction("change who can recruit into"))
        {
            return;
        }

        if (Fellowship != null)
        {
            if (Guid.Full != Fellowship.FellowshipLeaderGuid)
            {
                Session.Network.EnqueueSend(new GameEventWeenieError(Session, WeenieError.YouMustBeLeaderOfFellowship));
                return;
            }

            if (!Fellowship.IsLocked)
            {
                Fellowship.UpdateOpenness(openness);
            }
            else
            {
                Session.Network.EnqueueSend(new GameEventWeenieError(Session, WeenieError.FellowshipIsLocked));
            }
        }
    }

    public void HandleActionFellowshipChangeLock(bool lockState, string lockName)
    {
        if (Fellowship != null)
        {
            Fellowship.UpdateLock(lockState, lockName);
        }
    }

    public void FellowshipQuit(bool disband)
    {
        if (disband && RefuseTownDefenseAction("disband"))
        {
            return;
        }

        if (disband)
        {
            foreach (var member in Fellowship.GetFellowshipMembers())
            {
                if (member.Value.CurrentLandblock.IsFellowshipRequired())
                {
                    var leader = PlayerManager.GetOnlinePlayer(Fellowship.FellowshipLeaderGuid);

                    leader.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            "A member of this fellowship is in an area that prevents disbanding.",
                            ChatMessageType.Broadcast
                        )
                    );

                    return;
                }
            }
        }

        if (Fellowship != null)
        {
            Fellowship.QuitFellowship(this, disband);
        }
    }

    public void FellowshipDismissPlayer(uint dismissGuid)
    {
        if (RefuseTownDefenseAction("dismiss anyone from"))
        {
            return;
        }

        if (Fellowship == null)
        {
            return;
        }

        if (Guid.Full != Fellowship.FellowshipLeaderGuid)
        {
            Session.Network.EnqueueSend(new GameEventWeenieError(Session, WeenieError.YouMustBeLeaderOfFellowship));
            return;
        }

        if (Guid.Full == dismissGuid)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat("You can't dismiss yourself from the fellowship", ChatMessageType.Broadcast)
            );
            return;
        }

        var fellowToDismiss = PlayerManager.GetOnlinePlayer(dismissGuid);

        if (fellowToDismiss == null)
        {
            return;
        }

        Fellowship.RemoveFellowshipMember(fellowToDismiss, this);
    }

    public void FellowshipRecruit(Player newPlayer)
    {
        if (RefuseTownDefenseAction("recruit into"))
        {
            return;
        }

        if (newPlayer == null)
        {
            return;
        }

        // An Olthoi player cannot join a fellowship
        if (newPlayer.IsOlthoiPlayer)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "The Olthoi's hunger for destruction is too great to understand a request for fellowship.",
                    ChatMessageType.Broadcast
                )
            );
            SendWeenieError(WeenieError.None);
            return;
        }

        if (newPlayer.GetCharacterOption(CharacterOption.IgnoreFellowshipRequests))
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{newPlayer.Name} is not accepting fellowship requests.",
                    ChatMessageType.Fellowship
                )
            );
            Session.Network.EnqueueSend(new GameEventWeenieError(Session, WeenieError.FellowshipIgnoringRequests));
        }
        else if (Fellowship != null)
        {
            if (Guid.Full == Fellowship.FellowshipLeaderGuid || Fellowship.Open)
            {
                Fellowship.AddFellowshipMember(this, newPlayer);
            }
            else
            {
                Session.Network.EnqueueSend(new GameEventWeenieError(Session, WeenieError.YouMustBeLeaderOfFellowship));
            }
        }
    }

    public void FellowshipNewLeader(uint newLeaderGuid)
    {
        if (RefuseTownDefenseAction("pass the lead of"))
        {
            return;
        }

        if (Fellowship == null || Guid.Full == newLeaderGuid)
        {
            return;
        }

        if (Guid.Full != Fellowship.FellowshipLeaderGuid)
        {
            _log.Warning(
                $"{Name} tried to assign new fellowship leader from {Fellowship.FellowshipLeaderGuid:X8} to {newLeaderGuid:X8}"
            );
            return;
        }

        var newLeader = PlayerManager.GetOnlinePlayer(newLeaderGuid);

        if (newLeader == null)
        {
            return;
        }

        if (newLeader.Fellowship != Fellowship)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{newLeader.Name} is not a member of the fellowship!",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        Fellowship.AssignNewLeader(this, newLeader);
    }

    /// <summary>
    /// A Town Defense fellowship can only be left: TownEventManager adds its members and disbands it. Tells the player
    /// and returns true if they're in one.
    /// </summary>
    /// <param name="action">What they tried to do, to finish "You can't ... the Town Defense"</param>
    private bool RefuseTownDefenseAction(string action)
    {
        if (Fellowship is not { IsTownDefense: true })
        {
            return false;
        }

        Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"You can't {action} the Town Defense. You can only leave it, and it disbands when the attack is over.",
                ChatMessageType.Broadcast
            )
        );
        return true;
    }

    public bool FellowshipPanelOpen { get; set; }

    /// <summary>
    /// Called when player opens / closes the fellowship panel
    /// </summary>
    public void HandleFellowshipUpdateRequest(bool panelOpen)
    {
        FellowshipPanelOpen = panelOpen;

        if (Fellowship != null && FellowshipPanelOpen)
        {
            Session.Network.EnqueueSend(new GameEventFellowshipFullUpdate(Session));
        }
    }
}
