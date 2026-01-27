using System;
using System.Linq;
using System.Text;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands.WorldObjectCommands;

public class TextureMapDump
{
    [CommandHandler(
        "texturemapdump",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Lists the SurfaceTextures (0x05), their Textures (0x06), and any Surfaces (0x08) mappings " +
        "for the target object's WCID, based on the database (weenie_properties_texture_map / weenie_properties_surface_map), " +
        "not the object's Setup."
    )]
    public static void HandleTextureMapDump(Session session, params string[] parameters)
    {
        var target = CommandHandlerHelper.GetLastAppraisedObject(session);

        if (target == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat("No last appraised object found.", ChatMessageType.System)
            );
            return;
        }

        var wcid = target.WeenieClassId;

        var db = DatabaseManager.World;
        var weenie = db.GetWeenie(wcid);
        if (weenie == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"No weenie found for WCID {wcid}.",
                    ChatMessageType.System
                )
            );
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Texture / Surface map dump for WCID {wcid} ({target.Name})");
        sb.AppendLine("(from database: weenie_properties_texture_map, weenie_properties_surface_map)");
        sb.AppendLine();

        // --- Surface map section (0x08) ---
        if (weenie.WeeniePropertiesSurfaceMap != null && weenie.WeeniePropertiesSurfaceMap.Count > 0)
        {
            sb.AppendLine("Surface map (0x08) overrides: weenie_properties_surface_map");
            foreach (var map in weenie.WeeniePropertiesSurfaceMap.OrderBy(m => m.Index))
            {
                sb.AppendLine($"Index: {map.Index}");
                sb.AppendLine($"  Surface (0x08) OldId: 0x{map.OldId:X8} ({map.OldId})");
                sb.AppendLine($"  Surface (0x08) NewId: 0x{map.NewId:X8} ({map.NewId})");
                sb.AppendLine();
            }

            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("No entries in weenie_properties_surface_map for this weenie.");
            sb.AppendLine();
        }

        // --- Texture map section (0x05 / 0x06) ---
        if (weenie.WeeniePropertiesTextureMap == null || weenie.WeeniePropertiesTextureMap.Count == 0)
        {
            sb.AppendLine("No entries in weenie_properties_texture_map for this weenie.");
        }
        else
        {
            sb.AppendLine("Texture map (0x05/0x06) overrides: weenie_properties_texture_map");
            sb.AppendLine("NOTE: OldId/NewId are SurfaceTexture (0x05) DIDs.");
            sb.AppendLine("      Below each SurfaceTexture we list its Textures (0x06).");
            sb.AppendLine();

            foreach (var map in weenie.WeeniePropertiesTextureMap.OrderBy(m => m.Index))
            {
                sb.AppendLine($"Index: {map.Index}");
                sb.AppendLine($"  SurfaceTexture (0x05) OldId: 0x{map.OldId:X8} ({map.OldId})");
                sb.AppendLine($"  SurfaceTexture (0x05) NewId: 0x{map.NewId:X8} ({map.NewId})");

                DescribeSurfaceTexture(sb, "Old", map.OldId);
                DescribeSurfaceTexture(sb, "New", map.NewId);

                sb.AppendLine();
            }
        }

        session.Network.EnqueueSend(
            new GameMessageSystemChat(sb.ToString(), ChatMessageType.System)
        );
    }

    private static void DescribeSurfaceTexture(StringBuilder sb, string label, uint surfaceTextureDid)
    {
        sb.AppendLine($"  [{label}] SurfaceTexture (0x05):");

        if (surfaceTextureDid == 0)
        {
            sb.AppendLine("    <DID is 0, nothing to describe>");
            return;
        }

        SurfaceTexture st = null;

        try
        {
            st = DatManager.PortalDat.ReadFromDat<SurfaceTexture>(surfaceTextureDid);
        }
        catch (Exception ex)
        {
            sb.AppendLine($"    <failed to read SurfaceTexture from DAT: {ex.Message}>");
        }

        if (st == null)
        {
            sb.AppendLine("    <SurfaceTexture not found in client_portal.dat>");
            return;
        }

        sb.AppendLine($"    SurfaceTexture DID: 0x{st.Id:X8} ({st.Id})");
        sb.AppendLine($"    Textures (0x06) count: {st.Textures.Count}");

        if (st.Textures.Count == 0)
        {
            sb.AppendLine("    Textures (0x06): <none>");
        }
        else
        {
            sb.AppendLine("    Textures (0x06):");
            foreach (var texId in st.Textures)
            {
                sb.AppendLine($"    - Texture DID: 0x{texId:X8} ({texId})");

                try
                {
                    var tex = DatManager.PortalDat.ReadFromDat<Texture>(texId);
                    if (tex != null)
                    {
                        sb.AppendLine(
                            $"      Size: {tex.Width}x{tex.Height}, " +
                            $"Format: {tex.Format}, Length: {tex.Length}"
                        );
                    }
                    else
                    {
                        sb.AppendLine("      <texture not found in client_portal.dat>");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"      <failed to read Texture from DAT: {ex.Message}>");
                }
            }
        }
    }
}
