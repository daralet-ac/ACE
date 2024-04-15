using System.Threading;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord;
using Serilog.Events;
using Serilog;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Timers;
using ACE.Common;
using ACE.Server.Managers;
using Timer = System.Timers.Timer;

namespace ACE.Server.Discord;

public class DiscordBot
{
    private readonly ILogger _log = Log.ForContext<DiscordBot>();
    private IConfiguration _configuration;
    private IServiceProvider _services;
    private DiscordSocketClient _client;

    private readonly DiscordSocketConfig _socketConfig = new()
    {
        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
        AlwaysDownloadUsers = true,
    };

    public async Task Initialize(IConfiguration configuration)
    {
        _configuration = configuration;

        _services = new ServiceCollection()
            .AddSingleton(_configuration)
            .AddSingleton(_socketConfig)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<InteractionHandler>()
            .BuildServiceProvider();

        _client = _services.GetRequiredService<DiscordSocketClient>();

        _client.Log += LogAsync;

        await _services.GetRequiredService<InteractionHandler>()
            .InitializeAsync();

        _client.Ready += CreatePopulationChannelUpdateTimer;
        _client.Ready += CreateDayNightCycleChannelUpdateTimer;

        await _client.LoginAsync(TokenType.Bot, _configuration.GetValue<string>("Token"));
        await _client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private Task CreatePopulationChannelUpdateTimer()
    {
        var updateInterval = _configuration.GetSection("StatChannels").GetSection("Population").GetValue<double>("UpdateInterval");

        var timer = new Timer
        {
            AutoReset = true,
            Interval = updateInterval
        };

        timer.Elapsed += DoPopulationChannelUpdate;

        timer.Start();

        return Task.CompletedTask;
    }

    private async void DoPopulationChannelUpdate(object sender, ElapsedEventArgs e)
    {
        var populationChannelId = _configuration.GetSection("StatChannels").GetSection("Population").GetValue<ulong>("ChannelId");
        if (await _client.GetChannelAsync(populationChannelId) is not IVoiceChannel channel) return;

        var oldChannelName = channel.Name;
        var newChannelName = $"Population: \ud83d\udfe2 {PlayerManager.GetOnlineCount()}";
        if (string.Equals(oldChannelName, newChannelName)) return;

        _log.Information("Updating population channel name. Old: {OldPopulationChannelName} | New: {NewPopulationChannelName}", oldChannelName, newChannelName);

        try
        {
            await channel.ModifyAsync(prop => prop.Name = newChannelName);
            _log.Information("Population channel name updated.");
        }
        catch (Exception ex)
        {
            _log.Error(ex, ex.Message);
        }
    }

    private Task CreateDayNightCycleChannelUpdateTimer()
    {
        var updateInterval = _configuration.GetSection("StatChannels").GetSection("DayNightCycle").GetValue<double>("UpdateInterval");

        var timer = new Timer
        {
            AutoReset = true,
            Interval = updateInterval
        };

        timer.Elapsed += DoDayNightCycleChannelUpdate;

        timer.Start();

        return Task.CompletedTask;
    }


    private async void DoDayNightCycleChannelUpdate(object sender, ElapsedEventArgs e)
    {
        var dayNightCycleChannelId = _configuration.GetSection("StatChannels").GetSection("DayNightCycle").GetValue<ulong>("ChannelId");
        if (await _client.GetChannelAsync(dayNightCycleChannelId) is not IVoiceChannel channel) return;

        var oldChannelName = channel.Name;
        var newChannelName = GetCurrentDayNightCycleChannelName();
        if (string.Equals(oldChannelName.Split(" ").Last(), newChannelName.Split(" ").Last())) return;

        _log.Information("Updating day/night cycle channel name. Old: {OldDayNightCycleChannelName} | New: {NewDayNightCycleChannelName}", oldChannelName, newChannelName);

        try
        {
            await channel.ModifyAsync(prop => prop.Name = newChannelName);
            _log.Information("Day/Night cycle channel name updated.");
        }
        catch (Exception ex)
        {
            _log.Error(ex, ex.Message);
        }
    }

    private static string GetCurrentDayNightCycleChannelName()
    {
        var isNightTime = DerethDateTime.UtcNowToEMUTime.IsNighttime;

        return isNightTime
            ? "Time: \ud83c\udf11 Night"
            : "Time: \ud83c\udf1e Day";
    }

    private async Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };

        _log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);

        await Task.CompletedTask;
    }
}
