using System.Diagnostics;

using Darkages.Network.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Xunit;

using ZolianTest.Integration.Hosting;

namespace ZolianTest.Integration.Fixtures;

public sealed class ZolianHostFixture : IAsyncLifetime
{
    private IHost? _host;

    public IHost Host => _host!;
    public ServerSetup Setup => _host!.Services.GetRequiredService<ServerSetup>();
    public WorldServer WorldServer => _host!.Services.GetRequiredService<WorldServer>();

    public async ValueTask InitializeAsync()
    {
        // If you used the csproj “CopyToOutputDirectory” approach:
        var configPath = Path.Combine(AppContext.BaseDirectory, "ServerConfig.json");

        _host = ZolianTestHost.BuildHost(configPath);

        await _host.StartAsync().ConfigureAwait(false);

        // Gate: server is considered "up" when Running flips true
        await WaitUntilAsync(
            predicate: static () => ServerSetup.Instance.Running,
            timeout: TimeSpan.FromSeconds(30),
            poll: TimeSpan.FromMilliseconds(25)
        ).ConfigureAwait(false);

        // Optional hardening (recommended): Running + one real invariant
        // await WaitUntilAsync(
        //     predicate: () => ServerSetup.Instance.Running && Setup.GlobalMapCache.Count > 0,
        //     timeout: TimeSpan.FromSeconds(30),
        //     poll: TimeSpan.FromMilliseconds(25)
        // ).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is null) return;

        await _host.StopAsync().ConfigureAwait(false);

        if (_host is IAsyncDisposable ad)
            await ad.DisposeAsync().ConfigureAwait(false);
        else
            _host.Dispose();

        _host = null;
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout, TimeSpan poll)
    {
        var sw = Stopwatch.StartNew();

        while (!predicate())
        {
            if (sw.Elapsed > timeout)
                throw new TimeoutException("Server did not reach the ready gate within the timeout.");

            await Task.Delay(poll).ConfigureAwait(false);
        }
    }
}
