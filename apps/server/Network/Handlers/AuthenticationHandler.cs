using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACE.Common;
using ACE.Database;
using ACE.Database.Models.Auth;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.Enum;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Managers;
using ACE.Server.Network.Packets;
using Serilog;

namespace ACE.Server.Network.Handlers;

public static class AuthenticationHandler
{
    /// <summary>
    /// Seconds until an authentication request will timeout/expire.
    /// </summary>
    public const int DefaultAuthTimeout = 15;

    private static readonly ILogger _log = Log.ForContext(typeof(AuthenticationHandler));

    private static List<string> _vpnBlockedIPs = null;
    public static List<string> VpnBlockedIPs
    {
        get
        {
            if (_vpnBlockedIPs == null)
            {
                _vpnBlockedIPs = new List<string>();
            }

            return _vpnBlockedIPs;
        }
    }

    private static List<string> _vpnApprovedIPs = null;
    public static List<string> VpnApprovedIPs
    {
        get
        {
            if (_vpnApprovedIPs == null)
            {
                _vpnApprovedIPs = new List<string>();
            }

            return _vpnApprovedIPs;
        }
    }

    public static void HandleLoginRequest(ClientPacket packet, Session session)
    {
        try
        {
            var loginRequest = new PacketInboundLoginRequest(packet);

            if (loginRequest.Account.Length > 50)
            {
                NetworkManager.SendLoginRequestReject(session, CharacterError.AccountInvalid);
                session.Terminate(SessionTerminationReason.AccountInformationInvalid);
                return;
            }

            var t = new Task(() => DoLogin(session, loginRequest));
            t.Start();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Received LoginRequest from {ClientEndpoint} that threw an exception.", session.EndPointC2S);
        }
    }

    private static void DoLogin(Session session, PacketInboundLoginRequest loginRequest)
    {
        var account = DatabaseManager.Authentication.GetAccountByName(loginRequest.Account);

        if (account == null)
        {
            if (loginRequest.NetAuthType == NetAuthType.AccountPassword && loginRequest.Password != "")
            {
                if (ConfigManager.Config.Server.Accounts.AllowAutoAccountCreation)
                {
                    // no account, dynamically create one
                    if (WorldManager.WorldStatus == WorldManager.WorldStatusState.Open)
                    {
                        _log.Information($"Auto creating account for: {loginRequest.Account}");
                    }

                    _log.Debug("Auto creating account for: {Account}", loginRequest.Account);

                    var accessLevel = (AccessLevel)ConfigManager.Config.Server.Accounts.DefaultAccessLevel;

                    if (!System.Enum.IsDefined(typeof(AccessLevel), accessLevel))
                    {
                        accessLevel = AccessLevel.Player;
                    }

                    if (DatabaseManager.AutoPromoteNextAccountToAdmin)
                    {
                        accessLevel = AccessLevel.Admin;
                        DatabaseManager.AutoPromoteNextAccountToAdmin = false;
                        _log.Warning(
                            $"Automatically setting account AccessLevel to Admin for account \"{loginRequest.Account}\" because there are no admin accounts in the current database."
                        );
                    }

                    account = DatabaseManager.Authentication.CreateAccount(
                        loginRequest.Account.ToLower(),
                        loginRequest.Password,
                        accessLevel,
                        session.EndPointC2S.Address
                    );
                }
            }
        }

        try
        {
            _log.Debug("new client connected: {Account}. setting session properties", loginRequest.Account);
            AccountSelectCallback(account, session, loginRequest);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error in HandleLoginRequest trying to find the account.");
            session.Terminate(SessionTerminationReason.AccountSelectCallbackException);
        }
    }

