using System.ComponentModel.DataAnnotations;

namespace SubscriptionService.Configurations;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    [Required]
    public string WebhookSecret { get; init; } = string.Empty;

    [Required]
    public string PublicKey { get; init; } = string.Empty;

    [Required]
    public string SuccessUrl { get; init; } = string.Empty;

    [Required]
    public string CancelUrl { get; init; } = string.Empty;

    [Required]
    public Dictionary<string, string> Plans { get; init; } = new();
}