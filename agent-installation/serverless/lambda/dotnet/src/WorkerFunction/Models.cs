namespace WorkerFunction;

public sealed class WorkerRequest
{
    public string? Input { get; set; }
    public int? Count { get; set; }
}

public sealed class WorkerResponse
{
    public string[] Items { get; set; } = Array.Empty<string>();
    public DateTimeOffset TimestampUtc { get; set; }
}