    private static void AccountSelectCallback(Account account, Session session, PacketInboundLoginRequest loginRequest)
    {
        _log.Verbose("ConnectRequest TS: {0}", Timers.PortalYearTicks);

        if (session.Network.ConnectionData.ServerSeed == null || session.Network.ConnectionData.ClientSeed == null)
        {
            // these are null if ConnectionData.DiscardSeeds() is called because of some other error condition.
            session.Terminate(
                SessionTerminationReason.BadHandshake,
                new GameMessageCharacterError(CharacterError.ServerCrash1)
            );
            return;
        }

        if (loginRequest.ClientVersion == null || !loginRequest.ClientVersion.Equals("1802"))
        {
            session.Terminate(
                SessionTerminationReason.ClientVersionIncorrect,
                new GameMessageBootAccount(
                    " because your client is not the correct version for this server. Please visit http://play.emu.ac/ to update to latest client"
                )
            );
            return;
        }

        var connectRequest = new PacketOutboundConnectRequest(
            Timers.PortalYearTicks,
            session.Network.ConnectionData.ConnectionCookie,
            session.Network.ClientId,
            session.Network.ConnectionData.ServerSeed,
            session.Network.ConnectionData.ClientSeed
        );

        session.Network.ConnectionData.DiscardSeeds();

        session.Network.EnqueueSend(connectRequest);

        if (loginRequest.NetAuthType < NetAuthType.AccountPassword)
        {
            if (loginRequest.Account == "acservertracker:jj9h26hcsggc")
            {
                //log.Info($"Incoming ping from a Thwarg-Launcher client... Sending Pong...");

                session.Terminate(
                    SessionTerminationReason.PongSentClosingConnection,
                    new GameMessageCharacterError(CharacterError.ServerCrash1)
                );

                return;
            }

            if (WorldManager.WorldStatus == WorldManager.WorldStatusState.Open)
            {
                _log.Information(
                    "client {loginRequest.Account} connected with no Password or GlsTicket included so booting",
                    loginRequest.Account
                );
            }
            else
            {
                _log.Debug(
                    "client {Account} connected with no Password or GlsTicket included so booting",
                    loginRequest.Account
                );
            }

            session.Terminate(
                SessionTerminationReason.NotAuthorizedNoPasswordOrGlsTicketIncludedInLoginReq,
                new GameMessageCharacterError(CharacterError.AccountInvalid)
            );

            return;
        }

        if (account == null)
        {
            session.Terminate(
                SessionTerminationReason.NotAuthorizedAccountNotFound,
                new GameMessageCharacterError(CharacterError.AccountDoesntExist)
            );
            return;
        }

        if (!PropertyManager.GetBool("account_login_boots_in_use").Item)
        {
            if (NetworkManager.Find(account.AccountName) != null)
            {
                session.Terminate(
                    SessionTerminationReason.AccountInUse,
                    new GameMessageCharacterError(CharacterError.Logon)
                );
                return;
            }
        }

        if (loginRequest.NetAuthType == NetAuthType.AccountPassword)
        {
            if (!account.PasswordMatches(loginRequest.Password))
            {
                if (WorldManager.WorldStatus == WorldManager.WorldStatusState.Open)
                {
                    _log.Information($"client {loginRequest.Account} connected with non matching password so booting");
                }
                else
                {
                    _log.Debug(
                        "client {Account} connected with non matching password so booting",
                        loginRequest.Account
                    );
                }

                session.Terminate(
                    SessionTerminationReason.NotAuthorizedPasswordMismatch,
                    new GameMessageBootAccount(" because the password entered for this account was not correct")
                );

                // TO-DO: temporary lockout of account preventing brute force password discovery
                // exponential duration of lockout for targeted account

                return;
            }

            //Disallow VPN connections
             var isAccountVpnWhitelisted = false;
             var whitelist = PropertyManager.GetString("vpn_account_whitelist").Item.Split(",");
             if(whitelist.Length > 0)
             {
                 var match = whitelist.FirstOrDefault(account.AccountName);
                 if (match != null)
                 {
                     isAccountVpnWhitelisted = true;
                 }
             }

             if (PropertyManager.GetBool("block_vpn_connections").Item && !isAccountVpnWhitelisted)
             {
                 try
                 {
                     var currIp = session.EndPointC2S.Address.ToString();
                     var isVpn = false;
                     if (!VpnApprovedIPs.Contains(currIp))
                     {
                         if (VpnBlockedIPs.Contains(currIp))
                         {
                             isVpn = true;
                         }
                         else
                         {
                             //The IP isn't on the block list or on the cleared list, so check against API to see if its a VPN
                             isVpn = CheckForVpn(currIp);
                             if (isVpn)
                             {
                                 VpnBlockedIPs.Add(currIp);
                             }
                             else
                             {
                                 VpnApprovedIPs.Add(currIp);
                             }
                         }
                     }

                     if (isVpn)
                     {
                         _log.Information($"Blocked login attempt for account {session.Account} from IP {currIp} due to VPN detection");
                         var bootMsg = " Connections from VPN / proxy disallowed by server policy";
                         session.Terminate(SessionTerminationReason.AccountBooted, new GameMessageBootAccount(bootMsg), null, bootMsg);
                         return;
                     }
                 }
                 catch (Exception ex)
                 {
                     _log.Error($"Exception during VPN detection check for account = {session.Account}.  Ex: {ex}");
                 }
             }

            if (PropertyManager.GetBool("account_login_boots_in_use").Item)
            {
                var previouslyConnectedAccount = NetworkManager.Find(account.AccountName);

                if (previouslyConnectedAccount != null)
                {
                    // Boot the existing account
                    previouslyConnectedAccount.Terminate(
                        SessionTerminationReason.AccountLoggedIn,
                        new GameMessageCharacterError(CharacterError.Logon)
                    );

                    // We still can't let the new account in. They'll need to retry after the previous account has been successfully booted.
                    session.Terminate(
                        SessionTerminationReason.AccountInUse,
                        new GameMessageCharacterError(CharacterError.Logon)
                    );
                    return;
                }
            }

            if (WorldManager.WorldStatus == WorldManager.WorldStatusState.Open)
            {
                _log.Information($"client {loginRequest.Account} connected with verified password");
            }
            else
            {
                _log.Debug("client {Account} connected with verified password", loginRequest.Account);
            }
        }
        else if (loginRequest.NetAuthType == NetAuthType.GlsTicket)
        {
            if (WorldManager.WorldStatus == WorldManager.WorldStatusState.Open)
            {
                _log.Information(
                    $"client {loginRequest.Account} connected with GlsTicket which is not implemented yet so booting"
                );
            }
            else
            {
                _log.Debug(
                    "client {Account} connected with GlsTicket which is not implemented yet so booting",
                    loginRequest.Account
                );
            }

            session.Terminate(
                SessionTerminationReason.NotAuthorizedGlsTicketNotImplementedToProcLoginReq,
                new GameMessageCharacterError(CharacterError.AccountInvalid)
            );

            return;
        }

        if (account.BanExpireTime.HasValue)
        {
            var now = DateTime.UtcNow;
            if (now < account.BanExpireTime.Value)
            {
                var reason = account.BanReason;
                session.Terminate(
                    SessionTerminationReason.AccountBanned,
                    new GameMessageAccountBanned(
                        account.BanExpireTime.Value,
                        $"{(reason != null ? $" - {reason}" : null)}"
                    ),
                    null,
                    reason
                );
                return;
            }
            else
            {
                account.UnBan();
            }
        }

        account.UpdateLastLogin(session.EndPointC2S.Address);

        session.SetAccount(account.AccountId, account.AccountName, (AccessLevel)account.AccessLevel);
        session.State = SessionState.AuthConnectResponse;
    }

