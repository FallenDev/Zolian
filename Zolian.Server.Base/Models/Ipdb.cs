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
    /// <summary>
    /// IP Address requested for the report
    /// </summary>
    [JsonProperty("ipAddress")]
    public string IpAddress { get; set; }

    /// <summary>
    /// Displays whether or not the address is for public usage
    /// </summary>
    [JsonProperty("isPublic")]
    public bool? IsPublic { get; set; }

    /// <summary>
    /// Displays IP Protocol (4 or 6)
    /// </summary>
    [JsonProperty("ipVersion")]
    public int IpVersion { get; set; }

    /// <summary>
    /// Is the IP Address white listed with AbuseIPDB
    /// </summary>
    [JsonProperty("isWhitelisted")]
    public bool? IsWhitelisted { get; set; }

    /// <summary>
    /// Confidence score - higher the # the greater the chance the IP is an attacker
    /// </summary>
    [JsonProperty("abuseConfidenceScore")]
    public int AbuseConfidenceScore { get; set; }

    /// <summary>
    /// Code for the country of address
    /// </summary>
    [JsonProperty("countryCode")]
    public string CountryCode { get; set; }

    /// <summary>
    /// Name for the country of address
    /// </summary>
    [JsonProperty("countryName")]
    public string CountryName { get; set; }

    /// <summary>
    /// Usage type - (Reserved, Data Center, Web Hosting, Transit, etc)
    /// </summary>
    [JsonProperty("usageType")]
    public string UsageType { get; set; }

    /// <summary>
    /// Internet Provider who hosts the address
    /// </summary>
    [JsonProperty("isp")]
    public string Isp { get; set; }

    /// <summary>
    /// Internet Providers domain
    /// </summary>
    [JsonProperty("domain")]
    public string Domain { get; set; }

    [JsonProperty("hostnames")]
    public List<object> Hostnames { get; set; }

    /// <summary>
    /// Whether or not the address is from Tor
    /// </summary>
    [JsonProperty("isTor")]
    public bool IsTor { get; set; }

    /// <summary>
    /// Number of reports against the Address
    /// </summary>
    [JsonProperty("totalReports")]
    public int TotalReports { get; set; }

    /// <summary>
    /// Number of distinct reports
    /// </summary>
    [JsonProperty("numDistinctUsers")]
    public int NumDistinctUsers { get; set; }

    /// <summary>
    /// Last time the address was reported
    /// </summary>
    [JsonProperty("lastReportedAt")]
    public DateTime? LastReportedAt { get; set; }

    /// <summary>
    /// List of reports - Can be used to see what the IP was reported for
    /// </summary>
    [JsonProperty("reports")]
    public List<Report> Reports { get; set; }

    /// <summary>
    /// Errors within the transmission
    /// </summary>
    [JsonProperty("errors")]
    public List<Error> Errors { get; set; }
}

public class Ipdb
{
    [JsonProperty("data")]
    public Data Data { get; set; }
}