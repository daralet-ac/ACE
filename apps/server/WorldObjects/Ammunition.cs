using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

public class Ammunition : Stackable
{
    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public Ammunition(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public Ammunition(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues() { }

    public override void OnCollideObject(WorldObject target)
    {
        ProjectileCollisionHelper.OnCollideObject(this, target);
    }

    public override void OnCollideEnvironment()
    {
        ProjectileCollisionHelper.OnCollideEnvironment(this);
    }

    public override void ActOnUse(WorldObject wo)
    {
        // Do nothing
    }

    public static void HandleAmmoSharpening(Player player, WorldObject source, WorldObject target)
    {
        var targetAmmo = target as Ammunition;
        if (targetAmmo is null)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{target.Name} is not a type of ammunition.",
                    ChatMessageType.Craft
                )
            );

            return;
        }

        var playerskill = (int)player.GetCreatureSkill((Skill.Woodworking)).Current;
        var difficulty = GetSharpenDifficulty(target.WieldDifficulty ?? 0);

        if (playerskill < difficulty)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You are not skilled enough with Woodworking to sharpen the {target.Name}.",
                    ChatMessageType.Craft
                )
            );
        }
        else
        {
            var maxUses = target.AmmoType is ACE.Entity.Enum.AmmoType.Arrow ? 1000 : 500;

            target.SetProperty(PropertyInt.AmmoEffect, (int)WorldObjects.AmmoEffect.Sharpened);
            target.SetProperty(PropertyInt.AmmoEffectUsesRemaining, maxUses);
        }
    }

    private static int GetSharpenDifficulty(int targetWieldDifficulty)
    {
        switch (targetWieldDifficulty)
        {
            case 270: return 175;
            case 250: return 150;
            case 230: return 130;
            case 215: return 115;
            case 200: return 100;
            case 175: return 75;
            case 125: return 25;
            default: return 0;
        }
    }

    public int? AmmoEffectUsesRemaining
    {
        get => GetProperty(PropertyInt.AmmoEffectUsesRemaining);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyInt.AmmoEffectUsesRemaining);
            }
            else
            {
                SetProperty(PropertyInt.AmmoEffectUsesRemaining, value.Value);
            }
        }
    }

    public int? AmmoEffect
    {
        get => GetProperty(PropertyInt.AmmoEffect);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyInt.AmmoEffect);
            }
            else
            {
                SetProperty(PropertyInt.AmmoEffect, value.Value);
            }
        }
    }
}

public enum AmmoEffect
{
    Sharpened = 0
}
