using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;

namespace ACE.Server.WorldObjects;

partial class WorldObject
{
    /// <summary>
    /// Handles casting SpellType.PortalLink spells
    /// </summary>
    private void HandleCastSpell_PortalLink(Spell spell, WorldObject target)
    {
        if (this is not Player player)
        {
            return;
        }

        if (player.IsOlthoiPlayer)
        {
            player.Session.Network.EnqueueSend(
                new GameEventWeenieError(player.Session, WeenieError.OlthoiCanOnlyRecallToLifestone)
            );
            return;
        }

        switch ((SpellId)spell.Id)
        {
            case SpellId.LifestoneTie1: // Lifestone Tie

                if (target.WeenieType == WeenieType.LifeStone)
                {
                    player.SendChatMessage(
                        this,
                        "You have successfully linked with the life stone.",
                        ChatMessageType.Magic
                    );
                    player.LinkedLifestone = target.Location;
                }
                else
                {
                    player.SendChatMessage(this, "You cannot link that.", ChatMessageType.Magic);
                }

                break;

            case SpellId.PortalTie1: // Primary Portal Tie
            case SpellId.PortalTie2: // Secondary Portal Tie

                if (target.WeenieType != WeenieType.Portal)
                {
                    player.SendChatMessage(this, "You cannot link that.", ChatMessageType.Magic);
                    break;
                }

                var targetPortal = target as Portal;

                var summoned = targetPortal is { OriginalPortal: not null };

                if (targetPortal != null)
                {
                    var targetDid = summoned ? targetPortal.OriginalPortal : targetPortal.WeenieClassId;

                    var tiePortal = GetPortal(targetDid.Value);

                    if (tiePortal == null)
                    {
                        player.Session.Network.EnqueueSend(
                            new GameEventWeenieError(player.Session, WeenieError.YouCannotLinkToThatPortal)
                        );
                        break;
                    }

                    tiePortal.PlayerUsingTieOrSummonSpell = true;

                    var result = tiePortal.CheckUseRequirements(player);

                    if (!result.Success && result.Message != null)
                    {
                        player.Session.Network.EnqueueSend(result.Message);
                    }

                    if (tiePortal.NoTie || !result.Success)
                    {
                        player.Session.Network.EnqueueSend(
                            new GameEventWeenieError(player.Session, WeenieError.YouCannotLinkToThatPortal)
                        );
                        break;
                    }

                    var isPrimary = spell.Id == (int)SpellId.PortalTie1;

                    if (isPrimary)
                    {
                        player.LinkedPortalOneDID = targetDid;
                        player.SetProperty(PropertyBool.LinkedPortalOneSummon, summoned);
                    }
                    else
                    {
                        player.LinkedPortalTwoDID = targetDid;
                        player.SetProperty(PropertyBool.LinkedPortalTwoSummon, summoned);
                    }
                }

                player.SendChatMessage(this, "You have successfully linked with the portal.", ChatMessageType.Magic);
                break;
        }
    }

    /// <summary>
    /// Returns a Portal object for a WCID
    /// </summary>
    protected static Portal GetPortal(uint wcid)
    {
        var weenie = DatabaseManager.World.GetCachedWeenie(wcid);

        return WorldObjectFactory.CreateWorldObject(weenie, new ObjectGuid(wcid)) as Portal;
    }

