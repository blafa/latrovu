using Latrovu.BuildingBlocks.Messaging;
using Latrovu.BuildingBlocks.Storage;
using Latrovu.Core.Submissions;

namespace Latrovu.Listings.Ingestion;

public sealed class SubmissionReceivedHandler(
    IListingIntake intake,
    IObjectStorage storage,
    IProcessedSubmissionStore processed,
    ILogger<SubmissionReceivedHandler> logger) : IMessageHandler<SubmissionReceived>
{
    public async Task HandleAsync(SubmissionReceived message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        string sourceId = string.IsNullOrWhiteSpace(message.SourceMessageId)
            ? message.SubmissionId.ToString()
            : message.SourceMessageId;
        if (await processed.IsProcessedAsync(sourceId, ct))
        {
            logger.LogInformation("Submission {SubmissionId} was already processed, skipping duplicate", message.SubmissionId);
            await DeleteFilesAsync(message, ct);
            return;
        }

        await intake.HandleAsync(message.ToSubmission(), await LoadFilesAsync(message, ct), ct);
        await processed.MarkProcessedAsync(sourceId, ct);
        await DeleteFilesAsync(message, ct);
    }

    private async Task<List<SubmittedFile>> LoadFilesAsync(SubmissionReceived message, CancellationToken ct)
    {
        List<SubmittedFile> files = [];
        foreach (SubmittedFileRef file in message.Files)
        {
            StoredObject? stored = await storage.GetAsync(file.StorageKey, ct);
            if (stored is null)
            {
                logger.LogWarning("File {StorageKey} of submission {SubmissionId} is missing", file.StorageKey, message.SubmissionId);
                continue;
            }

            files.Add(new SubmittedFile(file.FileName, file.MimeType, stored.Content));
        }

        return files;
    }

    private async Task DeleteFilesAsync(SubmissionReceived message, CancellationToken ct)
    {
        foreach (SubmittedFileRef file in message.Files)
        {
            await storage.DeleteAsync(file.StorageKey, ct);
        }

        logger.LogDebug("Deleted {FileCount} files of submission {SubmissionId}", message.Files.Count, message.SubmissionId);
    }
}
