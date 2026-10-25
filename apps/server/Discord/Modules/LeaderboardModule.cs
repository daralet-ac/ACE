using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ACE.Server.Commands.AdminCommands;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using Discord;
using Discord.Interactions;

namespace ACE.Server.Discord.Modules;

/// <summary>
/// The leaderboard exclusions (@leaderboard in game, leaderboardexclude in the console) from Discord: characters kept off
/// the website's general leaderboards. Answers are only seen by whoever asked.
///
/// Like every command here it can be used by whoever Discord's own settings let use it (Server Settings, Integrations);
/// until that is set up, only server administrators can, because it changes a leaderboard.
/// </summary>
[Group("leaderboard", "Website leaderboard commands")]
[DefaultMemberPermissions(GuildPermission.Administrator)]
public class LeaderboardModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly TimeSpan WorldThreadTimeout = TimeSpan.FromSeconds(15);

    [SlashCommand("exclude", "Keeps a character off the website's general leaderboards.")]
    public Task Exclude([Summary(description: "The character's name")] string character) =>
        RunExclusion(["exclude", character]);

    [SlashCommand("include", "Puts a character back on the website's general leaderboards.")]
    public Task Include([Summary(description: "The character's name")] string character) =>
        RunExclusion(["include", character]);

    [SlashCommand("list", "Lists the characters excluded from the general leaderboards.")]
    public Task List() => RunExclusion(["list"]);

    private async Task RunExclusion(string[] words)
    {
        await DeferAsync(ephemeral: true);

        // It changes a character, who may be playing, so it is done on the world thread, as a command typed in game is
        var messages = new List<string>();
        var done = new TaskCompletionSource();

        WorldManager.EnqueueAction(
            new ActionEventDelegate(() =>
            {
                try
                {
                    Leaderboard.RunExclusion(words, messages.Add);
                    done.TrySetResult();
                }
                catch (Exception ex)
                {
                    done.TrySetException(ex);
                }
            })
        );

        try
        {
            if (await Task.WhenAny(done.Task, Task.Delay(WorldThreadTimeout)) != done.Task)
            {
                await FollowupAsync("The server did not get to it in time. Try again in a moment.", ephemeral: true);
                return;
            }

            await done.Task;
        }
        catch (Exception ex)
        {
            await FollowupAsync($"That failed: {Format.Sanitize(ex.Message)}", ephemeral: true);
            return;
        }

        var reply = Format.Sanitize(string.Join("\n", messages));

        if (reply.Length > DiscordConfig.MaxMessageSize)
        {
            reply = reply[..(DiscordConfig.MaxMessageSize - 3)] + "...";
        }

        await FollowupAsync(reply.Length == 0 ? "Done." : reply, ephemeral: true);
    }
}
