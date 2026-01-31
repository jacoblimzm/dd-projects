using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Datadog.Trace;
using Serilog;
using Serilog.Formatting.Json;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace WorkerFunction;

public class Functions
{
    private static readonly ILogger Log = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console(new JsonFormatter())
        .CreateLogger();

    public Task<WorkerResponse> Handler(WorkerRequest? request, ILambdaContext context)
    {
        var input = string.IsNullOrWhiteSpace(request?.Input) ? "hello" : request!.Input;
        var count = Math.Clamp(request?.Count ?? 1, 1, 5);

        using var scope = StartWorkerScope(request);
        var activeSpan = Tracer.Instance.ActiveScope?.Span;
        Log.Information(
            "Worker active span ids trace_id {TraceId} span_id {SpanId} payload_trace_id {PayloadTraceId} payload_parent_id {PayloadParentId}",
            activeSpan?.TraceId,
            activeSpan?.SpanId,
            request?.TraceContext is { } ctx && ctx.TryGetValue("trace-id", out var payloadTraceId) ? payloadTraceId : "none",
            request?.TraceContext is { } ctx2 && ctx2.TryGetValue("parent-id", out var payloadParentId) ? payloadParentId : "none");
        Log.Information("Worker received input {Input} with count {Count}.", input, count);

        var items = Enumerable.Range(1, count)
            .Select(index => $"{input}-{index}")
            .ToArray();

        var response = new WorkerResponse
        {
            Items = items,
            TimestampUtc = DateTimeOffset.UtcNow
        };

        return Task.FromResult(response);
    }

    private static IDisposable StartWorkerScope(WorkerRequest? request)
    {
        var traceContext = request?.TraceContext;
        if (traceContext is null || traceContext.Count == 0)
        {
            return Tracer.Instance.StartActive("worker");
        }

        var traceKeys = traceContext.Count > 0 ? string.Join(",", traceContext.Keys) : "none";
        if (TryParseTraceContext(traceContext, out var parentContext))
        {
            Log.Information("Extracted trace context keys: {TraceKeys} (has parent: true)", traceKeys);
            var settings = new SpanCreationSettings
            {
                Parent = parentContext
            };
            return Tracer.Instance.StartActive("worker", settings);
        }

        Log.Information("Extracted trace context keys: {TraceKeys} (has parent: false)", traceKeys);
        return Tracer.Instance.StartActive("worker");
    }

    private static bool TryParseTraceContext(IReadOnlyDictionary<string, string> traceContext, out SpanContext parentContext)
    {
        parentContext = null!;
        if (!traceContext.TryGetValue("trace-id", out var traceIdValue)
            || !traceContext.TryGetValue("parent-id", out var parentIdValue))
        {
            return false;
        }

        if (!ulong.TryParse(traceIdValue, out var traceId) || !ulong.TryParse(parentIdValue, out var parentId))
        {
            return false;
        }

        parentContext = new SpanContext(traceId, parentId);
        return true;
    }
}
