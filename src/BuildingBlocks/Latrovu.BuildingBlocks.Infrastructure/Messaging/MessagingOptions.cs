using System.ComponentModel.DataAnnotations;

namespace Latrovu.BuildingBlocks.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";
    public const string DefaultErrorQueue = "latrovu-error";
    public const int DefaultMaxDeliveryAttempts = 5;

    [Required]
    public string RabbitMqConnectionString { get; init; } = string.Empty;

    public string ErrorQueue { get; init; } = DefaultErrorQueue;

    [Range(1, 20)]
    public int MaxDeliveryAttempts { get; init; } = DefaultMaxDeliveryAttempts;
}
