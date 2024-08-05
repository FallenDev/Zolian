using Darkages;
using Darkages.Interfaces;
using Darkages.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;

namespace Zolian.GameServer;

public interface IServer;

public class Server : IServer
{
    public Server(ILogger<ServerSetup> logger, IServerContext context, IServerConstants configConstants, IOptions<ServerOptions> serverOptions)
    {
        if (serverOptions.Value.Location == null) return;
        context.InitFromConfig(serverOptions.Value.Location, serverOptions.Value.ServerIp);
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"{configConstants.SERVER_TITLE} - IP: {serverOptions.Value.ServerIp} Server Start: {DateTime.Now}\n\n");
        context.Start(configConstants, logger);
    }
}