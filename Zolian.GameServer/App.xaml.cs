﻿using Chaos.Extensions.Common;
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
using Sentry.Profiling;
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
        var path = Directory.GetCurrentDirectory() + "\\SentrySecret.txt";
        var debugKey = File.ReadLines(path).Skip(1).Take(1).First();

        SentrySdk.Init(o =>
        {
            // Tells which project in Sentry to send events to:
            // When configuring for the first time, to see what the SDK is doing:
            o.Debug = true;
            // Set TracesSampleRate to 1.0 to capture 100% of transactions for performance monitoring.
            // We recommend adjusting this value in production.
            o.TracesSampleRate = 1.0;
            // Sample rate for profiling, applied on top of othe TracesSampleRate,
            // e.g. 0.2 means we want to profile 20 % of the captured transactions.
            // We recommend adjusting this value in production.
            o.ProfilesSampleRate = 1.0;
            // Requires NuGet package: Sentry.Profiling
            // Note: By default, the profiler is initialized asynchronously. This can
            // be tuned by passing a desired initialization timeout to the constructor.
            o.AddIntegration(new ProfilingIntegration(
                // During startup, wait up to 500ms to profile the app startup code.
                // This could make launching the app a bit slower so comment it out if you
                // prefer profiling to start asynchronously
                TimeSpan.FromMilliseconds(500)
            ));
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
            Crashes.TrackError(exception);
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
}