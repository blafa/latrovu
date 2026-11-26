using System.Net;
using Latrovu.BuildingBlocks.Infrastructure.Email;
using Latrovu.Listings.Infrastructure.Review.Web;

namespace Latrovu.Listings.Infrastructure.Ingestion;

internal static class IntakeReplyMail
{
    private const string FontStack = "Outfit,Helvetica,Arial,sans-serif";
    private const string IntakeAddress = "inserat@latrovu.com";
    private const int MaxOpenLabels = 5;

    public static string Subject(IntakeReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        int created = report.Files.Count(file => file.Status == IntakeFileStatus.Created);
        return created switch
        {
            > 1 => "Exposés erhalten – bitte prüfen",
            1 => "Exposé erhalten – bitte prüfen",
            _ when report.HasListings => "Exposé bereits erfasst",
            _ when report.HasNoPdf => "Kein PDF erhalten",
            _ => "Kein Exposé erkannt"
        };
    }

    public static string Text(IntakeReplyContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        IEnumerable<string> blocks = content.Report.Files.Select(file => FileText(file, content));
        return $"{Headline(content.Report)}\n\n{string.Join("\n\n", blocks)}\n\n{Footer(content.Report)}\n\nIhr latrovu-Team\n";
    }

    public static string Html(IntakeReplyContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return "<!doctype html><html lang=\"de-CH\"><body style=\"margin:0;padding:0;background:#F3F4F1;\">"
            + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F3F4F1;padding:32px 16px;\"><tr><td align=\"center\">"
            + $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:520px;background:#FFFFFF;border-radius:6px;font-family:{FontStack};color:#111418;font-size:15px;line-height:1.5;\">"
            + EmailBranding.HeaderRow
            + Row($"<p style=\"margin:0;font-size:20px;font-weight:600;\">{Encode(Headline(content.Report))}</p>")
            + string.Join(string.Empty, content.Report.Files.Select(file => Row(FileCard(file, content))))
            + Row($"<p style=\"margin:0 0 20px;color:#5E6660;font-size:13px;\">{Encode(Footer(content.Report))}</p>")
            + "</table></td></tr></table></body></html>";
    }

    private static string FileCard(IntakeFileOutcome file, IntakeReplyContent content)
    {
        if (file.Status == IntakeFileStatus.NotAListing)
        {
            return "<div style=\"border:1px solid #F2D8AE;background:#FCF6EC;border-radius:8px;padding:14px 16px;color:#633806;\">"
                + $"<p style=\"margin:0;\">📄 {Encode(file.FileName)}</p>"
                + $"<p style=\"margin:4px 0 0;font-size:13px;\">✕ {Encode(DocumentLabel(file.DocumentType))}, kein Exposé</p></div>";
        }

        IntakeListingSummary? summary = content.SummaryOf(file);
        string title = summary?.Title is { Length: > 0 } summaryTitle ? summaryTitle : file.FileName;
        return "<div style=\"border:1px solid #E1E4DD;border-radius:8px;padding:14px 16px;\">"
            + $"<p style=\"margin:0;font-weight:600;\">{Encode(title)}</p>"
            + $"<p style=\"margin:2px 0 0;color:#5E6660;font-size:12px;\">📄 {Encode(file.FileName)}</p>"
            + (file.Status == IntakeFileStatus.AlreadyKnown ? Chips([Chip("↺ bereits erfasst", "#EFF0EC", "#444441")]) : string.Empty)
            + (summary is null ? string.Empty : SummaryHtml(summary))
            + Button(file, content) + "</div>";
    }

    private static string SummaryHtml(IntakeListingSummary summary)
    {
        string chips = Chips(summary.SourceCounts.Select(count => ChipFor(count)).ToList());
        string open = summary.OpenLabels.Count == 0
            ? string.Empty
            : $"<p style=\"margin:8px 0 0;font-size:13px;color:#633806;\">⚠ {Encode(OpenLabels(summary))}</p>";
        return chips + open;
    }

