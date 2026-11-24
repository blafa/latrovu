using Latrovu.BuildingBlocks.Messaging;

namespace Latrovu.Searches;

public sealed class SearchRequestHandler(
    ISearchInterpreter interpreter,
    ISearchStore searches,
    ISeekerNotifier notifier,
    ILogger<SearchRequestHandler> logger) : IMessageHandler<SearchRequestReceived>
{
    private const string FallbackLanguage = "en";

    public async Task HandleAsync(SearchRequestReceived message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (await searches.IsProcessedAsync(message.SourceMessageId, ct))
        {
            logger.LogInformation("Search request {RequestId} was already processed, skipping duplicate", message.RequestId);
            return;
        }

        IReadOnlyList<Search> existing = await searches.FindBySeekerAsync(message.SenderAddress, ct);
        SearchInterpretation? interpretation = await interpreter.InterpretAsync(message.Text, existing, ct);
        Search? changed = interpretation is null
            ? null
            : SearchRequestPlanner.Apply(existing, interpretation, message.SenderAddress, message.Channel);
        if (changed is not null)
        {
            await searches.SaveAsync(changed, ct);
        }

        logger.LogInformation(
            "Search request {RequestId} interpreted as {Action}, search {SearchId}",
            message.RequestId,
            interpretation?.Action,
            changed?.Id);
        await notifier.SendReplyAsync(BuildReply(message, interpretation, existing, changed), ct);
        await searches.MarkProcessedAsync(message.SourceMessageId, ct);
    }

    private static SeekerReply BuildReply(
        SearchRequestReceived message,
        SearchInterpretation? interpretation,
        IReadOnlyList<Search> existing,
        Search? changed)
    {
        List<Search> open = existing.Where(search => search.Id != changed?.Id).Append(changed)
            .OfType<Search>()
            .Where(search => search.IsOpen)
            .ToList();
        bool understood = interpretation is not null && interpretation.Action != SearchAction.Unclear;

        return new SeekerReply(
            message.SenderAddress,
            message.Channel,
            interpretation?.Language ?? existing.FirstOrDefault()?.Language ?? FallbackLanguage,
            interpretation?.Summary,
            interpretation?.Refinements ?? [],
            open,
            understood);
    }
}
