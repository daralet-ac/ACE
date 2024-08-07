using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ACE.Common;
using Serilog;

namespace ACE.Server.Network.Managers;

public static class SocketManager
{
    private static readonly ILogger _log = Log.ForContext(typeof(SocketManager));

    private static ConnectionListener[] listeners;

    public static void Initialize()
    {
        var hosts = new List<IPAddress>();

        try
        {
            var splits = ConfigManager.Config.Server.Network.Host.Split(",");

            foreach (var split in splits)
            {
                hosts.Add(IPAddress.Parse(split));
            }
        }
        catch (Exception ex)
        {
            _log.Error(
                ex,
                $"Unable to use {ConfigManager.Config.Server.Network.Host} as host. Using IPAddress.Any as host instead."
            );
            hosts.Clear();
            hosts.Add(IPAddress.Any);
        }

        listeners = new ConnectionListener[hosts.Count * 2];

        for (var i = 0; i < hosts.Count; i++)
        {
            listeners[(i * 2) + 0] = new ConnectionListener(hosts[i], ConfigManager.Config.Server.Network.Port);
            _log.Information($"Binding ConnectionListener to {hosts[i]}:{ConfigManager.Config.Server.Network.Port}");

            listeners[(i * 2) + 1] = new ConnectionListener(hosts[i], ConfigManager.Config.Server.Network.Port + 1);
            _log.Information(
                $"Binding ConnectionListener to {hosts[i]}:{ConfigManager.Config.Server.Network.Port + 1}"
            );

            listeners[(i * 2) + 1].Start();
            listeners[(i * 2) + 0].Start();
        }
    }

    /// <summary>
    /// Given a ConnectionListener, return its matched ConnectionListener.
    /// <para>C2S ConnectionListener returns S2C ConnectionListener</para>
    /// <para>S2C ConnectionListener returns C2S ConnectionListener</para>
    /// </summary>
    public static ConnectionListener GetMatchedConnectionListener(ConnectionListener connectionListener) =>
        listeners
            .Where(c =>
                c.ListenerEndpoint.Address == connectionListener.ListenerEndpoint.Address
                && c.ListenerEndpoint.Port != connectionListener.ListenerEndpoint.Port
            )
            .FirstOrDefault();
}
