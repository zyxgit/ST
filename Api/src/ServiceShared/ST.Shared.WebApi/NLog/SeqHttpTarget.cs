using System.Net.Http.Json;
using System.Text.Json.Serialization;
using NLog;
using NLog.Common;
using NLog.Targets;

namespace ST.Shared.WebApi.Infra;

/// <summary>
/// 轻量 NLog Target，通过 HTTP 将日志推送到 Seq 日志中心。
/// 不依赖 NLog.Targets.Seq 包（因与 NLog 6.x 不兼容）。
/// </summary>
[Target("SeqHttp")]
public sealed class SeqHttpTarget : TargetWithLayout
{
    private readonly HttpClient _httpClient = new();
    private readonly string _baseUrl;

    public string? ApiKey { get; set; }

    public SeqHttpTarget(string serverUrl)
    {
        _baseUrl = serverUrl.TrimEnd('/');
    }

    protected override void Write(LogEventInfo logEvent)
    {
        var entry = new SeqEvent
        {
            Timestamp = logEvent.TimeStamp,
            Level = logEvent.Level.Name,
            MessageTemplate = RenderLogEvent(Layout, logEvent),
            Properties = new Dictionary<string, object?>
            {
                ["Logger"] = logEvent.LoggerName
            }
        };

        if (logEvent.Exception != null)
        {
            entry.Exception = logEvent.Exception.ToString();
        }

        var payload = new SeqPayload { Events = [entry] };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/events/raw")
            {
                Content = JsonContent.Create(payload)
            };
            if (!string.IsNullOrWhiteSpace(ApiKey))
            {
                request.Headers.Add("X-Seq-ApiKey", ApiKey);
            }
            _httpClient.SendAsync(request).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            InternalLogger.Error(ex, "SeqHttpTarget failed to send log event to {Url}", _baseUrl);
        }
    }
}

internal sealed class SeqPayload
{
    [JsonPropertyName("Events")]
    public List<SeqEvent> Events { get; set; } = [];
}

internal sealed class SeqEvent
{
    [JsonPropertyName("@t")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("@l")]
    public string Level { get; set; } = string.Empty;

    [JsonPropertyName("@mt")]
    public string MessageTemplate { get; set; } = string.Empty;

    [JsonPropertyName("@x")]
    public string? Exception { get; set; }

    [JsonPropertyName("@Properties")]
    public Dictionary<string, object?> Properties { get; set; } = [];
}
