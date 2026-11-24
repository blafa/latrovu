using System.Net;
using System.Text;

namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class ReviewContextPane
{
    public const string IntroPane = "intro";

    public static void Append(StringBuilder html, ReviewModel model)
    {
        html.Append("<aside class=\"panel context\" aria-label=\"Fundstelle und Bearbeitung\">")
            .Append(PaneStart(IntroPane))
            .Append("<p class=\"intro\">Wählen Sie links eine Angabe. Hier sehen Sie dann die Stelle in Ihrem Exposé ")
            .Append("und können den Wert bei Bedarf ändern.</p></div>");
        foreach (FieldDefinition field in model.Filled)
        {
            AppendFieldPane(html, model, field);
        }

        AppendImagesPane(html, model);
        html.Append("</aside>");
    }

    private static void AppendFieldPane(StringBuilder html, ReviewModel model, FieldDefinition field)
    {
        string confirmed = model.IsConfirmed(field) ? "<span class=\"state approved\">Von Ihnen bestätigt</span>" : string.Empty;
        html.Append(PaneStart(ReviewFieldRenderer.InputId(field)))
            .Append($"<p class=\"pane-kicker\">{Encode(ReviewGroups.LabelOf(field))}</p>")
            .Append($"<div class=\"pane-title\"><h2>{Encode(FieldLabels.Label(field.Path))}</h2>{confirmed}</div>")
            .Append(ReviewFieldRenderer.RenderEditor(model.Token, field, model.ValueOf(field)))
            .Append(Evidence(model, field));
        if (model.Open.Contains(field))
        {
            html.Append($"<button type=\"button\" class=\"ghost\" hx-post=\"/review/{Encode(model.Token)}/confirm\" ")
                .Append($"hx-vals='{{\"path\":\"{Encode(field.Path)}\"}}' hx-swap=\"none\">{ReviewIcons.Confirm}Wert stimmt</button>");
        }

        html.Append("</div>");
    }

    private static string Evidence(ReviewModel model, FieldDefinition field)
    {
        EvidenceLocation? location = model.EvidenceOf(field);
        if (location is not null)
        {
            return $"<figure class=\"evidence\"><figcaption>{ReviewIcons.Document}Ihr Exposé, Seite {location.Page}</figcaption>"
                + $"<blockquote>{Encode(location.Before)}<mark>{Encode(location.Match)}</mark>{Encode(location.After)}</blockquote></figure>";
        }

        if (ReviewFieldRenderer.IsWide(field) && model.SourceOf(field) == ValueSource.Found)
        {
            return Note("neutral", "Wörtlich aus dem Text Ihres Exposés übernommen.");
        }

        return model.SourceOf(field) switch
        {
            ValueSource.Open => Note("missing", ListingKeyFields.InferredPaths.Contains(field.Path)
                ? "Diesen Wert haben wir aus dem Exposé abgeleitet. Bitte prüfen Sie, ob er stimmt."
                : "Diesen Wert haben wir im Exposé nicht gefunden. Bitte prüfen Sie ihn."),
            ValueSource.Agent => Note("neutral", "Sie haben diesen Wert bestätigt."),
            ValueSource.AiWritten => Note("neutral", "Diesen Text hat die KI aus Ihrem Exposé formuliert. Passen Sie ihn gerne an."),
            _ => Note("neutral", "Diesen Wert haben wir aus dem Exposé abgeleitet. Er steht so nicht wörtlich drin.")
        };
    }

    private static string Note(string kind, string message) => $"<p class=\"evidence {kind}\">{Encode(message)}</p>";

    private static void AppendImagesPane(StringBuilder html, ReviewModel model)
    {
        if (model.Review.Images.Count == 0)
        {
            return;
        }

        html.Append(PaneStart(ReviewSummary.ImagesPane))
            .Append($"<h2>{model.Review.Images.Count} Bilder aus Ihrem Exposé</h2>");
        ReviewGallery.AppendViewer(html, model.Token, model.Review.Images);
        html.Append("</div>");
    }

    private static string PaneStart(string id) =>
        $"<div class=\"pane\" id=\"pane-{id}\"{(id == IntroPane ? string.Empty : " hidden")}>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
