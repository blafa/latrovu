namespace Latrovu.Searches;

public sealed record Search(
    Guid Id,
    string SeekerAddress,
    string Channel,
    string Name,
    string Language,
    string CriteriaJson,
    string Status)
{
    public const string ActiveStatus = "active";
    public const string PausedStatus = "paused";
    public const string StoppedStatus = "stopped";

    public bool IsOpen => Status != StoppedStatus;
}