    /// <summary>
    /// Handles casting SpellType.PortalRecall spells
    /// </summary>
    private void HandleCastSpell_PortalRecall(Spell spell, Creature targetCreature)
    {
        var player = this as Player;

        if (player is { IsOlthoiPlayer: true })
        {
            player.Session.Network.EnqueueSend(
                new GameEventWeenieError(player.Session, WeenieError.OlthoiCanOnlyRecallToLifestone)
            );
            return;
        }

        var targetPlayer = targetCreature as Player;

        if (player is { PKTimerActive: true })
        {
            player.Session.Network.EnqueueSend(
                new GameEventWeenieError(player.Session, WeenieError.YouHaveBeenInPKBattleTooRecently)
            );
            return;
        }

        var recall = PositionType.Undef;
        uint? recallDid = null;

        // verify pre-requirements for recalls

        switch ((SpellId)spell.Id)
        {
            case SpellId.PortalRecall: // portal recall

                if (targetPlayer is { LastPortalDID: null })
                {
                    // You must link to a portal to recall it!
                    targetPlayer.Session.Network.EnqueueSend(
                        new GameEventWeenieError(targetPlayer.Session, WeenieError.YouMustLinkToPortalToRecall)
                    );
                }
                else
                {
                    recall = PositionType.LastPortal;
                    if (targetPlayer != null)
                    {
                        recallDid = targetPlayer.LastPortalDID;
                    }
                }
                break;

            case SpellId.LifestoneRecall1: // lifestone recall

                if (targetPlayer != null && targetPlayer.GetPosition(PositionType.LinkedLifestone) == null)
                {
                    // You must link to a lifestone to recall it!
                    targetPlayer.Session.Network.EnqueueSend(
                        new GameEventWeenieError(targetPlayer.Session, WeenieError.YouMustLinkToLifestoneToRecall)
                    );
                }
                else
                {
                    recall = PositionType.LinkedLifestone;
                }

                break;

            case SpellId.LifestoneSending1:

                if (player?.GetPosition(PositionType.Sanctuary) != null)
                {
                    recall = PositionType.Sanctuary;
                }
                else if (targetPlayer != null && targetPlayer.GetPosition(PositionType.Sanctuary) != null)
                {
                    recall = PositionType.Sanctuary;
                }

                break;

            case SpellId.PortalTieRecall1: // primary portal tie recall

                if (targetPlayer != null && targetPlayer.LinkedPortalOneDID == null)
                {
                    // You must link to a portal to recall it!
                    targetPlayer.Session.Network.EnqueueSend(
                        new GameEventWeenieError(targetPlayer.Session, WeenieError.YouMustLinkToPortalToRecall)
                    );
                }
                else
                {
                    recall = PositionType.LinkedPortalOne;
                    if (targetPlayer != null)
                    {
                        recallDid = targetPlayer.LinkedPortalOneDID;
                    }
                }
                break;

            case SpellId.PortalTieRecall2: // secondary portal tie recall

                if (targetPlayer is { LinkedPortalTwoDID: null })
                {
                    // You must link to a portal to recall it!
                    targetPlayer.Session.Network.EnqueueSend(
                        new GameEventWeenieError(targetPlayer.Session, WeenieError.YouMustLinkToPortalToRecall)
                    );
                }
                else
                {
                    recall = PositionType.LinkedPortalTwo;
                    if (targetPlayer != null)
                    {
                        recallDid = targetPlayer.LinkedPortalTwoDID;
                    }
                }
                break;
        }

        if (recall != PositionType.Undef)
        {
            if (recallDid == null)
            {
                // lifestone recall
                var lifestoneRecall = new ActionChain();
                lifestoneRecall.AddAction(targetPlayer, () =>
                {
                    targetPlayer?.DoPreTeleportHide();
                });
                lifestoneRecall.AddDelaySeconds(2.0f); // 2 second delay
                lifestoneRecall.AddAction(targetPlayer, () => targetPlayer?.TeleToPosition(recall));
                lifestoneRecall.EnqueueChain();
            }
            else
            {
                // portal recall
                var portal = GetPortal(recallDid.Value);
                if (portal == null || portal.NoRecall)
                {
                    // You cannot recall that portal!
                    player?.Session.Network.EnqueueSend(
                        new GameEventWeenieError(player.Session, WeenieError.YouCannotRecallPortal)
                    );

                    return;
                }

                var result = portal.CheckUseRequirements(targetPlayer);
                if (!result.Success)
                {
                    if (result.Message != null)
                    {
                        targetPlayer.Session.Network.EnqueueSend(result.Message);
                    }

                    return;
                }

                var portalRecall = new ActionChain();
                portalRecall.AddAction(targetPlayer, () => targetPlayer.DoPreTeleportHide());
                portalRecall.AddDelaySeconds(2.0f); // 2 second delay
                portalRecall.AddAction(
                    targetPlayer,
                    () =>
                    {
                        // a capstone dungeon's entrance leaves it to its Portal emote to send the player anywhere
                        if (!portal.IsCapstoneEntrance)
                        {
                            var teleportDest = new Position(portal.Destination);
                            AdjustDungeon(teleportDest);

                            targetPlayer.Teleport(teleportDest);
                        }

                        portal.EmoteManager.OnPortal(player);
                    }
                );
                portalRecall.EnqueueChain();
            }
        }
    }

