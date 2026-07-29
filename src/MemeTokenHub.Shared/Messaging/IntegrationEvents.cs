namespace MemeTokenHub.Shared.Messaging;

public sealed record TokenPublishedEvent(string TokenId, string OwnerId, string Name, string Symbol) : IIntegrationEvent { public string EventType => "TokenPublished"; }
public sealed record FollowChangedEvent(string FollowerId, string TargetId, string TargetType, bool IsFollowing) : IIntegrationEvent { public string EventType => "FollowChanged"; }
public sealed record KolSupportChangedEvent(string UserId, string TokenId, bool IsSupporting) : IIntegrationEvent { public string EventType => "KolSupportChanged"; }
public sealed record TokenVoteChangedEvent(string UserId, string TokenId, string Vote) : IIntegrationEvent { public string EventType => "TokenVoteChanged"; }
public sealed record PostPublishedEvent(string PostId, string AuthorId, string Access) : IIntegrationEvent { public string EventType => "PostPublished"; }
public sealed record SocialChannelVerifiedEvent(string UserId, string Provider) : IIntegrationEvent { public string EventType => "SocialChannelVerified"; }
public sealed record ClaimApprovedEvent(string ClaimId, string UserId, string TokenId, string Status) : IIntegrationEvent { public string EventType => "ClaimApproved"; }
public sealed record UserUpdatedEvent(string UserId, string Username, string AccountType) : IIntegrationEvent { public string EventType => "UserUpdated"; }
public sealed record PaymentConfirmedEvent(string PaymentId, string UserId, string SubjectId, string Purpose) : IIntegrationEvent { public string EventType => "PaymentConfirmed"; }