    private static string Chips(IReadOnlyList<string> chips) =>
        chips.Count == 0 ? string.Empty : $"<p style=\"margin:10px 0 0;font-size:12px;line-height:2;\">{string.Join(" ", chips)}</p>";

    private static string Chip(string text, string background, string color) =>
        $"<span style=\"display:inline-block;background:{background};color:{color};border-radius:12px;padding:2px 9px;white-space:nowrap;\">{Encode(text)}</span>";

    private static string ChipFor(SourceCount count) => count.Source switch
    {
        ValueSource.Found => Chip($"✓ {count.Count} gefunden", "#E3F1D2", "#27500A"),
        ValueSource.Derived => Chip($"↻ {count.Count} abgeleitet", "#EFF0EC", "#444441"),
        ValueSource.AiWritten => Chip($"✎ {count.Count} KI", "#EEEDFE", "#3C3489"),
        ValueSource.Agent => Chip($"✓ {count.Count} bestätigt", "#E3F1D2", "#27500A"),
        _ => Chip($"⚠ {count.Count} prüfen", "#FAEEDA", "#633806")
    };

    private static string Button(IntakeFileOutcome file, IntakeReplyContent content) =>
        file.ReviewToken is null
            ? string.Empty
            : $"<a href=\"{Encode(content.ReviewUrl(file.ReviewToken))}\" style=\"display:inline-block;margin-top:12px;background:#B8E62E;color:#111418;font-weight:600;text-decoration:none;padding:10px 18px;border-radius:6px;\">"
                + (file.Status == IntakeFileStatus.Created ? "Prüfen und freigeben →" : "Zum Inserat →") + "</a>";

    private static string FileText(IntakeFileOutcome file, IntakeReplyContent content)
    {
        if (file.Status == IntakeFileStatus.NotAListing)
        {
            return $"{file.FileName}: {DocumentLabel(file.DocumentType)}, kein Exposé";
        }

        IntakeListingSummary? summary = content.SummaryOf(file);
        List<string> lines = [summary?.Title is { Length: > 0 } summaryTitle ? $"{summaryTitle} ({file.FileName})" : file.FileName];
        if (summary is not null)
        {
            lines.Add(string.Join(" · ", summary.SourceCounts.Select(count => $"{count.Count} {ShortLabel(count.Source)}")));
            if (summary.OpenLabels.Count > 0)
            {
                lines.Add($"Prüfen: {OpenLabels(summary)}");
            }
        }

        if (file.ReviewToken is not null)
        {
            lines.Add(content.ReviewUrl(file.ReviewToken));
        }

        return string.Join("\n", lines);
    }

    private static string ShortLabel(ValueSource source) => source switch
    {
        ValueSource.Found => "gefunden",
        ValueSource.Derived => "abgeleitet",
        ValueSource.AiWritten => "KI",
        ValueSource.Agent => "bestätigt",
        _ => "prüfen"
    };

    private static string OpenLabels(IntakeListingSummary summary)
    {
        int more = summary.OpenLabels.Count - MaxOpenLabels;
        return string.Join(", ", summary.OpenLabels.Take(MaxOpenLabels)) + (more > 0 ? $" und {more} weitere" : string.Empty);
    }

    private static string Headline(IntakeReport report)
    {
        int created = report.Files.Count(file => file.Status == IntakeFileStatus.Created);
        return created switch
        {
            > 1 => "Ihre Exposés sind da ✓",
            1 => "Ihr Exposé ist da ✓",
            _ when report.HasListings => "Bereits erfasst ✓",
            _ when report.HasNoPdf => "Kein PDF gefunden",
            _ => "Kein Exposé gefunden"
        };
    }

    private static string Footer(IntakeReport report) => report.HasListings
        ? "🔒 Code per E-Mail · sichtbar erst nach Freigabe"
        : $"Exposé als PDF an {IntakeAddress}";

    private static string DocumentLabel(DocumentType? type) => type switch
    {
        DocumentType.ApplicationForm => "Anmeldeformular",
        DocumentType.Contract => "Vertrag",
        _ => "anderes Dokument"
    };

    private static string Row(string content) => $"<tr><td style=\"padding:16px 32px 0;\">{content}</td></tr>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
