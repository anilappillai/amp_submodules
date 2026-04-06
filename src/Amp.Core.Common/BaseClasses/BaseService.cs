using Microsoft.Extensions.Logging;

namespace Amp.Core.Common.BaseClasses;

/// <summary>
/// Base class for service implementations. Provides a pre-configured logger
/// and a standard exception-to-response conversion helper.
/// </summary>
public abstract class BaseService<T>(ILogger<T> logger)
{
    protected readonly ILogger<T> Logger = logger;

    /// <summary>
    /// Executes <paramref name="action"/> and wraps any unhandled exception in a
    /// failed <see cref="ApiResponse{TResult}"/> instead of letting it propagate.
    /// </summary>
    protected async Task<ApiResponse<TResult>> ExecuteSafeAsync<TResult>(
        Func<Task<ApiResponse<TResult>>> action,
        string operationName)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unhandled exception in {ServiceType}.{Operation}", typeof(T).Name, operationName);
            return ApiResponse<TResult>.ServerError($"An unexpected error occurred in {operationName}.");
        }
    }
}
