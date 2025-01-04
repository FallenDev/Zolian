using System.Collections.Concurrent;
using System.Net;

using Darkages.Models;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using RestSharp;

using ServiceStack;

namespace Darkages.Network.Server;

public class ReportInfo
{
    public string RemoteIp { get; init; }
    public string Comment { get; init; }
    public DateTime FirstAttemptTime { get; init; }
}

public static class BadActor
{
    private static readonly ConcurrentQueue<ReportInfo> RetryQueue = [];
    private static bool _isProcessingQueue;
    private const string InternalIP = "192.168.50.1"; // Cannot use ServerConfig due to value needing to be constant

    public static void StartProcessingQueue()
    {
        if (_isProcessingQueue) return;

        _isProcessingQueue = true;
        Task.Run(ProcessRetryQueue);
    }

    public static bool ClientOnBlackList(string remoteIp)
    {
        if (remoteIp.IsNullOrEmpty() || !IPAddress.TryParse(remoteIp, out _)) return true;
        if (remoteIp is "127.0.0.1" or InternalIP) return false;

        try
        {
            var keyCode = ServerSetup.Instance.KeyCode;
            if (keyCode is null || keyCode.Length == 0)
            {
                ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
                return false;
            }

            // BLACKLIST check
            var request = new RestRequest("", Method.Get);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ipAddress", remoteIp);
            request.AddParameter("maxAgeInDays", "90");
            request.AddParameter("verbose", "");
            var response = ExecuteWithRetry(() => ServerSetup.Instance.RestClient.Execute<Ipdb>(request));

            if (response.Result?.IsSuccessful == true)
            {
                var ipdb = JsonConvert.DeserializeObject<Ipdb>(response.Result.Content!);
                var abuseConfidenceScore = ipdb?.Data?.AbuseConfidenceScore;
                var tor = ipdb?.Data?.IsTor;
                var usageType = ipdb?.Data?.UsageType;

                // Block if using tor, potentially malicious
                if (tor == true)
                {
                    LogBadActor(remoteIp, "using tor, potentially malicious");
                    return true;
                }

                // Block if an unauthorized usage type
                if (IsBlockedUsageType(usageType))
                {
                    LogBlockedType(remoteIp, $"using {usageType}");
                    return true;
                }

                // Block based on abuse confidence score
                if (abuseConfidenceScore >= 5)
                {
                    LogBadActor(remoteIp, $"high risk score of {abuseConfidenceScore}");
                    return true;
                }
            }
            else
            {
                ServerSetup.ConnectionLogger($"{remoteIp} - API Issue or Failed response");
            }
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger($"Error checking blacklist for {remoteIp}: {ex.Message}", LogLevel.Warning);
            SentrySdk.CaptureException(ex);
        }

        return false;
    }

    private static async Task<T> ExecuteWithRetry<T>(Func<T> operation, int maxRetries = 3)
    {
        var attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= maxRetries)
                {
                    ServerSetup.ConnectionLogger($"Max retries reached. Operation failed: {ex.Message}", LogLevel.Warning);
                    SentrySdk.CaptureException(ex);
                    return default(T);
                }

