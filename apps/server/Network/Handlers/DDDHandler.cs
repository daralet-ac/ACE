using System;
using System.Linq;
using ACE.Common;
using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Server.Managers;
using ACE.Server.Network.Enum;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;
using Serilog;

namespace ACE.Server.Network.Handlers;

public static class DDDHandler
{
    private static readonly ILogger _log = Log.ForContext(typeof(DDDHandler));

    public static bool Debug = false;

    [GameMessage(GameMessageOpcode.DDD_InterrogationResponse, SessionState.AuthConnected)]
    public static void DDD_InterrogationResponse(ClientMessage message, Session session)
    {
        var clientIsMissingIterations = false;

        var clientHasExtraIterations = false;

        var clientPortalDatIntSet = new CMostlyConsecutiveIntSet();
        var clientCellDatIntSet = new CMostlyConsecutiveIntSet();
        var clientLanguageDatIntSet = new CMostlyConsecutiveIntSet();
        var clientHighResDatIntSet = new CMostlyConsecutiveIntSet();

        var showDatWarning = PropertyManager.GetBool("show_dat_warning").Item;

        message.Payload.ReadUInt32(); // m_ClientLanguage

        var ItersWithKeys = message.Payload.ReadCAllIterationList();
        //var ItersWithoutKeys = message.Payload.ReadCAllIterationList(); // Not seen this populated in any pcap.
        // message.Payload.ReadUInt32(); // m_dwFlags - We don't need this

        foreach (var entry in ItersWithKeys.Lists)
        {
            switch (entry.DatFileId)
            {
                case 1: // PORTAL or HIGHRES
                    if (entry.DatFileType == 0) // PORTAL
                    {
                        clientPortalDatIntSet = entry.List;
                        if (entry.List.Iterations < DatManager.PortalDat.Iteration)
                        {
                            if (showDatWarning)
                            {
                                session.DatWarnPortal = true;
                            }

                            clientIsMissingIterations = true;
                        }
                        else if (entry.List.Iterations > DatManager.PortalDat.Iteration)
                        {
                            if (showDatWarning)
                            {
                                session.DatWarnPortal = true;
                            }

                            clientHasExtraIterations = true;
                        }
                    }
                    else if (entry.DatFileType == DDDManager.HiFi_String_As_Int) // HIGHRES
                    {
                        clientHighResDatIntSet = entry.List;

                        if (DatManager.HighResDat == null)
                        {
                            continue;
                        }

                        if (entry.List.Iterations < DatManager.HighResDat.Iteration)
                        {
                            // a client without the highres dat reports 0 iterations, and simply won't use it
                            if (clientHighResDatIntSet.Iterations == 0)
                            {
                                continue;
                            }

                            if (showDatWarning)
                            {
                                session.DatWarnHighRes = true;
                            }

                            clientIsMissingIterations = true;
                        }
                        else if (entry.List.Iterations > DatManager.HighResDat.Iteration)
                        {
                            if (showDatWarning)
                            {
                                session.DatWarnHighRes = true;
                            }

                            clientHasExtraIterations = true;
                        }
                    }
                    break;
                case 2: // CELL
                    clientCellDatIntSet = entry.List;
                    if (entry.List.Iterations < DatManager.CellDat.Iteration)
                    {
                        // an empty cell dat is fine, the client downloads cells on demand
                        if (clientCellDatIntSet.Iterations == 0)
                        {
                            continue;
                        }

                        if (showDatWarning)
                        {
                            session.DatWarnCell = true;
                        }

                        clientIsMissingIterations = true;
                    }
                    else if (entry.List.Iterations > DatManager.CellDat.Iteration)
                    {
                        if (showDatWarning)
                        {
                            session.DatWarnCell = true;
                        }

                        clientHasExtraIterations = true;
                    }
                    break;
                case 3: // LANGUAGE
                    clientLanguageDatIntSet = entry.List;
                    if (entry.List.Iterations < DatManager.LanguageDat.Iteration)
                    {
                        if (showDatWarning)
                        {
                            session.DatWarnLanguage = true;
                        }

                        clientIsMissingIterations = true;
                    }
                    else if (entry.List.Iterations > DatManager.LanguageDat.Iteration)
                    {
                        if (showDatWarning)
                        {
                            session.DatWarnLanguage = true;
                        }

                        clientHasExtraIterations = true;
                    }
                    break;
            }
        }

        if (Debug)
        {
            Console.WriteLine($"{session.Account} client_portal.dat:" + Environment.NewLine + clientPortalDatIntSet);
            Console.WriteLine($"{session.Account} client_cell_1.dat:" + Environment.NewLine + clientCellDatIntSet);
            Console.WriteLine(
                $"{session.Account} client_Local_English.dat:" + Environment.NewLine + clientLanguageDatIntSet
            );
            Console.WriteLine($"{session.Account} client_highres.dat:" + Environment.NewLine + clientHighResDatIntSet);
        }

        var enableDATpatching = ConfigManager.Config.DDD.EnableDATPatching;

        var logMsg =
            $"[DDD] client {session.Account} responded to Interrogation:\n client_portal.dat: {clientPortalDatIntSet.Iterations} | client_cell_1.dat: {clientCellDatIntSet.Iterations} | client_Local_English.dat: {clientLanguageDatIntSet.Iterations}";
        if (PropertyManager.GetBool("allow_highres_dat").Item)
        {
            logMsg +=
                $"\n client_highres.dat: {clientHighResDatIntSet.Iterations}{(DatManager.HighResDat == null ? " (server allows but does not have client_highres.dat to validate)" : "")}";
        }

        if (clientHasExtraIterations)
        {
            logMsg += " | client has more iterations than server, cannot update";
        }
        else if (clientIsMissingIterations)
        {
            logMsg += " | update required";
        }
        else
        {
            logMsg += " | no update required";
        }

        if (clientIsMissingIterations && !enableDATpatching)
        {
            logMsg += ", but DAT patching is disabled";
        }

        _log.Information(logMsg);

        if (clientHasExtraIterations)
        {
            var msg = PropertyManager.GetString("dat_newer_warning_msg").Item;
            session.Terminate(
                SessionTerminationReason.DATsNewerThanServer,
                new GameMessageBootAccount($" because {msg[..^1]}")
            );
        }
        else if (clientIsMissingIterations && enableDATpatching)
        {
            var totalMissingIterations = DDDManager.GetMissingIterations(
                clientPortalDatIntSet,
                clientCellDatIntSet,
                clientLanguageDatIntSet,
                clientHighResDatIntSet,
                out var totalFileSize,
                out var missingIterations
            );
            var patchStatusMessage = new GameMessageDDDBeginDDD(
                totalMissingIterations,
                totalFileSize,
                missingIterations
            );
            session.Network.EnqueueSend(patchStatusMessage);
            session.BeginDDDSentTime = DateTime.UtcNow;
            session.BeginDDDSent = true;

            var patch = new DDDPatchProgress(totalMissingIterations, totalFileSize, missingIterations);
            session.DDDPatch = patch;

            var missingIterationsSummary = DDDManager.DescribeMissingIterations(missingIterations);

            _log.Information(
                $"[DDD] client {session.Account} informed with BeginDDD payload:\n Total Missing Iterations: {totalMissingIterations} | Expected Data Transfer Size: {totalFileSize / 1024:N0} kB | Files: {patch.TotalExpectedFiles:N0}\n {missingIterationsSummary}"
            );

            DDDManager.RecordPatchStart(session.Account, missingIterationsSummary);

            void QueuePatchFile(uint fileId, DatDatabaseType datDatabaseType)
            {
                if (!DDDManager.AddToQueue(session, fileId, datDatabaseType))
                {
                    // its bytes are in the BeginDDD total but it will only be sent once, so the client will never reach the total
                    _log.Warning(
                        $"[DDD] DDD_InterrogationResponse: 0x{fileId:X8} ({datDatabaseType}) was already queued for client {session.Account}, it appears more than once in the patch list"
                    );
                }
            }

            var hasPortalMissingIterations = missingIterations.TryGetValue(
                DatDatabaseType.Portal,
                out var portalMissingIterations
            );
            var hasLanguageMissingIterations = missingIterations.TryGetValue(
                DatDatabaseType.Language,
                out var languageMissingIterations
            );
            var hasCellMissingIterations = missingIterations.TryGetValue(
                DatDatabaseType.Cell,
                out var cellMissingIterations
            );
            var hasHighResMissingIterations = missingIterations.TryGetValue(
                DatDatabaseType.HighRes,
                out var highResMissingIterations
            );

            if (hasCellMissingIterations)
            {
                foreach (var iteration in cellMissingIterations.Values)
                {
                    foreach (var fileId in iteration.OrderBy(f => f))
                    {
                        DatFileType qdid_type = 0;
                        var qdid_ID = fileId;

                        var hexValue = int.Parse(fileId.ToString("X8"), System.Globalization.NumberStyles.HexNumber);

                        if ((hexValue & 0xFFFF) == 0xFFFF)
                        {
                            qdid_type = DatFileType.LandBlock;
                            //Console.WriteLine($"Landblock: {fileId:X8} {fileId}");
                        }
                        else if ((hexValue & 0xFFFE) == 0xFFFE)
                        {
                            qdid_type = DatFileType.LandBlockInfo;
                            //Console.WriteLine($"LandblockInfo: {fileId:X8} {fileId}");
                        }
                        else
                        {
                            qdid_type = DatFileType.EnvCell;
                            //Console.WriteLine($"EnvCell: {fileId:X8} {fileId}");
                        }

                        // Unlike on-demand requests, don't add the LandBlockInfo (0xFFFE) companion here. If it changed it is already
                        // in the missing iterations list, and if it didn't, its bytes aren't in the BeginDDD total - sending it anyway
                        // pushes the client's byte count past the total, so it ends patching before the rest of the queue arrives.
                        if (
                            qdid_type == DatFileType.LandBlock
                            || qdid_type == DatFileType.LandBlockInfo
                            || qdid_type == DatFileType.EnvCell
                        )
                        {
                            if (DatManager.CellDat.AllFiles.TryGetValue(qdid_ID, out var datFile))
                            {
                                //session.Network.EnqueueSend(new GameMessageDDDDataMessage(qdid_ID, DatDatabaseType.Cell));
                                QueuePatchFile(qdid_ID, DatDatabaseType.Cell);
                            }
                            else
                            {
                                patch.UnqueuedFiles++;
                                _log.Warning(
                                    $"[DDD] DDD_InterrogationResponse: The server does not have 0x{qdid_ID:X8} | {qdid_type} from the patch list to send to client {session.Account}."
                                );
                            }
                        }
                        else
                        {
                            patch.UnqueuedFiles++;
                            _log.Warning(
                                $"[DDD] DDD_RequestDataMessage: client {session.Account} requested data on 0x{qdid_ID:X8} | {qdid_type} which has been ignored."
                            );
                        }
                    }
                }
            }

            if (hasPortalMissingIterations)
            {
                foreach (var iteration in portalMissingIterations.Values)
                {
                    foreach (var fileId in iteration.OrderBy(f => f))
                    {
                        if (DatManager.PortalDat.AllFiles.TryGetValue(fileId, out _))
                        {
                            //session.Network.EnqueueSend(new GameMessageDDDDataMessage(fileId, DatDatabaseType.Portal));
                            QueuePatchFile(fileId, DatDatabaseType.Portal);
                        }
                        else
                        {
                            patch.UnqueuedFiles++;
                            _log.Warning(
                                $"[DDD] DDD_InterrogationResponse: DDDManager.AddToQueue failed: DatManager.PortalDat.AllFiles does not contain 0x{fileId:X8}"
                            );
                        }
                    }
                }
            }

            if (hasLanguageMissingIterations)
            {
                foreach (var iteration in languageMissingIterations.Values)
                {
                    foreach (var fileId in iteration.OrderBy(f => f))
                    {
                        if (DatManager.LanguageDat.AllFiles.TryGetValue(fileId, out _))
                        {
                            //session.Network.EnqueueSend(new GameMessageDDDDataMessage(fileId, DatDatabaseType.Language));
                            QueuePatchFile(fileId, DatDatabaseType.Language);
                        }
                        else
                        {
                            patch.UnqueuedFiles++;
                            _log.Warning(
                                $"[DDD] DDD_InterrogationResponse: DDDManager.AddToQueue failed: DatManager.LanguageDat.AllFiles does not contain 0x{fileId:X8}"
                            );
                        }
                    }
                }
            }

            if (hasHighResMissingIterations)
            {
                foreach (var iteration in highResMissingIterations.Values)
                {
                    foreach (var fileId in iteration.OrderBy(f => f))
                    {
                        if (DatManager.HighResDat.AllFiles.TryGetValue(fileId, out _))
                        {
                            QueuePatchFile(fileId, DatDatabaseType.HighRes);
                        }
                        else
                        {
                            patch.UnqueuedFiles++;
                            _log.Warning(
                                $"[DDD] DDD_InterrogationResponse: DDDManager.AddToQueue failed: DatManager.HighResDat.AllFiles does not contain 0x{fileId:X8}"
                            );
                        }
                    }
                }
            }
        }
        else if (clientIsMissingIterations && !enableDATpatching)
        {
            var msg = PropertyManager.GetString("dat_older_warning_msg").Item;
            session.Terminate(
                SessionTerminationReason.DATsPatchingDisabled,
                new GameMessageBootAccount($" because {msg.TrimEnd('.')}")
            );
        }
        else // client dat files are up to date
        {
            DDDManager.RecordUpToDate(session.Account);
            session.Network.EnqueueSend(new GameMessageDDDEndDDD());
        }
    }

