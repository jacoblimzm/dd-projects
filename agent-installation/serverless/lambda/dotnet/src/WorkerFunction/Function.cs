using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Datadog.Trace;
using Datadog.Trace.Propagators;
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

    private static Scope StartWorkerScope(WorkerRequest? request)
    {
        var traceContext = request?.TraceContext;
        if (traceContext is null || traceContext.Count == 0)
        {
            return Tracer.Instance.StartActive("worker");
        }

        var propagationContext = Propagator.Current.Extract(
            traceContext,
            (carrier, key) => carrier.TryGetValue(key, out var value)
                ? new[] { value }
                : Array.Empty<string>());

        var parentContext = propagationContext.SpanContext;
        var traceKeys = traceContext.Count > 0 ? string.Join(",", traceContext.Keys) : "none";
        Log.Information("Extracted trace context keys: {TraceKeys} (has parent: {HasParent})", traceKeys, parentContext is not null);
        return parentContext is null
            ? Tracer.Instance.StartActive("worker")
            : Tracer.Instance.StartActive("worker", parent: parentContext);
    }
}
