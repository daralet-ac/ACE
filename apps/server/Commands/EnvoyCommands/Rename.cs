using System.Linq;
using System.Threading;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.EnvoyCommands;

public class Rename
{
    // rename <Current Name> <New Name>
    [CommandHandler(
        "rename",
        AccessLevel.Envoy,
        CommandHandlerFlag.None,
        2,
        "Rename a character. (Do NOT include +'s for admin names)",
        "< Current Name >, < New Name >"
    )]
    public static void HandleRename(Session session, params string[] parameters)
    {
        // @rename <Current Name>, <New Name> - Rename a character. (Do NOT include +'s for admin names)

        if (!string.Join(" ", parameters).Contains(','))
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "Error, cannot rename. You must include the old name followed by a comma and then the new name.\n Example: @rename Old Name, New Name",
                ChatMessageType.Broadcast
            );
            return;
        }

        var names = string.Join(" ", parameters).Split(",");

        var oldName = names[0].TrimStart(' ').TrimEnd(' ');
        var newName = names[1].TrimStart(' ').TrimEnd(' ');

        if (oldName.StartsWith("+"))
        {
            oldName = oldName.Substring(1);
        }

        if (newName.StartsWith("+"))
        {
            newName = newName.Substring(1);
        }

        newName = newName.First().ToString().ToUpper() + newName.Substring(1);

        var onlinePlayer = PlayerManager.GetOnlinePlayer(oldName);
        var offlinePlayer = PlayerManager.GetOfflinePlayer(oldName);
        if (onlinePlayer != null)
        {
            DatabaseManager.Shard.IsCharacterNameAvailable(
                newName,
                isAvailable =>
                {
                    if (!isAvailable)
                    {
                        CommandHandlerHelper.WriteOutputInfo(
                            session,
                            $"Error, a player named \"{newName}\" already exists.",
                            ChatMessageType.Broadcast
                        );
                        return;
                    }

                    onlinePlayer.Character.Name = newName;
                    onlinePlayer.CharacterChangesDetected = true;
                    onlinePlayer.Name = newName;
                    onlinePlayer.SavePlayerToDatabase();

                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"Player named \"{oldName}\" renamed to \"{newName}\" successfully!",
                        ChatMessageType.Broadcast
                    );

                    onlinePlayer.Session.LogOffPlayer();
                }
            );
        }
        else if (offlinePlayer != null)
        {
            DatabaseManager.Shard.IsCharacterNameAvailable(
                newName,
                isAvailable =>
                {
                    if (!isAvailable)
                    {
                        CommandHandlerHelper.WriteOutputInfo(
                            session,
                            $"Error, a player named \"{newName}\" already exists.",
                            ChatMessageType.Broadcast
                        );
                        return;
                    }

                    var character = DatabaseManager.Shard.BaseDatabase.GetCharacterStubByName(oldName);

                    DatabaseManager.Shard.GetCharacters(
                        character.AccountId,
                        false,
                        result =>
                        {
                            var foundCharacterMatch = result.Where(c => c.Id == character.Id).FirstOrDefault();

                            if (foundCharacterMatch == null)
                            {
                                CommandHandlerHelper.WriteOutputInfo(
                                    session,
                                    $"Error, a player named \"{oldName}\" cannot be found.",
                                    ChatMessageType.Broadcast
                                );
                            }

                            DatabaseManager.Shard.RenameCharacter(
                                foundCharacterMatch,
                                newName,
                                new ReaderWriterLockSlim(),
                                null
                            );
                        }
                    );

                    offlinePlayer.SetProperty(PropertyString.Name, newName);
                    offlinePlayer.SaveBiotaToDatabase();

                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"Player named \"{oldName}\" renamed to \"{newName}\" successfully!",
                        ChatMessageType.Broadcast
                    );
                }
            );
        }
        else
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Error, a player named \"{oldName}\" cannot be found.",
                ChatMessageType.Broadcast
            );
            return;
        }
    }
}
