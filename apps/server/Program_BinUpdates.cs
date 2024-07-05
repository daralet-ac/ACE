using System;
using System.Text.Json;
using Serilog;

namespace ACE.Server;

partial class Program
{
    private static void CheckForServerUpdate()
    {
        _log.Information("Automatic Server version check started...");
        try
        {
            var worldDb = new Database.WorldDatabase();
            var currentVersion = worldDb.GetVersion();
            _log.Information("Current Server Binary: {ServerFullVersion}", ServerBuildInfo.FullVersion);

            var url = "https://api.github.com/repos/ACEmulator/ACE/releases/latest";
            using var client = new WebClient();
            var html = client.GetStringFromURL(url).Result;

            var json = JsonSerializer.Deserialize<JsonElement>(html);

            var tag = json.GetProperty("tag_name").GetString();

            //Split the tag from "v{version}.{build}" into discrete components  - "tag_name": "v1.39.4192"
            var v = new Version(tag.Remove(0, 1));
            var currentServerVersion = ServerBuildInfo.GetServerVersion();

            var versionStatus = v.CompareTo(currentServerVersion);
            // Status returns > 0 if the GitHub version is newer. (0 if the same, or < 0 if older.)
            if (versionStatus > 0)
            {
                _log.Warning("There is a newer version of ACE available!");
                _log.Warning($"Please visit {json.GetProperty("html_url").GetString()} for more information.");

                // the Console.Title.Get() only works on Windows...
#pragma warning disable CA1416 // Validate platform compatibility
                Console.Title += " -- Server Binary Update Available";
#pragma warning restore CA1416 // Validate platform compatibility
            }
            else
            {
                _log.Information("Latest Server Version is {ServerVersion} -- No Update Required!", tag);
            }
            return;
        }
        catch (Exception ex)
        {
            _log.Information(ex, "Unable to continue with Automatic Server Version Check");
        }
    }
}
