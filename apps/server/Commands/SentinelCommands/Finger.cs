using System;
using System.Linq;
using System.Net;
using ACE.Database;
using ACE.Database.Models.Auth;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.SentinelCommands;

public class Finger
{
    private static readonly ILogger _log = Log.ForContext(typeof(Finger));

    // finger [ [-a] character] [-m account]
    [CommandHandler(
        "finger",
        AccessLevel.Sentinel,
        CommandHandlerFlag.None,
        1,
        "Show the given character's account name or vice-versa.",
        "[ [-a] character] [-m account]\n"
            + "Given a character name, this command displays the name of the owning account.\nIf the -m option is specified, the argument is considered an account name and the characters owned by that account are displayed.\nIf the -a option is specified, then the character name is fingered but their account is implicitly fingered as well."
    )]
    public static void HandleFinger(Session session, params string[] parameters)
    {
        // usage: @finger[ [-a] character] [-m account]
        // Given a character name, this command displays the name of the owning account.If the -m option is specified, the argument is considered an account name and the characters owned by that account are displayed.If the -a option is specified, then the character name is fingered but their account is implicitly fingered as well.
        // @finger - Show the given character's account name or vice-versa.

        var lookupCharAndAccount = parameters.Contains("-a");
        var lookupByAccount = parameters.Contains("-m");

        var charName = "";
        if (lookupByAccount || lookupCharAndAccount)
        {
            charName = string.Join(" ", parameters.Skip(1));
        }
        else
        {
            charName = string.Join(" ", parameters);
        }

        var message = "";
        if (!lookupByAccount && !lookupCharAndAccount)
        {
            var character = PlayerManager.FindByName(charName);

            if (character != null)
            {
                if (character.Account != null)
                {
                    message = $"Login name: {character.Account.AccountName}      Character: {character.Name}\n";
                }
                else
                {
                    message =
                        $"Login name: account not found, character is orphaned.      Character: {character.Name}\n";
                }
            }
            else
            {
                message = $"There was no active character named \"{charName}\" found in the database.\n";
            }
        }
        else
        {
            Account account;
            if (lookupCharAndAccount)
            {
                var character = PlayerManager.FindByName(charName);

                if (character == null)
                {
                    message = $"There was no active character named \"{charName}\" found in the database.\n";
                    CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.WorldBroadcast);
                    return;
                }
                account = character.Account;
                if (account == null)
                {
                    message =
                        $"Login name: account not found, character is orphaned.      Character: {character.Name}\n";
                    CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.WorldBroadcast);
                    return;
                }
                else // get updated data from db.
                {
                    account = DatabaseManager.Authentication.GetAccountById(account.AccountId);
                }
            }
            else
            {
                account = DatabaseManager.Authentication.GetAccountByName(charName);
            }

            if (account != null)
            {
                if (account.BannedTime != null)
                {
                    var bannedbyAccount =
                        account.BannedByAccountId > 0
                            ? $"account {DatabaseManager.Authentication.GetAccountById(account.BannedByAccountId.Value).AccountName}"
                            : "CONSOLE";

                    message =
                        $"Account '{account.AccountName}' was banned by {bannedbyAccount} until server time {account.BanExpireTime.Value.ToLocalTime():MMM dd yyyy  h:mmtt}.\n";
                }
                else
                {
                    message = $"Account '{account.AccountName}' is not banned.\n";
                }

                if (account.AccessLevel > (int)AccessLevel.Player)
                {
                    message +=
                        $"Account '{account.AccountName}' has been granted AccessLevel.{((AccessLevel)account.AccessLevel).ToString()} rights.\n";
                }

                message +=
                    $"Account created on {account.CreateTime.ToLocalTime()} by IP: {(account.CreateIP != null ? new IPAddress(account.CreateIP).ToString() : "N/A")} \n";
                message +=
                    $"Account last logged on at {(account.LastLoginTime.HasValue ? account.LastLoginTime.Value.ToLocalTime().ToString() : "N/A")} by IP: {(account.LastLoginIP != null ? new IPAddress(account.LastLoginIP).ToString() : "N/A")}\n";
                message += $"Account total times logged on {account.TotalTimesLoggedIn}\n";
                var characters = DatabaseManager.Shard.BaseDatabase.GetCharacters(account.AccountId, true);
                message += $"{characters.Count} Character(s) owned by: {account.AccountName}\n";
                message += "-------------------\n";
                foreach (var character in characters.Where(x => !x.IsDeleted && x.DeleteTime == 0))
                {
                    message +=
                        $"\"{(character.IsPlussed ? "+" : "")}{character.Name}\", ID 0x{character.Id.ToString("X8")}\n";
                }

                var pendingDeletedCharacters = characters.Where(x => !x.IsDeleted && x.DeleteTime > 0).ToList();
                if (pendingDeletedCharacters.Count > 0)
                {
                    message += "-------------------\n";
                    foreach (var character in pendingDeletedCharacters)
                    {
                        message +=
                            $"\"{(character.IsPlussed ? "+" : "")}{character.Name}\", ID 0x{character.Id.ToString("X8")} -- Will be deleted at server time {new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(character.DeleteTime).ToLocalTime().ToString("MMM d yyyy h:mm tt")}\n";
                    }
                }
                message += "-------------------\n";
                var deletedCharacters = characters.Where(x => x.IsDeleted).ToList();
                if (deletedCharacters.Count > 0)
                {
                    foreach (var character in deletedCharacters)
                    {
                        message +=
                            $"\"{(character.IsPlussed ? "+" : "")}{character.Name}\", ID 0x{character.Id.ToString("X8")} -- Deleted at server time {new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(character.DeleteTime).ToLocalTime().ToString("MMM d yyyy h:mm tt")}\n";
                    }
                }
                else
                {
                    message += "No deleted characters.\n";
                }
            }
            else
            {
                message = $"There was no account named \"{charName}\" found in the database.\n";
            }
        }

        CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.WorldBroadcast);
    }
}
