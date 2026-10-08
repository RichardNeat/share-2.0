using System.Globalization;
using System.Text.Json;
using Share.Web.Models;

namespace Share.Web.Ingest;

public record ParsedOffer(int RowNumber, Offer? Offer, string? Reference, string? SkipReason);

// Parses an expressions of interest file (GOV.UK Forms shaped submissions). Answers are matched on question text.
public static class OfferParser
{
    public static IReadOnlyList<ParsedOffer> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var results = new List<ParsedOffer>();
        var row = 0;
        foreach (var s in doc.RootElement.EnumerateArray())
        {
            row++;
            var reference = Str(s, "submission_reference");
            if (string.IsNullOrWhiteSpace(reference))
            {
                results.Add(new(row, null, null, "No submission reference"));
                continue;
            }
            var answers = s.TryGetProperty("answers", out var a) && a.ValueKind == JsonValueKind.Array ? a.EnumerateArray().ToList() : [];
            JsonElement? Q(string text) => answers.Cast<JsonElement?>().FirstOrDefault(x => Str(x!.Value, "question_text") == text);
            string? Ans(string text) => Q(text) is { } q && Str(q, "answer_text") is { Length: > 0 } v ? v.Trim() : null;
            int? Num(string text) => int.TryParse(Ans(text), out var n) ? n : null;
            bool? YesNo(string text) => Ans(text)?.ToLowerInvariant() switch { "yes" => true, "no" => false, _ => null };

            var address = Q("Address of the accommodation you are offering");
            string? Part(string name) => address is { } ad && Str(ad, name) is { Length: > 0 } v ? v.Trim() : null;
            var town = Part("town_or_city");

            var offer = new Offer
            {
                SubmissionReference = reference.Trim(),
                SubmittedAt = DateTime.TryParse(Str(s, "submitted_at"), CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at) ? at : DateTime.MinValue,
                HostName = Ans("Full name"),
                Email = Ans("Email address"),
                Telephone = Ans("Telephone number"),
                Address1 = Part("address1"),
                Address2 = Part("address2"),
                TownOrCity = town,
                Postcode = Part("postcode"),
                Council = town?.ToUpperInvariant(),
                AvailableFrom = DateOnly.TryParseExact(Ans("When is the accommodation available from?"), "dd/MM/yyyy", out var from) ? from : null,
                Adults = Num("How many adults could the accommodation sleep?"),
                Children = Num("How many children could the accommodation sleep?"),
                Bedrooms = Num("How many bedrooms are you offering?"),
                StepFree = YesNo("Does the accommodation have step-free access?"),
                Pets = YesNo("Would you accept a household with pets?"),
            };
            results.Add(new(row, offer, offer.SubmissionReference, null));
        }
        return results;
    }

    static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
