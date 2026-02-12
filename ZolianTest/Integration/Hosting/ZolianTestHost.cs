using Chaos.Networking;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using ILobbyClient = Darkages.Network.Client.Abstractions.ILobbyClient;
using ILoginClient = Darkages.Network.Client.Abstractions.ILoginClient;
using IWorldClient = Darkages.Network.Client.Abstractions.IWorldClient;
using Darkages.Network.Client.Abstractions;
using Darkages.Network.Server.Abstractions;
using Chaos.Extensions.DependencyInjection;
using Zolian.GameServer;
using GameServerIServer = Zolian.GameServer.IServer;

namespace ZolianTest.Integration.Hosting;

public static class ZolianTestHost
{
    public static IHost BuildHost(string serverConfigJsonPath)
    {
        if (!File.Exists(serverConfigJsonPath)) throw new FileNotFoundException("ServerConfig.json not found for integration host.", serverConfigJsonPath);

        var configDir = Path.GetDirectoryName(serverConfigJsonPath) ?? throw new InvalidOperationException("ServerConfig.json path invalid.");

        var config = new ConfigurationBuilder()
            .SetBasePath(configDir)
            .AddJsonFile(Path.GetFileName(serverConfigJsonPath), optional: false, reloadOnChange: false)
            .Build();

        var constants = config.GetSection("ServerConfig").Get<ServerConstants>() ?? throw new InvalidOperationException("Missing ServerConfig section in ServerConfig.json");

        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddOptions();

                services.AddLogging(l =>
                {
                    l.ClearProviders();
                    l.AddConsole();
                    l.SetMinimumLevel(LogLevel.Trace);
                });

                // Match App.xaml.cs
                services.Configure<ServerOptions>(config.GetSection("Content"));

                services.AddSingleton<IServerConstants, ServerConstants>(_ => constants);
                services.AddSingleton<ServerSetup>();
                services.AddSingleton<IServerContext>(sp => sp.GetRequiredService<ServerSetup>());
                services.AddSingleton<GameServerIServer, Server>();

                services.AddCryptography();
                services.AddPacketSerializer();
                services.AddSingleton<IRedirectManager, RedirectManager>();

                // Lobby
                services.AddSingleton<IClientFactory<LobbyClient>, ClientFactory<LobbyClient>>();
                services.AddSingleton<IClientRegistry<ILobbyClient>, ClientRegistry<ILobbyClient>>();
                services.AddSingleton<LobbyServer>();
                services.AddSingleton<ILobbyServer<ILobbyClient>>(sp => sp.GetRequiredService<LobbyServer>());

                // Login
                services.AddSingleton<IClientFactory<LoginClient>, ClientFactory<LoginClient>>();
                services.AddSingleton<IClientRegistry<ILoginClient>, ClientRegistry<ILoginClient>>();
                services.AddSingleton<LoginServer>();
                services.AddSingleton<ILoginServer<ILoginClient>>(sp => sp.GetRequiredService<LoginServer>());

                // World
                services.AddSingleton<IClientFactory<WorldClient>, ClientFactory<WorldClient>>();
                services.AddSingleton<IClientRegistry<IWorldClient>, ClientRegistry<IWorldClient>>();
                services.AddSingleton<WorldServer>();
                services.AddSingleton<IWorldServer<IWorldClient>>(sp => sp.GetRequiredService<WorldServer>());

                // Hosted service that boots everything
                services.AddHostedService<ServerOrchestrator>();
            })
            .Build();
    }
}
