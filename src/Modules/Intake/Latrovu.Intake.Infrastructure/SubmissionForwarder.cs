using System.Globalization;
using Latrovu.BuildingBlocks.Messaging;
using Latrovu.BuildingBlocks.Storage;
using Latrovu.Core.Submissions;
using Npgsql;

namespace Latrovu.Intake.Infrastructure;

public sealed class SubmissionForwarder(
    IObjectStorage storage,
    IOutbox outbox,
    NpgsqlDataSource dataSource,
    ILogger<SubmissionForwarder> logger) : ISubmissionForwarder
{
    private const string StorageKeyFormat = "submissions/{0}/{1:D2}{2}";
    private const string PdfExtension = ".pdf";
    private const string PngExtension = ".png";
    private const string JpegExtension = ".jpg";

    public async Task ForwardAsync(InboundMail mail, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mail);

        Guid submissionId = Guid.NewGuid();
        List<SubmittedFileRef> files = await UploadAsync(submissionId, mail.Attachments, ct);
        SubmissionReceived message = new(submissionId, mail.MessageId, mail.Sender, mail.Subject, files);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        await outbox.AddAsync([message], transaction, ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation("Submission {SubmissionId} with {FileCount} files forwarded", submissionId, files.Count);
    }

    private async Task<List<SubmittedFileRef>> UploadAsync(Guid submissionId, IReadOnlyList<SubmittedFile> attachments, CancellationToken ct)
    {
        List<SubmittedFileRef> files = [];
        foreach (SubmittedFile file in attachments.Where(file => file.IsPdf || file.IsImage))
        {
            string key = string.Format(CultureInfo.InvariantCulture, StorageKeyFormat, submissionId, files.Count + 1, ExtensionOf(file));
            await storage.PutAsync(key, file.Content, file.MimeType, ct);
            files.Add(new SubmittedFileRef(file.FileName, file.MimeType, key));
        }

        return files;
    }

    private static string ExtensionOf(SubmittedFile file) =>
        file.IsPdf ? PdfExtension : file.IsPng ? PngExtension : JpegExtension;
}
