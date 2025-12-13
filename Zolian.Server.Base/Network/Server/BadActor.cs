using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Darkages.Models;
using ServiceStack.Text;

namespace Darkages.Network.Server;

public class ReportInfo
{
    public string RemoteIp { get; init; }
    public string Comment { get; init; }
    public DateTime AttemptTime { get; set; }
}

public static class BadActor
{
    public static readonly IMemoryCache IpCache = new MemoryCache(new MemoryCacheOptions());

    // Good actors: cache “clean” for 30 days
    private static readonly TimeSpan CacheDurationGoodActor = TimeSpan.FromDays(30);
    // Bad actors: re-check every 5 days so reputation/ownership can change
    private static readonly TimeSpan CacheDurationBadActor = TimeSpan.FromDays(5);
    // AbuseIPDB API is down or unreachable
    private static readonly TimeSpan CacheDurationFailure = TimeSpan.FromMinutes(10);

    private static readonly ConcurrentQueue<ReportInfo> RetryQueue = [];
    private static bool _isProcessingQueue;

    private const string InternalIp = "192.168.50.1";

    private const string CategoryOpenProxy = "9";
    private const string CategoryWebSpam = "10";
    private const string CategorySsh = "14";
    private const string CategoryPortScan = "15";
    private const string CategoryHacking = "16";
    private const string CategoryDDoS = "4";

    private readonly struct IpCacheEntry
    {
        public bool IsBlocked { get; init; }
    }

    public static async Task<bool> ClientOnBlackListAsync(string remoteIp)
    {
        // Malformed / empty IPs are always blocked
        if (string.IsNullOrWhiteSpace(remoteIp) || !IPAddress.TryParse(remoteIp, out _)) return true;

        // Localhost and internal IPs are never blocked
        if (remoteIp is "127.0.0.1" or InternalIp) return false;

        // Is the IP cached? Return the cached result
        if (IpCache.TryGetValue(remoteIp, out IpCacheEntry entry)) return entry.IsBlocked;

        try
        {
            var keyCode = ServerSetup.Instance.KeyCode;
            if (!IsKeyCodeValid(keyCode))
            {
                ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
                return false;
            }

            var request = new RestRequest("", Method.Get);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ipAddress", remoteIp);
            request.AddParameter("maxAgeInDays", "90");
            request.AddParameter("verbose", "");

            var response = await ServerSetup.Instance.RestClient.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                var ipdb = JsonConvert.DeserializeObject<Ipdb>(response.Content!);
                var isPublic = ipdb?.Data?.IsPublic ?? false;
                var ipVersion = ipdb?.Data?.IpVersion ?? 4;
                var abuseScore = ipdb?.Data?.AbuseConfidenceScore ?? 0;
                var tor = ipdb?.Data?.IsTor ?? false;
                var usageType = ipdb?.Data?.UsageType;
                var isp = ipdb?.Data?.Isp;
                var shouldBlock = false;
                var isBlocked = tor || abuseScore >= 5;
                var isDisallowed = IsDisallowedUsageType(usageType);
                var isVpnBot = IsVpnBotUsageType(usageType) && abuseScore >= 3;

                // 1) Hard-disallowed: IPv6, non-public, disallowed usage, or blacklisted ISP
                if (ipVersion == 6 || isPublic == false || isDisallowed || IsBlackListed(isp))
                {
                    IpCache.Set(remoteIp, new IpCacheEntry { IsBlocked = true }, CacheDurationBadActor);
                    return true;
                }

                // 2) VPN / data-center with bad abuse score
                if (isVpnBot)
                {
                    shouldBlock = true;

                    if (IsWhiteListed(isp))
                        shouldBlock = false;
                }

                // 3) General abuse / TOR handling
                if (isBlocked)
                {
                    shouldBlock = true;

                    if (tor)
                    {
                        LogTor(remoteIp, "using the onion network.");
                    }
                    else
                    {
                        if (IsWhiteListed(isp))
                            shouldBlock = false;
                        else
                            LogBadActor(remoteIp, $"abuse score {abuseScore}");
                    }
                }

                // 4) Cache result with different TTLs for good vs bad actors
                var ttl = shouldBlock ? CacheDurationBadActor : CacheDurationGoodActor;

                IpCache.Set(remoteIp, new IpCacheEntry { IsBlocked = shouldBlock }, ttl);
                return shouldBlock;
            }

            ServerSetup.ConnectionLogger($"{remoteIp} - API Issue or Failed response");
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger($"Error checking blacklist for {remoteIp}: {ex.Message}", LogLevel.Warning);
            SentrySdk.CaptureException(ex);
        }

