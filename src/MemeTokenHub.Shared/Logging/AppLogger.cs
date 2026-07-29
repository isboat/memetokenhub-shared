using Microsoft.Extensions.Logging;

namespace MemeTokenHub.Shared.Logging;

public interface IAppLogger<T>
{
    void LogInformation(string message, params object?[] args);
    void LogWarning(string message, params object?[] args);
    void LogError(Exception exception, string message, params object?[] args);
}

public sealed class AppLogger<T>(ILogger<T> logger) : IAppLogger<T>
{
    public void LogInformation(string message, params object?[] args) => logger.LogInformation(message, args);
    public void LogWarning(string message, params object?[] args) => logger.LogWarning(message, args);
    public void LogError(Exception exception, string message, params object?[] args) => logger.LogError(exception, message, args);
}
