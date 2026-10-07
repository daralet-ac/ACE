using ACE.Server.Managers;

namespace ACE.Server.Network.GameMessages.Messages;

public class GameMessageDDDInterrogation : GameMessage
{
    public GameMessageDDDInterrogation()
        : base(GameMessageOpcode.DDD_Interrogation, GameMessageGroup.DatabaseQueue, 28)
    {
        // 0x4 tells the client it may use client_highres.dat
        var productID = 0x1u;
        if (PropertyManager.GetBool("allow_highres_dat").Item)
        {
            productID |= 0x4;
        }

        Writer.Write(1u); // m_dwServersRegion
        Writer.Write(1u); // m_NameRuleLanguage
        Writer.Write(productID); // m_dwProductID
        Writer.Write(2u); // m_SupportedLanguages.Count
        Writer.Write(0u); // Invalid
        Writer.Write(1u); // English
    }
}
