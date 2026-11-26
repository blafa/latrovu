namespace Latrovu.Listings.Infrastructure.Viewing.Viewings;

internal sealed record AgentViewingNotice(string Subject, string Sentence, string Highlight, string? PreviousWhen = null);
