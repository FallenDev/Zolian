using System;
using System.Reflection;
using Darkages;
using Darkages.Interfaces;
using Darkages.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zolian.GameServer;

public interface IServer
{
    string ZolianVersion { get; }
}

public class Server : IServer
{
    public Server(ILogger<ServerSetup> logger, IServerContext context, IServerConstants configConstants,
        IOptions<ServerOptions> serverOptions)
    {
        var time = DateTime.Now;
        var localLogger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (serverOptions.Value.Location == null) return;

        context.InitFromConfig(serverOptions.Value.Location, serverOptions.Value.ServerIp);
        localLogger.LogInformation($"{configConstants.SERVER_TITLE}: Server Version: {ZolianVersion}. Server IP: {serverOptions.Value.ServerIp} Last Restart: {time}");
        context.Start(configConstants, logger);
    }

    public string ZolianVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? version.ToString() : string.Empty;
        }
    }
}