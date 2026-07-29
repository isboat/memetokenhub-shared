namespace MemeTokenHub.Shared.Exceptions;

public class MemeTokenHubException : Exception
{
    public MemeTokenHubException(string message, string code = "UNKNOWN_ERROR", int statusCode = 500) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public int StatusCode { get; }
}

public sealed class NotFoundException(string resource, string id)
    : MemeTokenHubException($"{resource} with id {id} not found", "NOT_FOUND", 404);
public sealed class UnauthorizedException(string message = "Unauthorized")
    : MemeTokenHubException(message, "UNAUTHORIZED", 401);
public sealed class ForbiddenException(string message = "Access forbidden")
    : MemeTokenHubException(message, "FORBIDDEN", 403);
public sealed class ConflictException(string message)
    : MemeTokenHubException(message, "CONFLICT", 409);

public sealed class ValidationException : MemeTokenHubException
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", "VALIDATION_ERROR", 422) => Errors = errors;
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
