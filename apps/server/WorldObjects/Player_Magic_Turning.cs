using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.DatLoader;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Player
{
    // 20 from MoveToManager threshold?
    public const float MaxAngle = 5;

    public bool IsWithinAngle(WorldObject target)
    {
        // TODO: investigate this more, difference for GetAngle() between ACE and ac physics engine
        var angle = 0.0f;
        if (target != this)
        {
            if (target.CurrentLandblock == null)
            {
                FindObject(target.Guid.Full, SearchLocations.Everywhere, out _, out var rootOwner, out _);

                if (rootOwner == null)
                {
                    _log.Error($"{Name}.IsWithinAngle({target.Name} ({target.Guid})) - couldn't find rootOwner");
                }
                else if (rootOwner != this)
                {
                    angle = GetAngle(rootOwner);
                }
            }
            else
            {
                angle = GetAngle(target);
            }
        }

        //Console.WriteLine($"Angle: " + angle);
        var maxAngle = PropertyManager.GetDouble("spellcast_max_angle").Item;

        if (RecordCast.Enabled)
        {
            RecordCast.Log($"DoCastSpell(angle={angle} vs. {maxAngle})");
        }

        return angle <= maxAngle;
    }

    public WorldObject TurnTarget;

    public void TurnTo_Magic(WorldObject target)
    {
        //Console.WriteLine($"{Name}.TurnTo_Magic()");
        TurnTarget = target;

        MagicState.TurnStarted = true;
        MagicState.IsTurning = true;

        if (FastTick)
        {
            if (PropertyManager.GetDouble("spellcast_max_angle").Item > 5.0f && IsWithinAngle(target))
            {
                // emulate current gdle TurnTo - doesn't match retail, but some players may prefer this
                OnMoveComplete_Magic(WeenieError.None);
                return;
            }

            // verify cast radius before every automatic TurnTo after windup
            if (!VerifyCastRadius())
            {
                return;
            }

            var stopCompletely = !MagicState.CastMotionDone;
            //var stopCompletely = true;

            CreateTurnToChain2(target, null, null, stopCompletely, MagicState.AlwaysTurn);

            MagicState.AlwaysTurn = false;
        }
    }

    public void HandleMotionDone_Magic(uint motionID, bool success)
    {
        //Console.WriteLine($"HandleMotionDone_Magic({(MotionCommand)motionID}, {success})");

        if (!FastTick || !MagicState.IsCasting)
        {
            return;
        }

        if (motionID == (uint)MagicState.CastGesture)
        {
            if (RecordCast.Enabled)
            {
                RecordCast.Log(
                    $"{Name}.HandleMotionDone_Magic({(MotionCommand)motionID}, {success}) - cast gesture done"
                );
            }

            MagicState.CastMotionDone = true;

            var actionChain = new ActionChain();
            actionChain.AddDelayForOneTick();
            actionChain.AddAction(
                this,
                () =>
                {
                    if (!MagicState.IsCasting)
                    {
                        return;
                    }

                    MagicState.AlwaysTurn = true;

                    DoCastSpell(MagicState);
                }
            );
            actionChain.EnqueueChain();
        }
    }

    public void OnMoveComplete_Magic(WeenieError status)
    {
        //Console.WriteLine($"OnMoveComplete_Magic({status})");

        if (!FastTick || !MagicState.IsCasting || !MagicState.TurnStarted)
        {
            return;
        }

        // this occurs after the player is done turning
        // before the windup, or after the first half of the cast motion
        // either completed or cancelled

        if (RecordCast.Enabled)
        {
            RecordCast.Log($"{Name}.OnMoveComplete_Magic({status}) - DoCastSpell");
        }

        MagicState.IsTurning = false;

        var checkAngle = status != WeenieError.None;

        var actionChain = new ActionChain();
        actionChain.AddDelayForOneTick();
        actionChain.AddAction(
            this,
            () =>
            {
                if (!MagicState.IsCasting)
                {
                    return;
                }

                if (!MagicState.CastMotionDone)
                {
                    DoWindup(MagicState.WindupParams, checkAngle);
                }
                else
                {
                    DoCastSpell(MagicState, checkAngle);
                }
            }
        );
        actionChain.EnqueueChain();
    }

    public void OnTurnRelease()
    {
        MagicState.PendingTurnRelease = false;

        if (!MagicState.CastMotionDone)
        {
            DoWindup(MagicState.WindupParams, true);
        }
        else
        {
            DoCastSpell(MagicState, true);
        }
    }

    public bool VerifyCastRadius()
    {
        if (MagicState.CastGestureStartTime != DateTime.MinValue)
        {
            var dist = StartPos.Distance(PhysicsObj.Position);

            if (dist > Windup_MaxMove && PlayerKillerStatus != PlayerKillerStatus.NPK)
            {
                FailCast();
                return false;
            }
        }
        return true;
    }

    public void CheckTurn()
    {
        // verify cast radius while manually moving after windup
        if (!VerifyCastRadius())
        {
            return;
        }

        if (TurnTarget != null && IsWithinAngle(TurnTarget))
        {
            if (MagicState.PendingTurnRelease)
            {
                OnTurnRelease();
            }
            else
            {
                PhysicsObj.StopCompletely(false);
            }
        }
    }
}
