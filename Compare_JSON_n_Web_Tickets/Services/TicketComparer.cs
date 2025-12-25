using System;
using System.Collections.Generic;
using System.Linq;
using Compare_JSON_n_Web_Tickets.Models;

namespace Compare_JSON_n_Web_Tickets.Services
{
    public sealed class ComparisonResult
    {
        public List<string> MissingTickets { get; } = new();
        public List<(string TicketName, List<string> Expected, List<string> Actual)> TagMismatches { get; } = new();
        public bool HasFailures => MissingTickets.Any() || TagMismatches.Any();

        public string FormatReport()
        {
            var lines = new List<string>();
            if (MissingTickets.Any())
            {
                lines.Add("Missing tickets:");
                lines.AddRange(MissingTickets.Select(t => $"- {t}"));
            }

            if (TagMismatches.Any())
            {
                lines.Add("Tag mismatches:");
                foreach (var m in TagMismatches)
                {
                    var expected = string.Join(", ", m.Expected);
                    var actual = string.Join(", ", m.Actual);
                    lines.Add($"- {m.TicketName}");
                    lines.Add($"  Expected: [{expected}]");
                    lines.Add($"  Actual:   [{actual}]");
                }
            }

            if (!lines.Any()) lines.Add("No differences found.");

            return string.Join(Environment.NewLine, lines);
        }
    }

    public static class TicketComparer
    {
        // Exact equality of tag sets (case-insensitive)
        public static ComparisonResult CompareExact(IEnumerable<TicketModel> expected, IDictionary<string, List<string>> actual)
        {
            var result = new ComparisonResult();

            var expectedByName = expected
                .Where(t => !string.IsNullOrEmpty(t.Name))
                .ToDictionary(t => t.Name!, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in expectedByName)
            {
                var name = kvp.Key;
                var expectedTags = (kvp.Value.Tags ?? new List<string>()).Select(s => s.Trim()).Where(s => s.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                if (!actual.TryGetValue(name, out var actualTags))
                {
                    result.MissingTickets.Add(name);
                    continue;
                }

                var actualDistinct = (actualTags ?? new List<string>()).Select(s => s.Trim())
                    .Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                var expectedSet = new HashSet<string>(expectedTags, StringComparer.OrdinalIgnoreCase);
                var actualSet = new HashSet<string>(actualDistinct, StringComparer.OrdinalIgnoreCase);

                if (!expectedSet.SetEquals(actualSet))
                {
                    result.TagMismatches.Add((name, expectedTags, actualDistinct));
                }
            }

            return result;
        }
    }
}