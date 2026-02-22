using System.Net;
using System.Text;
using System.Text.Json;

using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;

namespace Darkages.Network.Components;

/// <summary>
/// World-status API for external web integrations
/// </summary>
public sealed class WebApiComponent(WorldServer server) : WorldServerComponent(server)
{
    private HttpListener _listener;

    protected internal override async Task Update()
    {
        if (!ServerSetup.Instance.WebApiEnabled)
            return;

        var prefix = ServerSetup.Instance.WebApiPrefix;

        if (string.IsNullOrWhiteSpace(prefix))
        {
            ServerSetup.EventsLogger("Web API disabled: WebApiPrefix is empty.");
            return;
        }

        _listener = new HttpListener();

        try
        {
            _listener.Prefixes.Add(prefix);
            _listener.Start();
            ServerSetup.EventsLogger($"Web API listening on {prefix}");

            // Main loop to handle incoming requests
            while (ServerSetup.Instance.Running && _listener.IsListening)
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleRequest(context));
            }
        }
        catch (HttpListenerException ex)
        {
            ServerSetup.EventsLogger($"Web API listener error: {ex.Message}", Microsoft.Extensions.Logging.LogLevel.Error);
            SentrySdk.CaptureException(ex);
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger($"Web API fatal error: {ex.Message}", Microsoft.Extensions.Logging.LogLevel.Critical);
            SentrySdk.CaptureException(ex);
        }
        finally
        {
            try
            {
                if (_listener.IsListening)
                    _listener.Stop();

                _listener.Close();
            }
            catch { }
        }
    }

    private static async Task HandleRequest(HttpListenerContext context)
    {
        try
        {
            var req = context.Request;
            var res = context.Response;

            ApplyCors(res);

            if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                res.StatusCode = (int)HttpStatusCode.NoContent;
                res.Close();
                return;
            }

            if (!IsAuthorized(req))
            {
                await WriteJson(res, HttpStatusCode.Unauthorized, new { error = "Unauthorized" }).ConfigureAwait(false);
                return;
            }

            if (req.Url == null)
            {
                await WriteJson(res, HttpStatusCode.BadRequest, new { error = "Invalid request" }).ConfigureAwait(false);
                return;
            }

            var path = req.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant();

            // Simple health check endpoint
            if (path is "/health")
            {
                await WriteJson(res, HttpStatusCode.OK, new { status = "ok" }).ConfigureAwait(false);
                return;
            }

            // Main endpoint to get world snapshot
            if (path is "/api/world")
            {
                var payload = BuildWorldSnapshot();
                await WriteJson(res, HttpStatusCode.OK, payload).ConfigureAwait(false);
                return;
            }

            await WriteJson(res, HttpStatusCode.NotFound, new { error = "Not found" }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger($"Web API request error: {ex.Message}", Microsoft.Extensions.Logging.LogLevel.Error);
            SentrySdk.CaptureException(ex);

            try
            {
                if (context.Response.OutputStream.CanWrite)
                    await WriteJson(context.Response, HttpStatusCode.InternalServerError, new { error = "Internal server error" }).ConfigureAwait(false);
            }
            catch { }
        }
    }

    private static object BuildWorldSnapshot()
    {
        var players = 0;
        Server.ForEachLoggedInAisling(_ => players++);

        var mapsWithPlayers = 0;
        var monstersAlive = 0;

        foreach (var map in ServerSetup.Instance.GlobalMapCache.Values)
        {
            if (map.HasPlayers)
                mapsWithPlayers++;

            monstersAlive += ObjectService.CountWithPredicate<Monster>(map, static m => m is { IsAlive: true });
        }

        return new
        {
            utc = DateTime.UtcNow,
            playersLoggedIn = players,
            monstersSpawned = monstersAlive,
            xpRate = ServerSetup.Instance.Config.HolidayExpBonus,
            activeMaps = mapsWithPlayers,
            serverTitle = ServerSetup.Instance.Config.SERVER_TITLE
        };
    }

    private static bool IsAuthorized(HttpListenerRequest req)
    {
        var configuredKey = ServerSetup.Instance.WebApiApiKey;

        // If no API key is configured, deny access
        if (string.IsNullOrWhiteSpace(configuredKey))
            return false;

        var providedKey = req.Headers["X-Api-Key"];

        // If not provided in header, check query string for backward compatibility
        if (string.IsNullOrWhiteSpace(providedKey))
            providedKey = req.QueryString["apiKey"];

        return string.Equals(configuredKey, providedKey, StringComparison.Ordinal);
    }

    private static void ApplyCors(HttpListenerResponse res)
    {
        var allowedOrigin = ServerSetup.Instance.WebApiAllowedOrigin;

        // If no allowed origin is configured, default to allowing all origins
        if (string.IsNullOrWhiteSpace(allowedOrigin))
            allowedOrigin = "*";

        res.Headers["Access-Control-Allow-Origin"] = allowedOrigin;
        res.Headers["Access-Control-Allow-Headers"] = "Content-Type, X-Api-Key";
        res.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
    }

    private static async Task WriteJson(HttpListenerResponse res, HttpStatusCode code, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var data = Encoding.UTF8.GetBytes(json);

        res.StatusCode = (int)code;
        res.ContentType = "application/json";
        res.ContentEncoding = Encoding.UTF8;
        res.ContentLength64 = data.Length;

        await res.OutputStream.WriteAsync(data).ConfigureAwait(false);
        res.Close();
    }
}