    /// <summary>
    /// Handles casting SpellType.PortalSummon spells
    /// </summary>
    private void HandleCastSpell_PortalSummon(Spell spell, Creature targetCreature, WorldObject itemCaster)
    {
        var player = this as Player;

        switch (player)
        {
            case { IsOlthoiPlayer: true }:
                player.Session.Network.EnqueueSend(
                    new GameEventWeenieError(player.Session, WeenieError.OlthoiCanOnlyRecallToLifestone)
                );
                return;
            case { PKTimerActive: true }:
                player.Session.Network.EnqueueSend(
                    new GameEventWeenieError(player.Session, WeenieError.YouHaveBeenInPKBattleTooRecently)
                );
                return;
        }

        var source = player ?? itemCaster;

        uint portalId;
        bool linkSummoned;

        // spell.link = 1 = LinkedPortalOneDID
        // spell.link = 2 = LinkedPortalTwoDID

        if (spell.Link <= 1)
        {
            portalId = source.LinkedPortalOneDID ?? 0;
            linkSummoned = source.GetProperty(PropertyBool.LinkedPortalOneSummon) ?? false;
        }
        else
        {
            portalId = source.LinkedPortalTwoDID ?? 0;
            linkSummoned = source.GetProperty(PropertyBool.LinkedPortalTwoSummon) ?? false;
        }

        Position summonLoc = null;

        if (player != null)
        {
            if (portalId == 0)
            {
                // You must link to a portal to summon it!
                player.Session.Network.EnqueueSend(
                    new GameEventWeenieError(player.Session, WeenieError.YouMustLinkToPortalToSummonIt)
                );
                return;
            }

            var summonPortal = GetPortal(portalId);
            if (
                summonPortal == null
                || summonPortal.NoSummon
                || (linkSummoned && !PropertyManager.GetBool("gateway_ties_summonable").Item)
            )
            {
                // You cannot summon that portal!
                player.Session.Network.EnqueueSend(
                    new GameEventWeenieError(player.Session, WeenieError.YouCannotSummonPortal)
                );
                return;
            }

            summonPortal.PlayerUsingTieOrSummonSpell = true;

            var result = summonPortal.CheckUseRequirements(player);
            if (!result.Success)
            {
                if (result.Message != null)
                {
                    player.Session.Network.EnqueueSend(result.Message);
                }

                return;
            }

            summonLoc = player.Location.InFrontOf(3.0f);
        }
        else if (itemCaster != null)
        {
            if (itemCaster.PortalSummonLoc != null)
            {
                summonLoc = new Position(itemCaster.PortalSummonLoc);
            }
            else
            {
                if (itemCaster.Location != null)
                {
                    summonLoc = itemCaster.Location.InFrontOf(3.0f);
                }
                else if (targetCreature is { Location: not null })
                {
                    summonLoc = targetCreature.Location.InFrontOf(3.0f);
                }
            }
        }

        if (summonLoc != null)
        {
            summonLoc.LandblockId = new LandblockId(summonLoc.GetCell());
        }

        var success = SummonPortal(portalId, summonLoc, spell.PortalLifetime, InstanceId);

        if (!success && player != null)
        {
            player.Session.Network.EnqueueSend(
                new GameEventWeenieError(player.Session, WeenieError.YouFailToSummonPortal)
            );
        }
    }

