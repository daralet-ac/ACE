using ACE.Server.Network;

namespace ACE.Server.WorldObjects;

partial class Player
{
    /// <summary>
    /// A fake player an admin made for testing (FakePlayerManager): it has no client, is never saved, and is taken out of
    /// the world instead of logging out
    /// </summary>
    public bool IsFakePlayer { get; private set; }

    /// <summary>
    /// Makes a newly built player a fake one, on a session with no client behind it
    /// </summary>
    public void MakeFake(Session headlessSession)
    {
        IsFakePlayer = true;
        Session = headlessSession;
        headlessSession.SetPlayer(this);
    }
}
