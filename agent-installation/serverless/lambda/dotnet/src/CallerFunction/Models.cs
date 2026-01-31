namespace CallerFunction;

public sealed class CallerRequest
{
    public string? Message { get; set; }
    public int? Count { get; set; }
}

public sealed class CallerResponse
{
    public string Status { get; set; } = "ok";
    public string Message { get; set; } = string.Empty;
    public int? WorkerStatusCode { get; set; }
    public string WorkerPayload { get; set; } = string.Empty;
}

public sealed class WorkerRequest
{
    public string Input { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
    public Dictionary<string, string>? TraceContext { get; set; }
}
