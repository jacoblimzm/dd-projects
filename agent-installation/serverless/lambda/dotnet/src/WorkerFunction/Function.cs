using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
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

    public APIGatewayHttpApiV2ProxyResponse Handler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        var workerRequest = ParseRequest(request);
        var input = string.IsNullOrWhiteSpace(workerRequest.Input) ? "hello" : workerRequest.Input;
        var count = Math.Clamp(workerRequest.Count, 1, 5);

        Log.Information("Worker received input {Input} with count {Count}.", input, count);

        var items = Enumerable.Range(1, count)
            .Select(index => $"{input}-{index}")
            .ToArray();

        var response = new WorkerResponse
        {
            Items = items,
            TimestampUtc = DateTimeOffset.UtcNow
        };

        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };
    }

    private static WorkerRequest ParseRequest(APIGatewayHttpApiV2ProxyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return new WorkerRequest();
        }

        var body = request.IsBase64Encoded
            ? Encoding.UTF8.GetString(Convert.FromBase64String(request.Body))
            : request.Body;

        var parsed = JsonSerializer.Deserialize<WorkerRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return parsed ?? new WorkerRequest();
    }
}
