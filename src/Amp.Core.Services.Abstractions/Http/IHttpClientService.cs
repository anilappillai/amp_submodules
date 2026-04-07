using Amp.Core.Common.BaseClasses;

namespace Amp.Core.Services.Abstractions.Http;

/// <summary>
/// Strongly-typed HTTP client abstraction that wraps the <see cref="System.Net.Http.IHttpClientFactory"/>
/// pattern and adds resilience, JSON serialisation, and structured logging.
/// </summary>
public interface IHttpClientService
{
    /// <summary>Sends a GET request and deserialises the JSON response body.</summary>
    Task<ApiResponse<T>> GetAsync<T>(string url, CancellationToken ct = default);

    /// <summary>Sends a GET request with custom headers.</summary>
    Task<ApiResponse<T>> GetAsync<T>(string url, IDictionary<string, string> headers, CancellationToken ct = default);

    /// <summary>Sends a POST request with a JSON body.</summary>
    Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest payload, CancellationToken ct = default);

    /// <summary>Sends a PUT request with a JSON body.</summary>
    Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest payload, CancellationToken ct = default);

    /// <summary>Sends a PATCH request with a JSON body.</summary>
    Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string url, TRequest payload, CancellationToken ct = default);

    /// <summary>Sends a DELETE request.</summary>
    Task<ApiResponse<bool>> DeleteAsync(string url, CancellationToken ct = default);
}