                // Wait before retrying
                await Task.Delay(300); // Retry delay
            }
        }

        return default(T);
    }

    private static void LogBadActor(string remoteIp, string reason)
    {
        ServerSetup.ConnectionLogger($"Blocking {remoteIp} - Reason: {reason}", LogLevel.Warning);
        SentrySdk.CaptureMessage($"{remoteIp} blocked due to {reason}");
        ReportMaliciousEndpoint(remoteIp, $"Blocked due to {reason}");
    }

    private static void LogBlockedType(string remoteIp, string reason)
    {
        ServerSetup.ConnectionLogger($"Blocking {remoteIp} - Using: {reason}", LogLevel.Warning);
        ReportSuspiciousEndpoint(remoteIp, "Blocked due to Web Spam, Port Scanning");
    }

    private static bool IsBlockedUsageType(string usageType)
    {
        return usageType switch
        {
            "Commercial" or "Organization" or "Government" or "Military" or "Content Delivery Network" or "Data Center/Web Hosting/Transit" => true,
            _ => false
        };
    }

    public static void ReportMaliciousEndpoint(string remoteIp, string comment)
    {
        var keyCode = ServerSetup.Instance.KeyCode;
        if (keyCode is null || keyCode.Length == 0)
        {
            ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
            return;
        }

        try
        {
            var request = new RestRequest("", Method.Post);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ip", remoteIp);
            request.AddParameter("categories", "14, 15, 16, 21");
            request.AddParameter("comment", comment);
            var response = ExecuteWithRetry(() => ServerSetup.Instance.RestReport.Execute(request));

            if (response.Result?.IsSuccessful == true) return;
            ServerSetup.ConnectionLogger($"Error reporting {remoteIp} : {comment}");
            SentrySdk.CaptureMessage($"Error reporting {remoteIp} : {comment}");
        }
        catch (HttpRequestException httpEx) when (httpEx.Message.Contains("TooManyRequests"))
        {
            ServerSetup.ConnectionLogger($"Exception while reporting {remoteIp}: Too many requests.", LogLevel.Warning);
            // add to queue where we report them also for DDoS (4)
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
        var keyCode = ServerSetup.Instance.KeyCode;
        if (keyCode is null || keyCode.Length == 0)
        {
            ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
            return;
        }

        try
        {
            var request = new RestRequest("", Method.Post);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ip", remoteIp);
            request.AddParameter("categories", "10, 14");
            request.AddParameter("comment", comment);
            var response = ExecuteWithRetry(() => ServerSetup.Instance.RestReport.Execute(request));

            if (response.Result?.IsSuccessful == true) return;
            ServerSetup.ConnectionLogger($"Error reporting {remoteIp} : {comment}");
            SentrySdk.CaptureMessage($"Error reporting {remoteIp} : {comment}");
        }
        catch (HttpRequestException httpEx) when (httpEx.Message.Contains("TooManyRequests"))
        {
            ServerSetup.ConnectionLogger($"Exception while reporting {remoteIp}: Too many requests.", LogLevel.Warning);
            // add to queue where we report them also for DDoS (4)
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
            FirstAttemptTime = DateTime.Now
        });
    }

    private static async Task ProcessRetryQueue()
    {
        while (_isProcessingQueue)
        {
            if (!RetryQueue.IsEmpty)
            {
                // Process each report in the queue
                if (RetryQueue.TryDequeue(out var report))
                {
                    var elapsedTime = DateTime.Now - report.FirstAttemptTime;

                    if (elapsedTime.TotalMinutes >= 1)
                    {
                        var success = await ReportSuspiciousEndpointWithDDoS(report);
                        if (success)
                        {
                            ServerSetup.ConnectionLogger($"Successfully reported {report.RemoteIp} after retry.");
                        }
                        else
                        {
                            RetryQueue.Enqueue(report);
                            ServerSetup.ConnectionLogger($"Retry failed for {report.RemoteIp}, re-queued.");
                        }
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1) - elapsedTime);
                        RetryQueue.Enqueue(report);
                    }
                }
            }

            await Task.Delay(5000);
        }
    }

    private static async Task<bool> ReportSuspiciousEndpointWithDDoS(ReportInfo report)
    {
        var adjustedComment = $"{report.Comment} - DDoS";

        try
        {
            var keyCode = ServerSetup.Instance.KeyCode;
            if (string.IsNullOrEmpty(keyCode))
            {
                ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
                return false;
            }

            var request = new RestRequest("", Method.Post);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ip", report.RemoteIp);
            request.AddParameter("categories", "4, 10, 14");
            request.AddParameter("comment", adjustedComment);

            var response = await ServerSetup.Instance.RestReport.ExecuteAsync(request);
            return response.IsSuccessful;
        }
        catch
        {
            return false;
        }
    }
}