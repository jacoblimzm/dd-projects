using System.Net.Http;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
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
    private static readonly HttpClient HttpClient = new();

    public async Task<CallerResponse> Handler(CallerRequest? request, ILambdaContext context)
    {
        try
        {
            var workerFunctionUrl = System.Environment.GetEnvironmentVariable("WORKER_FUNCTION_URL");
            if (string.IsNullOrWhiteSpace(workerFunctionUrl))
            {
                Log.Warning("WORKER_FUNCTION_URL is not set.");
                return new CallerResponse
                {
                    Status = "error",
                    Message = "WORKER_FUNCTION_URL is not set."
                };
            }

            var workerRequest = new WorkerRequest
            {
                Input = string.IsNullOrWhiteSpace(request?.Message) ? "hello" : request!.Message,
                Count = request?.Count ?? 1
            };

            var payload = JsonSerializer.Serialize(workerRequest, JsonOptions);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, workerFunctionUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            Log.Information("Invoking worker URL {WorkerFunctionUrl}.", workerFunctionUrl);
            using var response = await HttpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            Log.Information("Worker response status {StatusCode} reason {Reason}.", (int)response.StatusCode, response.ReasonPhrase ?? "none");
            return new CallerResponse
            {
                Status = response.IsSuccessStatusCode ? "ok" : "error",
                Message = response.IsSuccessStatusCode ? "invocation completed" : response.ReasonPhrase ?? "worker call failed",
                WorkerStatusCode = (int)response.StatusCode,
                WorkerPayload = responseBody
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Caller function failed.");
            return new CallerResponse
            {
                Status = "error",
                Message = ex.Message
            };
        }
    }
}
