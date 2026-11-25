using System.Globalization;
using System.Net;
using Latrovu.BuildingBlocks.Infrastructure.Email;

namespace Latrovu.Searches.Infrastructure;

public static class SeekerAgentAnswerMail
{
    private const string FontStack = "Outfit,Helvetica,Arial,sans-serif";

    public static string Subject(SeekerAgentAnswer answer, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(texts);

        string subject = Format(texts.AgentAnswerSubject, answer.ListingTitle);
        return string.IsNullOrEmpty(answer.Reference) ? subject : $"{subject} [{answer.Reference}]";
    }

    public static string Text(SeekerAgentAnswer answer, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(texts);

        string questions = string.Join('\n', answer.Questions.Select(question => $"– {question}"));
        return $"{texts.Greeting}\n\n{Format(texts.AgentAnswerIntro, answer.ListingTitle)}\n\n{questions}\n\n{answer.Answer}\n\n"
            + $"{texts.AgentAnswerNote}\n\n{texts.Closing}\n";
    }

    public static string Html(SeekerAgentAnswer answer, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(texts);

        string questions = string.Join(string.Empty, answer.Questions.Select(question =>
            $"<p style=\"margin:0 0 4px;color:#5E6660;font-size:14px;\">– {Encode(question)}</p>"));
        return $"<!doctype html><html lang=\"{Encode(answer.Language)}\"><body style=\"margin:0;padding:0;background:#F3F4F1;\">"
            + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F3F4F1;padding:32px 16px;\"><tr><td align=\"center\">"
            + $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:520px;background:#FFFFFF;border-radius:6px;font-family:{FontStack};color:#111418;font-size:15px;line-height:1.55;\">"
            + EmailBranding.HeaderRow
            + Row($"<p style=\"margin:0;\">{Encode(texts.Greeting)}</p><p style=\"margin:12px 0 0;\">{Encode(Format(texts.AgentAnswerIntro, answer.ListingTitle))}</p>")
            + Row(questions)
            + Row($"<div style=\"background:#EEF8D2;border-radius:4px;padding:14px 16px;white-space:pre-line;\">{Encode(answer.Answer)}</div>")
            + Row($"<p style=\"margin:0;color:#5E6660;font-size:13px;\">{Encode(texts.AgentAnswerNote)}</p>")
            + Row($"<p style=\"margin:0 0 8px;white-space:pre-line;\">{Encode(texts.Closing)}</p>")
            + "</table></td></tr></table></body></html>";
    }

    private static string Format(string template, string value) => string.Format(CultureInfo.InvariantCulture, template, value);

    private static string Row(string content) => $"<tr><td style=\"padding:16px 32px 0;\">{content}</td></tr>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