    [GameMessage(GameMessageOpcode.DDD_EndDDD, SessionState.AuthConnected)]
    public static void DDD_EndDDD(ClientMessage message, Session session)
    {
        // We don't need to reply to this message unless GameMessageDDDBeginDDD was sent.

        if (session.BeginDDDSent)
        {
            session.BeginDDDSent = false;

            session.Network.EnqueueSend(new GameMessageDDDEndDDD());

            session.DatWarnPortal = false;
            session.DatWarnCell = false;
            session.DatWarnLanguage = false;
            session.DatWarnHighRes = false;

            var remainingFiles = session.DDDQueueCount;
            var patch = session.DDDPatch;
            var summary = "no patch progress recorded";

            if (patch != null)
            {
                patch.EndTime = DateTime.UtcNow;
                summary = patch.Summary();
            }

            var incomplete =
                remainingFiles > 0
                || patch != null
                    && (
                        patch.TotalSentFiles != patch.TotalExpectedFiles
                        || patch.SentBytes != patch.ExpectedBytes
                        || patch.FailedFiles > 0
                    );

            if (incomplete)
            {
                _log.Warning(
                    $"[DDD] client {session.Account} ended patching but the patch does not match what was advertised, its DAT files are likely incomplete: {summary} | {remainingFiles:N0} files still queued"
                );

                DDDManager.RecordPatchOutcome(
                    session.Account,
                    $"client ended patching with an incomplete patch: {summary} | {remainingFiles:N0} files still queued"
                );
            }
            else
            {
                _log.Information(
                    $"[DDD] client {session.Account} reported it successfully received and patched its DAT files with expected BeginDDD payload: {summary}"
                );

                DDDManager.RecordPatchOutcome(session.Account, $"client ended patching normally: {summary}");
            }
        }
        else
        {
            _log.Debug($"[DDD] client {session.Account} sent EndDDD without a patch in progress");
        }
    }

