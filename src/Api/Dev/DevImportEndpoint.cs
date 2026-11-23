using Latrovu.Core.Submissions;
using Latrovu.Intake;

namespace Latrovu.Api.Dev;

public static class DevImportEndpoint
{
    private const string Route = "/dev/import";
    private const string FolderSettingKey = "DevImport:Folder";
    private const string SenderSettingKey = "DevImport:Sender";
    private const string DefaultFolder = "../../tests/exposes";
    private const string LoggerCategory = "Latrovu.Api.Dev.DevImport";
    private const string DevSender = "dev-import@latrovu.local";
    private const string ListingRecipient = "inserat@latrovu.com";
    private const string PdfSearchPattern = "*.pdf";
    private const string PdfMimeType = "application/pdf";

    public static void MapDevImport(this WebApplication app)
    {
        app.MapPost(Route, ImportFolderAsync);
    }

    private static async Task<IResult> ImportFolderAsync(
        IConfiguration config,
        IWebHostEnvironment environment,
        IMailDispatcher dispatcher,
        ILoggerFactory loggerFactory,
        string? pattern,
        CancellationToken ct)
    {
        ILogger logger = loggerFactory.CreateLogger(LoggerCategory);
        string folder = Path.GetFullPath(Path.Combine(environment.ContentRootPath, config[FolderSettingKey] ?? DefaultFolder));
        if (!Directory.Exists(folder))
        {
            return Results.NotFound($"Folder {folder} does not exist");
        }

        string[] files = Directory.GetFiles(folder, string.IsNullOrWhiteSpace(pattern) ? PdfSearchPattern : pattern);
        string sender = config[SenderSettingKey] ?? DevSender;
        logger.LogInformation("Importing {FileCount} PDFs from {Folder}", files.Length, folder);

        int failed = 0;
        foreach (string file in files)
        {
            failed += await TryImportFileAsync(file, sender, dispatcher, logger, ct) ? 0 : 1;
        }

        return Results.Ok(new { folder, processed = files.Length, failed });
    }

    private static async Task<bool> TryImportFileAsync(
        string file,
        string sender,
        IMailDispatcher dispatcher,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            await dispatcher.DispatchAsync(await ToMailAsync(file, sender, ct), ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Import of {FileName} failed", Path.GetFileName(file));
            return false;
        }
    }

    private static async Task<InboundMail> ToMailAsync(string file, string sender, CancellationToken ct)
    {
        string fileName = Path.GetFileName(file);
        byte[] content = await File.ReadAllBytesAsync(file, ct);
        SubmittedFile attachment = new(fileName, PdfMimeType, content);
        return new InboundMail($"dev-import:{fileName}", sender, fileName, [ListingRecipient], [attachment]);
    }
}
