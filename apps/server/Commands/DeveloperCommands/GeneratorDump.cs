using System.Linq;
using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class GeneratorDump
{
    [CommandHandler(
        "generatordump",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Lists all properties for the last generator you examined.",
        ""
    )]
    public static void HandleGeneratorDump(Session session, params string[] parameters)
    {
        // TODO: output

        var objectId = new ObjectGuid();

        if (
            session.Player.HealthQueryTarget.HasValue
            || session.Player.ManaQueryTarget.HasValue
            || session.Player.CurrentAppraisalTarget.HasValue
        )
        {
            if (session.Player.HealthQueryTarget.HasValue)
            {
                objectId = new ObjectGuid((uint)session.Player.HealthQueryTarget);
            }
            else if (session.Player.ManaQueryTarget.HasValue)
            {
                objectId = new ObjectGuid((uint)session.Player.ManaQueryTarget);
            }
            else
            {
                objectId = new ObjectGuid((uint)session.Player.CurrentAppraisalTarget);
            }

            var wo = session.Player.CurrentLandblock?.GetObject(objectId);

            if (wo == null)
            {
                return;
            }

            if (objectId.IsPlayer())
            {
                return;
            }

            var msg = "";
            if (wo.IsGenerator)
            {
                msg = $"Generator Dump for {wo.Name} (0x{wo.Guid.ToString()})\n";
                msg += $"Generator WCID: {wo.WeenieClassId}\n";
                msg += $"Generator WeenieClassName: {wo.WeenieClassName}\n";
                msg += $"Generator WeenieType: {wo.WeenieType.ToString()}\n";
                msg += $"Generator Status: {(wo.GeneratorDisabled ? "Disabled" : "Enabled")}\n";
                msg += $"GeneratorType: {wo.GeneratorType.ToString()}\n";
                msg += $"GeneratorTimeType: {wo.GeneratorTimeType.ToString()}\n";
                if (wo.GeneratorTimeType == GeneratorTimeType.Event)
                {
                    msg +=
                        $"GeneratorEvent: {(!string.IsNullOrWhiteSpace(wo.GeneratorEvent) ? wo.GeneratorEvent : "Undef")}\n";
                }

                if (wo.GeneratorTimeType == GeneratorTimeType.RealTime)
                {
                    msg +=
                        $"GeneratorStartTime: {wo.GeneratorStartTime} ({Time.GetDateTimeFromTimestamp(wo.GeneratorStartTime).ToLocalTime()})\n";
                    msg +=
                        $"GeneratorEndTime: {wo.GeneratorEndTime} ({Time.GetDateTimeFromTimestamp(wo.GeneratorEndTime).ToLocalTime()})\n";
                }
                msg += $"GeneratorEndDestructionType: {wo.GeneratorEndDestructionType.ToString()}\n";
                msg += $"GeneratorDestructionType: {wo.GeneratorDestructionType.ToString()}\n";
                msg += $"GeneratorRadius: {wo.GetProperty(PropertyFloat.GeneratorRadius) ?? 0f}\n";
                msg += $"InitGeneratedObjects: {wo.InitGeneratedObjects}\n";
                msg += $"MaxGeneratedObjects: {wo.MaxGeneratedObjects}\n";
                msg += $"GeneratorInitialDelay: {wo.GeneratorInitialDelay}\n";
                msg += $"RegenerationInterval: {wo.RegenerationInterval}\n";
                msg +=
                    $"GeneratorUpdateTimestamp: {wo.GeneratorUpdateTimestamp} ({Time.GetDateTimeFromTimestamp(wo.GeneratorUpdateTimestamp).ToLocalTime()})\n";
                msg +=
                    $"NextGeneratorUpdateTime: {wo.NextGeneratorUpdateTime} ({((wo.NextGeneratorUpdateTime == double.MaxValue) ? "Disabled" : Time.GetDateTimeFromTimestamp(wo.NextGeneratorUpdateTime).ToLocalTime().ToString())})\n";
                msg +=
                    $"RegenerationTimestamp: {wo.RegenerationTimestamp} ({Time.GetDateTimeFromTimestamp(wo.RegenerationTimestamp).ToLocalTime()})\n";
                msg +=
                    $"NextGeneratorRegenerationTime: {wo.NextGeneratorRegenerationTime} ({((wo.NextGeneratorRegenerationTime == double.MaxValue) ? "On Demand" : Time.GetDateTimeFromTimestamp(wo.NextGeneratorRegenerationTime).ToLocalTime().ToString())})\n";

                msg += $"GeneratorProfiles.Count: {wo.GeneratorProfiles.Count(g => !g.IsPlaceholder)}\n";
                msg += $"GeneratorActiveProfiles.Count: {wo.GeneratorActiveProfiles.Count}\n";
                msg += $"CurrentCreate: {wo.CurrentCreate}\n";

                msg += $"===============================================\n";
                foreach (var activeProfile in wo.GeneratorActiveProfiles)
                {
                    var profile = wo.GeneratorProfiles[activeProfile];

                    msg += $"Active GeneratorProfile id: {activeProfile} | LinkId: {profile.LinkId}\n";

                    msg +=
                        $"Probability: {profile.Biota.Probability} | WCID: {profile.Biota.WeenieClassId} | Delay: {profile.Biota.Delay} | Init: {profile.Biota.InitCreate} | Max: {profile.Biota.MaxCreate}\n";
                    msg +=
                        $"WhenCreate: {profile.Biota.WhenCreate.ToString()} | WhereCreate: {profile.Biota.WhereCreate.ToString()}\n";
                    msg +=
                        $"StackSize: {profile.Biota.StackSize} | PaletteId: {profile.Biota.PaletteId} | Shade: {profile.Biota.Shade}\n";
                    msg +=
                        $"CurrentCreate: {profile.CurrentCreate} | Spawned.Count: {profile.Spawned.Count} | SpawnQueue.Count: {profile.SpawnQueue.Count}\n";
                    msg += $"GeneratedTreasureItem: {profile.GeneratedTreasureItem}\n";
                    msg += $"IsMaxed: {profile.IsMaxed}\n";
                    if (!profile.IsMaxed)
                    {
                        msg +=
                            $"IsAvailable: {profile.IsAvailable}{(profile.IsAvailable ? "" : $", NextAvailable: {profile.NextAvailable.ToLocalTime()}")}\n";
                    }

                    msg += $"--====--\n";
                    if (profile.Spawned.Count > 0)
                    {
                        msg += "Spawned Objects:\n";
                        foreach (var spawn in profile.Spawned.Values)
                        {
                            msg += $"0x{spawn.Guid}: {spawn.Name} - {spawn.WeenieClassId} - {spawn.WeenieType}\n";
                            var spawnWO = spawn.TryGetWorldObject();
                            if (spawnWO != null)
                            {
                                if (spawnWO.Location != null)
                                {
                                    msg += $" LOC: {spawnWO.Location.ToLOCString()}\n";
                                }
                                else if (spawnWO.ContainerId == wo.Guid.Full)
                                {
                                    msg += $" Contained by Generator\n";
                                }
                                else if (spawnWO.WielderId == wo.Guid.Full)
                                {
                                    msg += $" Wielded by Generator\n";
                                }
                                else
                                {
                                    msg += $" Location Unknown\n";
                                }
                            }
                            else
                            {
                                msg += $" LOC: Unknown, WorldObject could not be found\n";
                            }
                        }
                        msg += $"--====--\n";
                    }

                    if (profile.SpawnQueue.Count > 0)
                    {
                        msg += "Pending Spawn Times:\n";
                        foreach (var spawn in profile.SpawnQueue)
                        {
                            msg += $"{spawn.ToLocalTime()}\n";
                        }
                        msg += $"--====--\n";
                    }

                    msg += $"===============================================\n";
                }
            }
            else
            {
                msg = $"{wo.Name} (0x{wo.Guid.ToString()}) is not a generator.";
            }

            session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.System));
        }
    }
}
