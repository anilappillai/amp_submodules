using Amp.Core.Common.BaseClasses;
using Amp.Core.Common.Helpers;
using Amp.Core.Services.Abstractions.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Amp.Core.Services.Http;

/// <summary>
/// <see cref="IHttpClientService"/> implementation backed by a named
/// <see cref="System.Net.Http.HttpClient"/> from <see cref="System.Net.Http.IHttpClientFactory"/>.
///
/// Features:
///   • Consistent JSON serialisation via <see cref="JsonHelper.DefaultOptions"/>
///   • Structured logging of request/response metadata
///   • Maps HTTP status codes to typed <see cref="ApiResponse{T}"/>
///   • Resilience (retry + circuit-breaker) is configured at registration time
///     via Microsoft.Extensions.Http.Resilience (see <see cref="CoreHttpClientExtensions"/>).
/// </summary>
public sealed class HttpClientService(
    HttpClient httpClient,
    ILogger<HttpClientService> logger) : IHttpClientService
{
    public async Task<ApiResponse<T>> GetAsync<T>(string url, CancellationToken ct = default)
        => await GetAsync<T>(url, new Dictionary<string, string>(), ct);

    public async Task<ApiResponse<T>> GetAsync<T>(string url, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var (k, v) in headers) request.Headers.Add(k, v);
        return await SendAsync<T>(request, ct);
    }

    public async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest payload, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = ToJsonContent(payload)
        };
        return await SendAsync<TResponse>(request, ct);
    }

    public async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest payload, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = ToJsonContent(payload)
        };
        return await SendAsync<TResponse>(request, ct);
    }

    public async Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string url, TRequest payload, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = ToJsonContent(payload)
        };
        return await SendAsync<TResponse>(request, ct);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(string url, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        var response = await SendAsync<object>(request, ct);
        return response.Success
            ? ApiResponse<bool>.Ok(true)
            : ApiResponse<bool>.Fail(response.Message ?? "Delete failed.", response.StatusCode);
    }

    private async Task<ApiResponse<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            logger.LogDebug("HTTP {Method} {Url}", request.Method, request.RequestUri);
            var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("HTTP {Method} {Url} responded {StatusCode}", request.Method, request.RequestUri, (int)response.StatusCode);
                return ApiResponse<T>.Fail($"Upstream responded with {(int)response.StatusCode}.", response.StatusCode);
            }

            var data = JsonSerializer.Deserialize<T>(body, JsonHelper.DefaultOptions);
            return ApiResponse<T>.Ok(data!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed: {Method} {Url}", request.Method, request.RequestUri);
            return ApiResponse<T>.ServerError($"HTTP request failed: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "HTTP request timed out: {Method} {Url}", request.Method, request.RequestUri);
            return ApiResponse<T>.Fail("Request timed out.", HttpStatusCode.GatewayTimeout);
        }
    }

    private static StringContent ToJsonContent<T>(T payload) =>
        new(JsonSerializer.Serialize(payload, JsonHelper.DefaultOptions), Encoding.UTF8, "application/json");
}
