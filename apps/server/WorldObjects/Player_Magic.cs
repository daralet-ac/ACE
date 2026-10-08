using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Player
{
    // TODO: get rid of this, only used for determining if TurnTo is required
    public enum TargetCategory
    {
        Undef,
        WorldObject,
        Wielded,
        Inventory,
        Self,
        Fellowship
    }

    public MagicState MagicState;

    /// <summary>
    /// The last spell projectile launched by this player
    /// to successfully collided with a target
    /// </summary>
    public Spell LastHitSpellProjectile;

    /// <summary>
    /// Limiter for switching between war and void magic
    /// </summary>
    public double LastSuccessCast_Time;

    public bool DebugSpell { get; set; }

    public bool DebugSpellcasting { get; set; }

    public string DebugDamageBuffer { get; set; }

    public RecordCast RecordCast { get; set; }

    /// <summary>
    /// Returns the magic skill associated with the magic school
    /// for the last collided spell projectile
    /// </summary>
    private Skill GetCurrentMagicSkill()
    {
        if (LastHitSpellProjectile == null)
        {
            return Skill.WarMagic; // this should never happen, but just in case
        }

        switch (LastHitSpellProjectile.School)
        {
            case MagicSchool.WarMagic:
            default:
                return Skill.WarMagic;
            case MagicSchool.LifeMagic:
                return Skill.LifeMagic;
            case MagicSchool.CreatureEnchantment:
                return Skill.CreatureEnchantment;
            case MagicSchool.PortalMagic:
                return Skill.PortalMagic;
            case MagicSchool.VoidMagic:
                return Skill.VoidMagic;
        }
    }

    /// <summary>
    /// The checks every cast request goes through first: magic combat mode, magic stance, jumping, PK logout and busy.
    /// A request made while busy with a cast may be queued instead.
    /// Returns FALSE if the cast can't start now.
    /// </summary>
    private bool TryAcceptCastRequest(CastQueueType castType, uint targetGuid, uint spellId, WorldObject casterItem)
    {
        if (CombatMode != CombatMode.Magic)
        {
            var request =
                castType == CastQueueType.Targeted
                    ? $"HandleActionCastTargetedSpell({targetGuid:X8}, {spellId}, {casterItem?.Name})"
                    : $"HandleActionMagicCastUnTargetedSpell({spellId})";

            _log.Error($"{Name}.{request} - CombatMode mismatch {CombatMode}, LastCombatMode: {LastCombatMode}");

            if (LastCombatMode == CombatMode.Magic)
            {
                CombatMode = CombatMode.Magic;
            }
            else
            {
                SendUseDoneEvent();
                return false;
            }
        }

        if (
            FastTick
            && PhysicsObj.MovementManager.MotionInterpreter.InterpretedState.CurrentStyle != (uint)MotionStance.Magic
        )
        {
            _log.Warning(
                $"{Name} CombatMode: {CombatMode}, CurrentMotionState: {CurrentMotionState.Stance}.{CurrentMotionState.MotionState.ForwardCommand}, Physics: {(MotionStance)PhysicsObj.MovementManager.MotionInterpreter.InterpretedState.CurrentStyle}.{(MotionCommand)PhysicsObj.MovementManager.MotionInterpreter.InterpretedState.ForwardCommand}"
            );
            ApplyPhysicsMotion(new Motion(MotionStance.Magic));
            SendUseDoneEvent(WeenieError.YoureTooBusy);
            return false;
        }

        if (IsJumping)
        {
            SendUseDoneEvent(WeenieError.YouCantDoThatWhileInTheAir);
            return false;
        }

        if (PKLogout)
        {
            SendUseDoneEvent(WeenieError.YouHaveBeenInPKBattleTooRecently);
            return false;
        }

        if (IsBusy && MagicState.CanQueue)
        {
            MagicState.CastQueue = new CastQueue(castType, targetGuid, spellId, casterItem);
            MagicState.CanQueue = false;
            return false;
        }

        return VerifyBusy();
    }

    /// <summary>
    /// Handles player targeted casting message
    /// </summary>
    /// <param name="casterItem">The casting item, when casting one of its built-in spells</param>
    public void HandleActionCastTargetedSpell(uint targetGuid, uint spellId, WorldObject casterItem = null)
    {
        if (!TryAcceptCastRequest(CastQueueType.Targeted, targetGuid, spellId, casterItem))
        {
            return;
        }

        // verify spell is contained in player's spellbook,
        // or in the weapon's spellbook in the case of built-in spells
        if (!VerifySpell(spellId, casterItem))
        {
            SendUseDoneEvent(WeenieError.MagicInvalidSpellType);
            return;
        }

        var targetCategory = GetTargetCategory(targetGuid, spellId, out var target);

        if (target == null || target.Teleporting)
        {
            SendUseDoneEvent(WeenieError.TargetNotAcquired);
            return;
        }

        MagicState.OnCastStart();
        MagicState.SetWindupParams(targetGuid, spellId, casterItem);

        StartPos = new Physics.Common.Position(PhysicsObj.Position);

        if (RecordCast.Enabled)
        {
            RecordCast.OnCastTargetedSpell(new Spell(spellId), target);
        }

        if (targetCategory != TargetCategory.WorldObject && targetCategory != TargetCategory.Wielded)
        {
            if (!CreatePlayerSpell(target, targetCategory, spellId, casterItem))
            {
                MagicState.OnCastDone();
            }

            return;
        }

        // start turning
        if (!FastTick)
        {
            var rotateTarget = target;
            if (rotateTarget.WielderId != null)
            {
                rotateTarget = CurrentLandblock?.GetObject(rotateTarget.WielderId.Value);
            }

            var rotateTime = Rotate(rotateTarget);
            var actionChain = new ActionChain();
            actionChain.AddDelaySeconds(rotateTime);

            actionChain.AddAction(
                this,
                () =>
                {
                    // ensure target still exists
                    targetCategory = GetTargetCategory(targetGuid, spellId, out target);

                    if (target == null)
                    {
                        SendUseDoneEvent(WeenieError.TargetNotAcquired);
                        MagicState.OnCastDone();
                        return;
                    }

                    if (!CreatePlayerSpell(target, targetCategory, spellId, casterItem))
                    {
                        MagicState.OnCastDone();
                    }
                }
            );

            actionChain.EnqueueChain();
        }
        else
        {
            TurnTo_Magic(target);
        }
    }

    private TargetCategory GetTargetCategory(uint targetGuid, uint spellId, out WorldObject target)
    {
        // fellowship spell
        var spell = new Spell(spellId);
        if (spell.IsFellowshipSpell)
        {
            target = this;
            return TargetCategory.Fellowship;
        }

        // direct landblock object
        target = CurrentLandblock?.GetObject(targetGuid);

        if (target != null)
        {
            return targetGuid == Guid.Full ? TargetCategory.Self : TargetCategory.WorldObject;
        }

        // self-wielded
        target = GetEquippedItem(targetGuid);
        if (target != null)
        {
            return TargetCategory.Inventory;
        }

        // inventory item
        target = GetInventoryItem(targetGuid);
        if (target != null)
        {
            return TargetCategory.Inventory;
        }

        // other selectable wielded
        target = CurrentLandblock?.GetWieldedObject(targetGuid, true);
        if (target != null)
        {
            return TargetCategory.Wielded;
        }

        // known trade objects
        var tradePartner = GetKnownTradeObj(new ObjectGuid(targetGuid));
        if (tradePartner != null)
        {
            target = tradePartner.GetEquippedItem(targetGuid);
            if (target != null)
            {
                return TargetCategory.Wielded;
            }

            target = tradePartner.GetInventoryItem(targetGuid);
            if (target != null)
            {
                return TargetCategory.Inventory;
            }
        }

        return TargetCategory.Undef;
    }

    /// <summary>
    /// Handles player untargeted casting message
    /// </summary>
    public void HandleActionMagicCastUnTargetedSpell(uint spellId)
    {
        if (!TryAcceptCastRequest(CastQueueType.Untargeted, 0, spellId, null))
        {
            return;
        }

        // verify spell is contained in player's spellbook,
        // or in the weapon's spellbook in the case of built-in spells
        if (!VerifySpell(spellId))
        {
            return;
        }

        if (RecordCast.Enabled)
        {
            RecordCast.OnCastUntargetedSpell(new Spell(spellId));
        }

        MagicState.OnCastStart();

        StartPos = new Physics.Common.Position(PhysicsObj.Position);

        if (!CreatePlayerSpell(spellId))
        {
            MagicState.OnCastDone();
        }
    }

    public enum CastingPreCheckStatus
    {
        CastFailed,
        InvalidPKStatus,
        Success
    }

    private const float Windup_MaxMove = 6.0f;

    public Physics.Common.Position StartPos { get; set; }

    private void HandleCastQueue()
    {
        MagicState.CanQueue = false;

        if (MagicState.CastQueue != null)
        {
            if (MagicState.CastQueue.Type == CastQueueType.Targeted)
            {
                HandleActionCastTargetedSpell(
                    MagicState.CastQueue.TargetGuid,
                    MagicState.CastQueue.SpellId,
                    MagicState.CastQueue.CasterItem
                );
            }
            else
            {
                HandleActionMagicCastUnTargetedSpell(MagicState.CastQueue.SpellId);
            }
        }
    }

    /// <summary>
    /// Sends a chat message with respect to SquelchManager
    /// </summary>
    public void SendChatMessage(WorldObject source, string msg, ChatMessageType msgType)
    {
        if (!SquelchManager.Squelches.Contains(source, msgType))
        {
            Session.Network.EnqueueSend(new GameMessageSystemChat(msg, msgType));
        }
    }
}