    [GameMessage(GameMessageOpcode.DDD_RequestDataMessage, SessionState.WorldConnected)]
    public static void DDD_RequestDataMessage(ClientMessage message, Session session)
    {
        var enableDATpatching = ConfigManager.Config.DDD.EnableDATPatching;
        var showDatWarning = PropertyManager.GetBool("show_dat_warning").Item;

        var qdid_type = (DatFileType)message.Payload.ReadUInt32();
        var qdid_ID = message.Payload.ReadUInt32();

        //Console.WriteLine($"Message: {message}, Type: {qdid_type}, ID: {qdid_ID}, IDX8: {qdid_ID:X8}");

        if (enableDATpatching)
        {
            _log.Information(
                "[DDD] client {AccountId} requested data on 0x{FileId:X8} | {FileType}",
                session.Account,
                qdid_ID,
                qdid_type
            );
        }
        else if (!enableDATpatching && showDatWarning)
        {
            _log.Information(
                "[DDD] client {AccountId} requested data on 0x{FileId:X8} | {FileType}; DAT patching is disabled and client will be booted.",
                session.Account,
                qdid_ID,
                qdid_type
            );
        }
        else
        {
            _log.Information(
                "[DDD] client {AccountId} requested data on 0x{FileId:X8} | {FileType}; DAT patching is disabled.",
                session.Account,
                qdid_ID,
                qdid_type
            );
        }

        if (!enableDATpatching)
        {
            if (showDatWarning)
            {
                var msg = PropertyManager.GetString("dat_older_warning_msg").Item;
                var popupMsg = new GameEventPopupString(session, msg);
                var chatMsg = new GameMessageSystemChat(msg, ChatMessageType.WorldBroadcast);
                var transientMsg = new GameEventCommunicationTransientString(session, msg);

                //var resourceType = message.Payload.ReadUInt32();
                //var dataId = message.Payload.ReadUInt32();
                var errorType = 1u; // unknown enum... this seems to trigger reattempt request by client.

                var dddErrorMsg = new GameMessageDDDErrorMessage((uint)qdid_type, qdid_ID, errorType);

                if (session.Player.FirstEnterWorldDone) // Boot client with msg
                {
                    //session.Network.EnqueueSend(new GameMessageBootAccount($"\n{msg}"), dddErrorMsg);
                    //session.LogOffPlayer(true);
                    session.Terminate(
                        SessionTerminationReason.DATsPatchingDisabled,
                        new GameMessageBootAccount($" because {msg.TrimEnd('.')}")
                    );
                }
                else // cannot cleanly boot player that hasn't completed first login, client crashes so msg wouldn't be seen, instead spam msgs until server auto boots them or they disconnect.
                {
                    session.Network.EnqueueSend(popupMsg, chatMsg, transientMsg, dddErrorMsg);
                }
            }

            return;
        }

        // Landblock also needs to send the LandBlockInfo (0xFFFE) file with it...
        if (qdid_type == DatFileType.LandBlock)
        {
            var qdid_ID_FFFE = qdid_ID - 1;
            if (DatManager.CellDat.AllFiles.TryGetValue(qdid_ID_FFFE, out var datFileFFFE))
            {
                //session.Network.EnqueueSend(new GameMessageDDDDataMessage(qdid_ID_FFFE, DatDatabaseType.Cell));
                DDDManager.AddToQueue(session, qdid_ID_FFFE, DatDatabaseType.Cell);
            }
            //else
            //    _log.Warning($"[DDD] The server does not have the requested data on 0x{qdid_ID_FFFE:X8} | {qdid_type} to send.");
        }

        if (
            qdid_type == DatFileType.LandBlock
            || qdid_type == DatFileType.LandBlockInfo
            || qdid_type == DatFileType.EnvCell
        )
        {
            if (DatManager.CellDat.AllFiles.TryGetValue(qdid_ID, out var datFile))
            {
                //session.Network.EnqueueSend(new GameMessageDDDDataMessage(qdid_ID, DatDatabaseType.Cell));
                DDDManager.AddToQueue(session, qdid_ID, DatDatabaseType.Cell);
            }
            else if (qdid_type != DatFileType.LandBlockInfo)
            {
                _log.Warning(
                    $"[DDD] DDD_RequestDataMessage: The server does not have the requested data on 0x{qdid_ID:X8} | {qdid_type} to send to client {session.Account}."
                );
            }
        }
        else
        {
            _log.Warning(
                $"[DDD] DDD_RequestDataMessage: client {session.Account} requested data on 0x{qdid_ID:X8} | {qdid_type} which has been ignored."
            );
        }
    }
}
