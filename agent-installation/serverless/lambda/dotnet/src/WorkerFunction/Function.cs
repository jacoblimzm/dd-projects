using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
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
}
