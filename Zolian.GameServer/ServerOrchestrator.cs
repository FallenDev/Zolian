using System;
using System.Threading;
using System.Threading.Tasks;

using Darkages.Network.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Zolian.GameServer;

public sealed class ServerOrchestrator : BackgroundService
{
    private readonly IServiceProvider _services;

    public ServerOrchestrator(IServiceProvider services) => _services = services;

    /// <summary>
    ///     ServerOrchestrator is registered as an IHostedService; when the host starts,
    ///     StartAsync() is called, which in turn runs ExecuteAsync() on a background task
    ///     that performs server initialization and startup until shutdown is requested.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Runs bootstrap/init deterministically
        // Triggers ServerSetup.Start() via Server class
        _ = _services.GetRequiredService<IServer>();

        // Start listeners in order
        var lobby = _services.GetRequiredService<LobbyServer>();
        await lobby.StartAsync(stoppingToken).ConfigureAwait(false);

        var login = _services.GetRequiredService<LoginServer>();
        await login.StartAsync(stoppingToken).ConfigureAwait(false);

        var world = _services.GetRequiredService<WorldServer>();
        await world.StartAsync(stoppingToken).ConfigureAwait(false);

        // Keep running until stopped
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop in reverse order if they were started
        // Use GetService so stop doesn’t throw if something never started
        var world = _services.GetService<WorldServer>();
        if (world is not null) await world.StopAsync(cancellationToken).ConfigureAwait(false);

        var login = _services.GetService<LoginServer>();
        if (login is not null) await login.StopAsync(cancellationToken).ConfigureAwait(false);

        var lobby = _services.GetService<LobbyServer>();
        if (lobby is not null) await lobby.StopAsync(cancellationToken).ConfigureAwait(false);

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
