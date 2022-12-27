using Newtonsoft.Json;

namespace Darkages.Models;

public class Source
{
    [JsonProperty("parameter")]
    public string Parameter { get; set; }
}

public class Error
{
    [JsonProperty("detail")]
    public string Detail { get; set; }

    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("source")]
    public Source Source { get; set; }
}

public class Report
{
    [JsonProperty("reportedAt")]
    public DateTime ReportedAt { get; set; }

    [JsonProperty("comment")]
    public string Comment { get; set; }

    [JsonProperty("categories")]
    public List<int> Categories { get; set; }

    [JsonProperty("reporterId")]
    public int ReporterId { get; set; }

    [JsonProperty("reporterCountryCode")]
    public string ReporterCountryCode { get; set; }

    [JsonProperty("reporterCountryName")]
    public string ReporterCountryName { get; set; }
}

public class Data
{
    [JsonProperty("ipAddress")]
    public string IpAddress { get; set; }

    [JsonProperty("isPublic")]
    public bool? IsPublic { get; set; }

    [JsonProperty("ipVersion")]
    public int IpVersion { get; set; }

    [JsonProperty("isWhitelisted")]
    public bool? IsWhitelisted { get; set; }

    [JsonProperty("abuseConfidenceScore")]
    public int AbuseConfidenceScore { get; set; }

    [JsonProperty("countryCode")]
    public string CountryCode { get; set; }

    [JsonProperty("countryName")]
    public string CountryName { get; set; }

    [JsonProperty("usageType")]
    public string UsageType { get; set; }

    [JsonProperty("isp")]
    public string Isp { get; set; }

    [JsonProperty("domain")]
    public string Domain { get; set; }

    [JsonProperty("hostnames")]
    public List<object> Hostnames { get; set; }

    [JsonProperty("totalReports")]
    public int TotalReports { get; set; }

    [JsonProperty("numDistinctUsers")]
    public int NumDistinctUsers { get; set; }

    [JsonProperty("lastReportedAt")]
    public DateTime? LastReportedAt { get; set; }

    [JsonProperty("reports")]
    public List<Report> Reports { get; set; }

    [JsonProperty("errors")]
    public List<Error> Errors { get; set; }
}

public class Ipdb
{
    [JsonProperty("data")]
    public Data Data { get; set; }
}