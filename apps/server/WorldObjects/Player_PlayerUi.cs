using ACE.Entity;
using ACE.Server.Entity.PlayerUi;

namespace ACE.Server.WorldObjects;

public partial class Player
{
    /// <summary>
    /// The Daralet plugin shows this player's bank in a window of its own (@playerui bankwindow on): opening a bank then
    /// leaves out the messages that open the game's bank window (Storage.SendBankVaultInventory), and tells the plugin the
    /// bank opened and closed instead. Not saved: the plugin says which it wants each time it starts.
    /// </summary>
    public bool PlayerUiBankWindow { get; set; }

    /// <summary>
    /// The same for corpses and chests (@playerui lootwindow on): opening one leaves out the game's window for it
    /// (Container.SendInventory), and the plugin's Corpse window lists everything in it instead.
    /// </summary>
    public bool PlayerUiLootWindow { get; set; }

    /// <summary>
    /// The container this player last opened with its game window left out, which the client will never say it stopped
    /// looking at. Kept rather than worked out from the settings above: turning one off with a container open would otherwise
    /// leave that container held open from anywhere, with no one else able to use it.
    /// </summary>
    public ObjectGuid PlayerUiOpenedContainer { get; set; } = ObjectGuid.Invalid;

    /// <summary>
    /// Whether this container's game window is left out for the plugin's, for this player.
    /// </summary>
    public bool UsesPlayerUiWindow(Container container)
    {
        if (container is Storage)
        {
            return PlayerUiBankWindow;
        }

        return PlayerUiLootWindow && (container is Corpse || PlayerUiCorpseData.IsLootableContainer(container));
    }

    /// <summary>
    /// Closes a container the player opened in the plugin's window and walked away from. The game closes one when its
    /// client says it's stopped looking (NoLongerViewingContents), which the client only does for a container it opened a
    /// window for: without this, it would stay open, and no one else could use it. From the heartbeat.
    /// </summary>
    private void ClosePlayerUiContainerOutOfReach()
    {
        if (PlayerUiOpenedContainer == ObjectGuid.Invalid)
        {
            return;
        }

        // something else has been opened since, which closed this one
        if (PlayerUiOpenedContainer != LastOpenedContainerId)
        {
            PlayerUiOpenedContainer = ObjectGuid.Invalid;
            return;
        }

        if (CurrentLandblock?.GetObject(PlayerUiOpenedContainer) is not Container container)
        {
            return;
        }

        if (!container.IsOpen || container.Viewer != Guid.Full)
        {
            PlayerUiOpenedContainer = ObjectGuid.Invalid;
            return;
        }

        if (!CurrentLandblock.WithinUseRadius(this, container.Guid, out _))
        {
            container.Close(this);
            PlayerUiOpenedContainer = ObjectGuid.Invalid;
        }
    }
}