        // Fallback: API failure / unexpected flow -> treat as not blocked, but cache briefly
        IpCache.Set(remoteIp, new IpCacheEntry { IsBlocked = false }, CacheDurationFailure);
        return false;
    }

    public static void StartProcessingQueue()
    {
        if (_isProcessingQueue) return;
        _isProcessingQueue = true;
        _ = Task.Run(ProcessRetryQueueAsync);
    }

    private static async Task ProcessRetryQueueAsync()
    {
        try
        {
            while (!RetryQueue.IsEmpty)
            {
                if (RetryQueue.TryDequeue(out var report))
                {
                    var elapsed = DateTime.UtcNow - report.AttemptTime;

                    if (elapsed.TotalMinutes >= 1)
                    {
                        var success = await ReportSuspiciousEndpointWithDDoS(report);
                        if (success)
                        {
                            ServerSetup.ConnectionLogger($"Successfully reported {report.RemoteIp} after retry.");
                        }
                        else
                        {
                            report.AttemptTime = DateTime.UtcNow;
                            RetryQueue.Enqueue(report);
                            ServerSetup.ConnectionLogger($"Retry failed for {report.RemoteIp}, re-queued.");
                        }
                    }
                    else
                    {
                        report.AttemptTime = DateTime.UtcNow;
                        RetryQueue.Enqueue(report);
                    }
                }

                await Task.Delay(5000);
            }
        }
        finally
        {
            _isProcessingQueue = false;
        }
    }

    private static async Task<bool> ReportSuspiciousEndpointWithDDoS(ReportInfo report)
    {
        var comment = $"{report.Comment} - DDoS";

        try
        {
            if (!IsKeyCodeValid(ServerSetup.Instance.KeyCode))
            {
                ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
                return false;
            }

            var request = new RestRequest("", Method.Post);
            request.AddHeader("Key", ServerSetup.Instance.KeyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ip", report.RemoteIp);
            request.AddParameter("categories", $"{CategoryDDoS}, {CategoryWebSpam}, {CategorySsh}");
            request.AddParameter("comment", comment);

            var response = await ServerSetup.Instance.RestReport.ExecuteAsync(request);
            return response.IsSuccessful;
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger($"Retry DDoS report failed: {ex.Message}", LogLevel.Warning);
            SentrySdk.CaptureException(ex);
            return false;
        }
    }

    public static void ReportMaliciousEndpoint(string remoteIp, string comment)
    {
        if (!IsKeyCodeValid(ServerSetup.Instance.KeyCode))
        {
            ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
            return;
        }

        try
        {
            var request = new RestRequest("", Method.Post);
            request.AddHeader("Key", ServerSetup.Instance.KeyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ip", remoteIp);
            request.AddParameter("categories", $"{CategorySsh}, {CategoryPortScan}, {CategoryHacking}, {CategoryWebSpam}");
            request.AddParameter("comment", comment);

            var response = ServerSetup.Instance.RestReport.Execute(request);

            if (response.IsSuccessful) return;
            ServerSetup.ConnectionLogger($"Error reporting {remoteIp} : {comment}");
            SentrySdk.CaptureMessage($"Error reporting {remoteIp} : {comment}");
        }
        catch (HttpRequestException httpEx) when (httpEx.Message.Contains("TooManyRequests"))
        {
            ServerSetup.ConnectionLogger($"Too many requests when reporting {remoteIp}", LogLevel.Warning);
            AddToRetryQueue(remoteIp, comment);
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger($"Exception while reporting {remoteIp}: {ex.Message}", LogLevel.Warning);
            SentrySdk.CaptureException(ex);
        }
    }

    private static void ReportSuspiciousEndpoint(string remoteIp, string comment)
    {
        if (!IsKeyCodeValid(ServerSetup.Instance.KeyCode))
        {
            ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
            return;
        }

        try
        {
            var request = new RestRequest("", Method.Post);
            request.AddHeader("Key", ServerSetup.Instance.KeyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ip", remoteIp);
            request.AddParameter("categories", $"{CategoryWebSpam}, {CategorySsh}");
            request.AddParameter("comment", comment);

            var response = ServerSetup.Instance.RestReport.Execute(request);

            if (response.IsSuccessful) return;
            ServerSetup.ConnectionLogger($"Error reporting {remoteIp} : {comment}");
            SentrySdk.CaptureMessage($"Error reporting {remoteIp} : {comment}");
        }
        catch (HttpRequestException httpEx) when (httpEx.Message.Contains("TooManyRequests"))
        {
            ServerSetup.ConnectionLogger($"Too many requests when reporting {remoteIp}", LogLevel.Warning);
            AddToRetryQueue(remoteIp, comment);
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger($"Exception while reporting {remoteIp}: {ex.Message}", LogLevel.Warning);
            SentrySdk.CaptureException(ex);
        }
    }

    private static void ReportTorEndpoint(string remoteIp, string comment)
    {
        if (!IsKeyCodeValid(ServerSetup.Instance.KeyCode))
        {
            ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
            return;
        }

        try
        {
            comment = "Attempted to access restricted space with Tor";
            var request = new RestRequest("", Method.Post);
            request.AddHeader("Key", ServerSetup.Instance.KeyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ip", remoteIp);
            request.AddParameter("categories", $"{CategoryOpenProxy}, {CategoryWebSpam}");
            request.AddParameter("comment", comment);

            var response = ServerSetup.Instance.RestReport.Execute(request);

            if (response.IsSuccessful) return;
            ServerSetup.ConnectionLogger($"Error reporting {remoteIp} : {comment}");
            SentrySdk.CaptureMessage($"Error reporting {remoteIp} : {comment}");
        }
        catch (HttpRequestException httpEx) when (httpEx.Message.Contains("TooManyRequests"))
        {
            ServerSetup.ConnectionLogger($"Too many requests when reporting {remoteIp}", LogLevel.Warning);
            AddToRetryQueue(remoteIp, comment);
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger($"Exception while reporting {remoteIp}: {ex.Message}", LogLevel.Warning);
            SentrySdk.CaptureException(ex);
        }
    }


    private static void AddToRetryQueue(string remoteIp, string comment)
    {
        RetryQueue.Enqueue(new ReportInfo
        {
            RemoteIp = remoteIp,
            Comment = comment,
            AttemptTime = DateTime.UtcNow
        });

        StartProcessingQueue();
    }

    private static void LogBadActor(string remoteIp, string reason)
    {
        ServerSetup.ConnectionLogger($"Blocking {remoteIp} - Reason: {reason}", LogLevel.Warning);
        SentrySdk.CaptureMessage($"{remoteIp} blocked due to {reason}");
        ReportMaliciousEndpoint(remoteIp, $"Blocked due to {reason}");
    }

    private static void LogTor(string remoteIp, string reason)
    {
        ServerSetup.ConnectionLogger($"Blocking {remoteIp} - Reason: {reason}", LogLevel.Warning);
        SentrySdk.CaptureMessage($"{remoteIp} blocked due to {reason}");
        ReportTorEndpoint(remoteIp, $"Blocked due to {reason}");
    }

    private static bool IsDisallowedUsageType(string? usageType)
    {
        return usageType switch
        {
            "Commercial" or "Organization" or "Government" or "Military" or "Content Delivery Network" => true,
            _ => false
        };
    }

    private static bool IsVpnBotUsageType(string? usageType)
    {
        return usageType switch
        {
            "Data Center/Web Hosting/Transit" => true,
            _ => false
        };
    }

    private static bool IsWhiteListed(string? isp) => isp switch
    {
        "Erisco LLC" => true,
        _ => false
    };

    private static bool IsBlackListed(string? isp) => isp switch
    {
        "Driftnet Ltd" or "DigitalOcean, LLC" => true,
        _ => false
    };

    private static readonly HashSet<string> _bannedIPs = [];

    public static bool BannedIpCheck(string ip)
    {
        if (ip.IsNullOrEmpty()) return true;

        // Check against known bad actors cache
        var knownBadActor = BadActor.IpCache.TryGetValue(ip, out _);
        if (knownBadActor) return true;

        // Add banned player IPs to the _bannedIPs HashSet
        _bannedIPs.Add("0.0.0.0");

        return _bannedIPs.Contains(ip);
    }

    private static bool IsKeyCodeValid(string? keyCode) => !string.IsNullOrWhiteSpace(keyCode);
}
