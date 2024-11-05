using ACE.Server.Network;

namespace ACE.Server.Commands.Handlers;

public delegate void CommandHandler(Session session, params string[] parameters);
