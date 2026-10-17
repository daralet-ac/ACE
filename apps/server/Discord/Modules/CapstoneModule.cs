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
/// The capstone blacklist (@capstone blacklist in game, capstoneblacklist in the console) from Discord: characters whose
/// capstone runs stay off the website's capstone leaderboard. Answers are only seen by whoever asked.
///
/// Like every command here it can be used by whoever Discord's own settings let use it (Server Settings, Integrations);
/// until that is set up, only server administrators can, because it changes a leaderboard.
/// </summary>
[Group("capstone", "Capstone leaderboard commands")]
[DefaultMemberPermissions(GuildPermission.Administrator)]
public class CapstoneModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly TimeSpan WorldThreadTimeout = TimeSpan.FromSeconds(15);

    [SlashCommand("blacklist-add", "Keeps a character's capstone runs off the capstone leaderboard.")]
    public Task BlacklistAdd([Summary(description: "The character's name")] string character) =>
        RunBlacklist(["add", character]);

    [SlashCommand("blacklist-remove", "Puts a character's capstone runs back on the capstone leaderboard.")]
    public Task BlacklistRemove([Summary(description: "The character's name")] string character) =>
        RunBlacklist(["remove", character]);

    [SlashCommand("blacklist-list", "Lists the characters on the capstone blacklist.")]
    public Task BlacklistList() => RunBlacklist(["list"]);

    private async Task RunBlacklist(string[] words)
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
                    Capstone.RunBlacklist(words, messages.Add);
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