    /// <summary>
    /// Spawns a portal for SpellType.PortalSummon spells
    /// </summary>
    private static bool SummonPortal(uint portalId, Position location, double portalLifetime, uint instanceId)
    {
        var portal = GetPortal(portalId);

        if (portal == null || location == null)
        {
            return false;
        }

        if (WorldObjectFactory.CreateNewWorldObject("portalgateway") is not Portal gateway)
        {
            return false;
        }

        gateway.Location = new Position(location);
        gateway.OriginalPortal = portalId;

        gateway.UpdatePortalDestination(new Position(portal.Destination));

        gateway.TimeToRot = portalLifetime;

        gateway.MinLevel = portal.MinLevel;
        gateway.MaxLevel = portal.MaxLevel;
        gateway.PortalRestrictions = portal.PortalRestrictions;
        gateway.FellowshipRequired = portal.FellowshipRequired;
        gateway.AccountRequirements = portal.AccountRequirements;
        gateway.AdvocateQuest = portal.AdvocateQuest;

        gateway.Quest = portal.Quest;
        gateway.QuestRestriction = portal.QuestRestriction;

        gateway.Biota.PropertiesEmote = portal.Biota.PropertiesEmote;

        gateway.PortalRestrictions |= PortalBitmask.NoSummon; // all gateways are marked NoSummon but by default ruleset, the OriginalPortal is the one that is checked against

        // the gateway opens in the same instance as whoever summoned it
        gateway.InstanceId = instanceId;

        gateway.EnterWorld();

        return true;
    }

    /// <summary>
    /// Handles casting SpellType.PortalSending spells
    /// </summary>
    private static void HandleCastSpell_PortalSending(Spell spell, Creature targetCreature, WorldObject itemCaster)
    {
        if (targetCreature is Player targetPlayer)
        {
            if (targetPlayer.PKTimerActive)
            {
                targetPlayer.Session.Network.EnqueueSend(
                    new GameEventWeenieError(targetPlayer.Session, WeenieError.YouHaveBeenInPKBattleTooRecently)
                );
                return;
            }

            var portalSendingChain = new ActionChain();
            portalSendingChain.AddAction(targetPlayer, () => targetPlayer.DoPreTeleportHide());
            portalSendingChain.AddAction(
                targetPlayer,
                () =>
                {
                    var teleportDest = new Position(spell.Position);
                    AdjustDungeon(teleportDest);

                    targetPlayer.Teleport(teleportDest);

                    targetPlayer.SendTeleportedViaMagicMessage(itemCaster, spell);
                }
            );
            portalSendingChain.EnqueueChain();
        }
        else if (targetCreature != null)
        {
            // monsters can cast some portal spells on themselves too, possibly?
            // under certain circumstances, such as ensuring the destination is the same landblock
            var teleportDest = new Position(spell.Position);
            AdjustDungeon(teleportDest);

            targetCreature.FakeTeleport(teleportDest);
        }
    }

    /// <summary>
    /// Handles casting SpellType.FellowPortalSending spells
    /// </summary>
    private void HandleCastSpell_FellowPortalSending(Spell spell, Creature targetCreature, WorldObject itemCaster)
    {
        var creature = this as Creature;

        if (targetCreature is not Player targetPlayer || targetPlayer.Fellowship == null)
        {
            return;
        }

        if (targetPlayer.PKTimerActive)
        {
            targetPlayer.Session.Network.EnqueueSend(
                new GameEventWeenieError(targetPlayer.Session, WeenieError.YouHaveBeenInPKBattleTooRecently)
            );
            return;
        }

        if (creature != null)
        {
            var distanceToTarget = creature.GetDistance(targetPlayer);
            var skill = creature.GetCreatureSkill(spell.School);
            var magicSkill = skill.InitLevel + skill.Ranks; // synced with acclient DetermineSpellRange -> InqSkillLevel

            var maxRange = spell.BaseRangeConstant + magicSkill * spell.BaseRangeMod;
            if (maxRange == 0.0f)
            {
                maxRange = float.PositiveInfinity;
            }

            if (distanceToTarget > maxRange)
            {
                return;
            }
        }

        var portalSendingChain = new ActionChain();
        portalSendingChain.AddAction(targetPlayer, () => targetPlayer.DoPreTeleportHide());
        portalSendingChain.AddAction(
            targetPlayer,
            () =>
            {
                var teleportDest = new Position(spell.Position);
                AdjustDungeon(teleportDest);

                targetPlayer.Teleport(teleportDest);

                targetPlayer.SendTeleportedViaMagicMessage(itemCaster, spell);
            }
        );
        portalSendingChain.EnqueueChain();
    }
}
