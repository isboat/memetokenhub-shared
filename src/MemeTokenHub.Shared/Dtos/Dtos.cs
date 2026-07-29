namespace MemeTokenHub.Shared.Dtos;

public sealed record UserDto(string UserId, string Username, string? Email, string? WalletAddress, AccountType AccountType, DateTimeOffset CreatedAt);
public sealed record CreateUserDto(string Username, string? Email, string? WalletAddress);
public sealed record UpdateUserDto(string? Username, string? Bio, string? AvatarUrl);
public sealed record TokenDto(string TokenId, string Name, string Symbol, string Chain, string ContractAddress, TokenStatus Status, decimal? Price, DateTimeOffset CreatedAt);
public sealed record CreateTokenDto(string Name, string Symbol, string? Description, string Chain, string ContractAddress);
public sealed record ClaimDto(string ClaimId, string UserId, string TokenId, ClaimStatus Status, string? Description, DateTimeOffset SubmittedAt);
public sealed record CreateClaimDto(string TokenId, string? Description, IReadOnlyList<string> Attachments);
public sealed record ReviewClaimDto(ClaimStatus Status, string? Notes);
public sealed record FollowDto(string FollowerId, string TargetId, FollowTargetType TargetType, DateTimeOffset CreatedAt);
public sealed record ActivityDto(string ActivityId, string UserId, ActivityType Type, string Description, DateTimeOffset CreatedAt);
public sealed record ReputationDto(string UserId, int Score, IReadOnlyList<string> Badges);
public sealed record PaymentDto(string PaymentId, string UserId, string? TokenId, decimal Amount, PaymentStatus Status, PaymentPurpose Purpose, DateTimeOffset CreatedAt);
public sealed record CreateCheckoutDto(string SubjectId, decimal Amount, PaymentPurpose Purpose);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Limit, int Offset, long Total)
{
    public static PagedResponse<T> Create(IEnumerable<T> items, int limit, int offset, long total)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(total);
        return new(items.ToArray(), limit, offset, total);
    }
}

public enum AccountType { Fan, KOL, Developer, Moderator }
public enum FollowTargetType { User, Token, Network }
public enum VoteValue { Hot, NotHot }
public enum ContentAccess { Public, Subscribers }
public enum PaymentPurpose { TokenPurchase, Subscription, PremiumContent }
public enum ClaimStatus { Pending, Approved, Rejected, Appealed }
public enum TokenStatus { Draft, Unclaimed, Claimed, Featured, Published }
public enum PaymentStatus { Pending, Completed, Failed, Refunded }
public enum ActivityType { ClaimSubmitted, TokenPublished, UserFollowed, PostPublished }
