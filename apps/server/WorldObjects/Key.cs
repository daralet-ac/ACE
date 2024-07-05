using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using Serilog;

namespace ACE.Server.WorldObjects;

public class Key : WorldObject
{
    private readonly ILogger _log = Log.ForContext<Key>();

    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public Key(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public Key(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues()
    {
        // These shoudl come from the weenie. After confirmation, remove these
        //KeyCode = AceObject.KeyCode ?? "";
        //Structure = AceObject.Structure ?? AceObject.MaxStructure;

        if (Structure > MaxStructure)
        {
            Structure = MaxStructure;
        }
    }

    public string KeyCode
    {
        get => GetProperty(PropertyString.KeyCode);
        set
        {
            if (value == null)
            {
                RemoveProperty(PropertyString.KeyCode);
            }
            else
            {
                SetProperty(PropertyString.KeyCode, value);
            }
        }
    }

    public bool OpensAnyLock
    {
        get => GetProperty(PropertyBool.OpensAnyLock) ?? false;
        set
        {
            if (!value)
            {
                RemoveProperty(PropertyBool.OpensAnyLock);
            }
            else
            {
                SetProperty(PropertyBool.OpensAnyLock, value);
            }
        }
    }

    public override void HandleActionUseOnTarget(Player player, WorldObject target)
    {
        // verify use requirements
        var result = CheckUseRequirements(player);

        if (!result.Success)
        {
            if (result.Message != null && player != null)
            {
                player.Session.Network.EnqueueSend(result.Message);
            }

            player.SendUseDoneEvent();
            return;
        }

        if (Structure == 0 || Structure > MaxStructure)
        {
            _log.Warning(
                $"Key.HandleActionUseOnTarget: Structure / MaxStructure is {Structure:N0} / {MaxStructure:N0} for {Name} (0x{Guid}:{WeenieClassId}), used on {target.Name} (0x{target.Guid}:{target.WeenieClassId}) and used by {player.Name} (0x{player.Guid})"
            );

            var wo = player.FindObject(
                Guid.Full,
                Player.SearchLocations.Everywhere,
                out _,
                out var rootOwner,
                out var wasEquipped
            );
            DeleteObject(rootOwner);

            player.SendUseDoneEvent(WeenieError.YouCannotUseThatItem);
            return;
        }

        UnlockerHelper.UseUnlocker(player, this, target);
    }
}
