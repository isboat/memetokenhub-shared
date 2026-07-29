namespace MemeTokenHub.Shared.Constants;

public static class UserRoles
{
    public const string Anonymous = "Anonymous";
    public const string Authenticated = "Authenticated";
    public const string Creator = "Creator";
    public const string Collector = "Collector";
    public const string Moderator = "Moderator";
}

public static class CapabilityNames
{
    public const string ClaimType = "capability";
    public const string ProjectsWrite = "projects:write";
    public const string SupportWrite = "support:write";
    public const string ModerateClaims = "moderation:claims";
    public const string MonetizeContent = "content:monetize";
}

public static class MessagingNames
{
    public const string IntegrationTopic = "memetokenhub-integration";
}
