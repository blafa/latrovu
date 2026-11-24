using System.Net;
using System.Text;
using Latrovu.BuildingBlocks.Infrastructure.Email;

namespace Latrovu.Searches.Infrastructure;

public static class SeekerReplyHtml
{
    private const string FontStack = "Outfit,Helvetica,Arial,sans-serif";

    public static string Render(SeekerReply reply, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(reply);
        ArgumentNullException.ThrowIfNull(texts);

        StringBuilder html = new($"<!doctype html><html lang=\"{Encode(reply.Language)}\"><body style=\"margin:0;padding:0;background:#F3F4F1;\">");
        html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F3F4F1;padding:32px 16px;\"><tr><td align=\"center\">")
            .Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:520px;background:#FFFFFF;border-radius:6px;font-family:{FontStack};color:#111418;font-size:15px;line-height:1.55;\">")
            .Append(EmailBranding.HeaderRow)
            .Append(Row($"<p style=\"margin:0;\">{Encode(texts.Greeting)}</p>"));
        AppendUnderstanding(html, reply, texts);
        AppendRefinements(html, reply, texts);
        AppendSearches(html, reply, texts);
        html.Append(Row($"<p style=\"margin:0;color:#5E6660;font-size:14px;\">{Encode(texts.HowToChange)}</p>"))
            .Append(Row($"<p style=\"margin:0 0 8px;white-space:pre-line;\">{Encode(texts.Closing)}</p>"))
            .Append("</table></td></tr></table></body></html>");
        return html.ToString();
    }

    private static void AppendUnderstanding(StringBuilder html, SeekerReply reply, SeekerTexts texts)
    {
        if (!reply.Understood)
        {
            html.Append(Row(Box(Encode(texts.NotUnderstood), "#FCF1DE", "#7A4A07")));
            return;
        }

        html.Append(Row(
            $"<p style=\"margin:0 0 6px;font-weight:600;\">{Encode(texts.Understood)}</p>"
            + Box(Encode(reply.Summary ?? string.Empty), "#F6F7F4", "#111418")));
    }

    private static void AppendRefinements(StringBuilder html, SeekerReply reply, SeekerTexts texts)
    {
        if (reply.Refinements.Count == 0)
        {
            return;
        }

        string items = string.Concat(reply.Refinements.Select(item => $"<li style=\"margin:2px 0;\">{Encode(item)}</li>"));
        html.Append(Row(Box(
            $"<p style=\"margin:0 0 4px;font-weight:600;\">{Encode(texts.Refinements)}</p><ul style=\"margin:0;padding-left:18px;\">{items}</ul>",
            "#FCF1DE",
            "#7A4A07")));
    }

    private static void AppendSearches(StringBuilder html, SeekerReply reply, SeekerTexts texts)
    {
        if (reply.OpenSearches.Count == 0)
        {
            return;
        }

        string chips = string.Concat(reply.OpenSearches.Select(search => Chip(search, texts)));
        html.Append(Row($"<p style=\"margin:0 0 6px;font-weight:600;\">{Encode(texts.OpenSearches)}</p>{chips}"));
    }

    private static string Chip(Search search, SeekerTexts texts)
    {
        bool paused = search.Status == Search.PausedStatus;
        string label = paused ? $"{search.Name} ({texts.Paused})" : search.Name;
        string colors = paused ? "background:#F3F4F1;color:#5E6660;" : "background:#E3F1D2;color:#2E5410;";
        return $"<span style=\"display:inline-block;margin:0 6px 6px 0;padding:4px 10px;border-radius:4px;{colors}\">{Encode(label)}</span>";
    }

    private static string Row(string content) => $"<tr><td style=\"padding:16px 32px 0;\">{content}</td></tr>";

    private static string Box(string content, string background, string color) =>
        $"<div style=\"background:{background};color:{color};border-radius:4px;padding:12px 14px;\">{content}</div>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
