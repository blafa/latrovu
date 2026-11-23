using System.ComponentModel.DataAnnotations;

namespace Latrovu.Api.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public const string DefaultRegion = "us-east-1";

    [Required]
    public string Endpoint { get; init; } = string.Empty;

    [Required]
    public string Bucket { get; init; } = string.Empty;

    [Required]
    public string AccessKey { get; init; } = string.Empty;

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    public string Region { get; init; } = DefaultRegion;
}
