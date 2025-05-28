using Chaos.Extensions.Common;
using Chaos.Extensions.DependencyInjection;
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

using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Sentry;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using ILobbyClient = Darkages.Network.Client.Abstractions.ILobbyClient;
using ILoginClient = Darkages.Network.Client.Abstractions.ILoginClient;
using IWorldClient = Darkages.Network.Client.Abstractions.IWorldClient;
using Darkages.Network.Client.Abstractions;
using Darkages.Network.Server.Abstractions;

namespace Zolian.GameServer;

public partial class App
{
    private CancellationTokenSource ServerCtx { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledException;

        base.OnStartup(e);
        ServerCtx = new CancellationTokenSource();
        var path = Path.Combine(Directory.GetCurrentDirectory(), "SentrySecrets.txt");
        var secret = File.ReadLines(path).First();

        SentrySdk.Init(o =>
        {
            o.Dsn = secret;
            o.Debug = false;
            o.TracesSampleRate = 1.0;
            o.ProfilesSampleRate = 0.2;
        });

        var providers = new LoggerProviderCollection();
        const string logTemplate = "[{Timestamp:MMM-dd HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}";

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("_Zolian_logs_.txt", LogEventLevel.Verbose, logTemplate, rollingInterval: RollingInterval.Day)
            .WriteTo.Console(LogEventLevel.Verbose, logTemplate, theme: AnsiConsoleTheme.Literate)
            .CreateLogger();

        Win32.AllocConsole();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("ServerConfig.json");

        try
        {
            var config = builder.Build();
            var constants = config.GetSection("ServerConfig").Get<ServerConstants>();
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddOptions()
                .AddSingleton(providers)
                .AddSingleton<ILoggerFactory>(sc =>
                {
                    var providerCollection = sc.GetService<LoggerProviderCollection>();
                    var factory = new SerilogLoggerFactory(null, true, providerCollection);
                    foreach (var provider in sc.GetServices<ILoggerProvider>())
                        factory.AddProvider(provider);
                    return factory;
                })
                .AddLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Trace))
                .Configure<ServerOptions>(config.GetSection("Content"));
            serviceCollection.AddSingleton<IServerConstants, ServerConstants>(_ => constants)
                .AddSingleton<IServerContext, ServerSetup>()
                .AddSingleton<IServer, Server>();
            serviceCollection.AddCryptography();
            serviceCollection.AddPacketSerializer();
            serviceCollection.AddSingleton<IRedirectManager, RedirectManager>();

            // Lobby
            serviceCollection.AddSingleton<IClientFactory<LobbyClient>, ClientFactory<LobbyClient>>();
            serviceCollection.AddSingleton<ILobbyServer<ILobbyClient>, IHostedService, LobbyServer>();
            serviceCollection.AddSingleton<IClientRegistry<ILobbyClient>, ClientRegistry<ILobbyClient>>();

            // Login
            serviceCollection.AddSingleton<IClientFactory<LoginClient>, ClientFactory<LoginClient>>();
            serviceCollection.AddSingleton<ILoginServer<ILoginClient>, IHostedService, LoginServer>();
            serviceCollection.AddSingleton<IClientRegistry<ILoginClient>, ClientRegistry<ILoginClient>>();

            // World
            serviceCollection.AddSingleton<IClientFactory<WorldClient>, ClientFactory<WorldClient>>();
            serviceCollection.AddSingleton<IWorldServer<IWorldClient>, IHostedService, WorldServer>();
            serviceCollection.AddSingleton<IClientRegistry<IWorldClient>, ClientRegistry<IWorldClient>>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.GetService<IServer>();
            var hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();

            // Start the hosted services in a dedicated long-running task
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    await Task.WhenAll(hostedServices.Select(svc => svc.StartAsync(ServerCtx.Token)));
                    await ServerCtx.Token.WaitTillCanceled().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    SentrySdk.CaptureException(exception);
                }
            }, ServerCtx.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        catch (Exception exception)
        {
            SentrySdk.CaptureException(exception);
        }
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        SentrySdk.CaptureException(e.Exception);
        e.Handled = true;
    }

    private static void GlobalUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.IsTerminating)
        {
            SentrySdk.CaptureException(e.ExceptionObject as Exception ?? throw new InvalidOperationException());
        }
        else
        {
            SentrySdk.CaptureMessage($"{e.ExceptionObject}");
        }
    }
}