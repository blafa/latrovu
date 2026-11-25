namespace Latrovu.Hosts.Intake.Dev;

public sealed record DevMailRequest(string Body, string? Subject, string? To, string? Sender);
