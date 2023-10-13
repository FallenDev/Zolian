using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Chaos.Extensions.Common;
using Chaos.Extensions.DependencyInjection;
using Chaos.Networking;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities;
using Darkages;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Network.Server;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Zolian.GameServer;

public partial class App
{
    private CancellationTokenSource ServerCtx { get; set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledException;

        base.OnStartup(e);
        ServerCtx = new CancellationTokenSource();

        SetCountryCode();
        await Crashes.SetEnabledAsync(true);
        await Analytics.SetEnabledAsync(true);

        var path = Directory.GetCurrentDirectory() + "\\AppCenterAPIKeys.txt";
        var debugKey = File.ReadLines(path).Skip(1).Take(1).First();
        var releaseKey = File.ReadLines(path).Skip(4).Take(1).First();

#if DEBUG
        AppCenter.Start(debugKey,
            typeof(Analytics), typeof(Crashes));
#endif
#if RELEASE
            AppCenter.Start(releaseKey,
                typeof(Analytics), typeof(Crashes));
#endif

        var providers = new LoggerProviderCollection();
        const string logTemplate = "[{Timestamp:MMM-dd HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}";

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("_Zolian_General.txt", LogEventLevel.Verbose, logTemplate)
            .WriteTo.Async(wt => wt.Console(LogEventLevel.Verbose, logTemplate, theme: AnsiConsoleTheme.Literate))
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
            serviceCollection.AddSingleton<ILobbyServer<LobbyClient>, IHostedService, LobbyServer>();
            serviceCollection.AddSingleton<IClientRegistry<ILobbyClient>, ClientRegistry<ILobbyClient>>();

            // Login
            serviceCollection.AddSingleton<IClientFactory<LoginClient>, ClientFactory<LoginClient>>();
            serviceCollection.AddSingleton<ILoginServer<LoginClient>, IHostedService, LoginServer>();
            serviceCollection.AddSingleton<IClientRegistry<ILoginClient>, ClientRegistry<ILoginClient>>();

            // World
            serviceCollection.AddSingleton<IClientFactory<WorldClient>, ClientFactory<WorldClient>>();
            serviceCollection.AddSingleton<IWorldServer<WorldClient>, IHostedService, WorldServer>();
            serviceCollection.AddSingleton<IClientRegistry<IWorldClient>, ClientRegistry<IWorldClient>>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.GetService<IServer>();

            var hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
            await Task.Run(async () =>
            {
                await Task.WhenAll(hostedServices.Select(svc => svc.StartAsync(ServerCtx.Token)));
                await ServerCtx.Token.WaitTillCanceled().ConfigureAwait(false);
            });
        }
        catch (Exception exception)
        {
            ServerSetup.Logger($"{exception}");
        }
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Crashes.TrackError(e.Exception);
        e.Handled = true;
    }

    private static void GlobalUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.IsTerminating)
        {
            Crashes.TrackError(e.ExceptionObject as Exception);
        }
        else
        {
            Analytics.TrackEvent($"{e.ExceptionObject}");
        }
    }

    private static void SetCountryCode()
    {
        var countryCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;
        AppCenter.SetCountryCode(countryCode);
    }
}