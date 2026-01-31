using System.IO;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda;
using Serilog;
using Serilog.Formatting.Json;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace CallerFunction;

public class Functions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ILogger Log = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console(new JsonFormatter())
        .CreateLogger();
    private readonly IAmazonLambda _lambdaClient;

    public Functions()
        : this(new AmazonLambdaClient())
    {
    }

    internal Functions(IAmazonLambda lambdaClient)
    {
        _lambdaClient = lambdaClient;
    }

    public async Task<CallerResponse> Handler(CallerRequest? request, ILambdaContext context)
    {
        var workerFunctionName = System.Environment.GetEnvironmentVariable("WORKER_FUNCTION_NAME");
        if (string.IsNullOrWhiteSpace(workerFunctionName))
        {
            Log.Warning("WORKER_FUNCTION_NAME is not set.");
            return new CallerResponse
            {
                Status = "error",
                Message = "WORKER_FUNCTION_NAME is not set."
            };
        }

        var workerRequest = new WorkerRequest
        {
            Input = string.IsNullOrWhiteSpace(request?.Message) ? "hello" : request!.Message,
            Count = request?.Count ?? 1
        };

        var payload = JsonSerializer.Serialize(workerRequest, JsonOptions);
        var invokeRequest = new InvokeRequest
        {
            FunctionName = workerFunctionName,
            InvocationType = InvocationType.RequestResponse,
            Payload = payload
        };

        Log.Information("Invoking worker {WorkerFunctionName}.", workerFunctionName);
        var response = await _lambdaClient.InvokeAsync(invokeRequest);
        var responseBody = await ReadResponseAsync(response.Payload);

        Log.Information("Worker response status {StatusCode} error {FunctionError}.", response.StatusCode, response.FunctionError ?? "none");
        return new CallerResponse
        {
            Status = response.FunctionError is null ? "ok" : "error",
            Message = response.FunctionError ?? "invocation completed",
            WorkerStatusCode = response.StatusCode,
            WorkerPayload = responseBody
        };
    }

    private static async Task<string> ReadResponseAsync(Stream? payloadStream)
    {
        if (payloadStream is null)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(payloadStream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
