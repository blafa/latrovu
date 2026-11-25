namespace Latrovu.Listings.Viewing.Chat;

public sealed record KnowledgeEntry(long Id, string Question, string Answer, DateTimeOffset CreatedAt);
