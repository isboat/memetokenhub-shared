using System.Net.Mail;

namespace MemeTokenHub.Shared.Extensions;

public static class StringExtensions
{
    public static bool IsValidEmail(this string? email) =>
        !string.IsNullOrWhiteSpace(email) && MailAddress.TryCreate(email, out var address) && address.Address == email;
}