    public static void HandleConnectResponse(Session session)
    {
        if (WorldManager.WorldStatus == WorldManager.WorldStatusState.Open || session.AccessLevel > AccessLevel.Player)
        {
            DatabaseManager.Shard.GetCharacters(
                session.AccountId,
                false,
                result =>
                {
                    // If you want to create default characters for accounts that have none, here is where you would do it.

                    SendConnectResponse(session, result);
                }
            );
        }
        else
        {
            session.Terminate(
                SessionTerminationReason.WorldClosed,
                new GameMessageCharacterError(CharacterError.LogonServerFull)
            );
        }
    }

    private static void SendConnectResponse(Session session, List<Character> characters)
    {
        characters = characters.OrderByDescending(o => o.LastLoginTimestamp).ToList(); // The client highlights the first character in the list. We sort so the first character sent is the one we last logged in
        session.UpdateCharacters(characters);

        var characterListMessage = new GameMessageCharacterList(session.Characters, session);
        var serverNameMessage = new GameMessageServerName(
            ConfigManager.Config.Server.WorldName,
            PlayerManager.GetOnlineCount(),
            (int)ConfigManager.Config.Server.Network.MaximumAllowedSessions
        );
        var dddInterrogation = new GameMessageDDDInterrogation();

        session.Network.EnqueueSend(characterListMessage, serverNameMessage);
        session.Network.EnqueueSend(dddInterrogation);
    }

    private static bool CheckForVpn(string ip)
    {
        if(ip.Equals("127.0.0.1"))
        {
            return false;
        }

        var isVpn = false;
        //Console.WriteLine("In AuthenticationHandler.CheckForVpn");

        try
        {
            var task = VpnDetection.CheckVpn(ip);
            task.Wait();
            var ispInfo = task.Result;

            if (ispInfo != null && !String.IsNullOrEmpty(ispInfo.Proxy) && ispInfo.Proxy.Equals("yes"))
            {
                Console.WriteLine($"ISPInfo = {ispInfo}");
                _log.Warning($"VPN detected with ISPInfo = {ispInfo}");
                isVpn = true;
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Exception in AuthenticationHandler.CheckForVPN. Ex: {ex}");
        }

        //Console.WriteLine($"AuthenticationHandler.CheckForVpn returning isVpn = {isVpn}");
        return isVpn;
    }

    public static void ClearVpnBlockedIPs()
    {
        VpnBlockedIPs.Clear();
    }

    public static void RemoveIpFromVpnBlockList(string ip)
    {
        if(VpnBlockedIPs.Contains(ip))
        {
            VpnBlockedIPs.Remove(ip);
        }
    }
}
