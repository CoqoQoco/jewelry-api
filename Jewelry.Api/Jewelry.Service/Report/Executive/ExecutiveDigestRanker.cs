using System;
using System.Collections.Generic;
using System.Linq;
using Digest = jewelry.Model.Report.Executive.Digest;
using Finding = jewelry.Model.Production.Insight.Wip.Finding;

namespace Jewelry.Service.Report.Executive
{
    public class DigestTabFinding
    {
        public DigestTabFinding(Finding finding, string tab)
        {
            Finding = finding;
            Tab = tab;
        }

        public Finding Finding { get; }
        public string Tab { get; }
    }

    public class DigestDomainInput
    {
        public string Key { get; set; } = null!;
        public bool Available { get; set; }
        public List<DigestTabFinding> Problems { get; set; } = new List<DigestTabFinding>();
        public int ForecastCount { get; set; }
    }

    // pure — รวมผล insight หลาย domain เป็นกระดานสถานะ + top findings
    public static class ExecutiveDigestRanker
    {
        public const int MaxTop = 5;
        public const int MaxPerDomain = 2;

        public static readonly string[] DomainOrder = { "production", "sales", "material", "stock" };

        public static int SeverityRank(string? severity) => severity switch
        {
            "critical" => 0,
            "warning" => 1,
            "info" => 2,
            _ => 3
        };

        // worst severity ของ problems — critical > warning, info/ว่าง = ok
        public static string WorstStatus(IEnumerable<DigestTabFinding> problems)
        {
            var worst = problems.Select(p => SeverityRank(p.Finding.Severity)).DefaultIfEmpty(3).Min();
            return worst == 0 ? "critical" : worst == 1 ? "warning" : "ok";
        }

        public static List<Digest.DomainStatus> BuildDomains(IEnumerable<DigestDomainInput> inputs)
        {
            var byKey = inputs.ToDictionary(i => i.Key, StringComparer.Ordinal);
            return DomainOrder.Select(key =>
            {
                if (!byKey.TryGetValue(key, out var input) || !input.Available)
                {
                    return new Digest.DomainStatus { Key = key, Status = "unknown", Available = false };
                }

                return new Digest.DomainStatus
                {
                    Key = key,
                    Status = WorstStatus(input.Problems),
                    ProblemCount = input.Problems.Count,
                    ForecastCount = input.ForecastCount,
                    Available = true
                };
            }).ToList();
        }

        // critical ก่อน warning ก่อน info; เท่ากันเรียงตามลำดับ domain แล้วลำดับเดิมใน domain (stable); จำกัด 2 ต่อ domain; รวม 5
        public static List<Digest.TopItem> BuildTop(IEnumerable<DigestDomainInput> inputs)
        {
            var byKey = inputs.ToDictionary(i => i.Key, StringComparer.Ordinal);
            var candidates = new List<(int Rank, int DomainIndex, int Seq, string Domain, DigestTabFinding Item)>();

            for (var d = 0; d < DomainOrder.Length; d++)
            {
                if (!byKey.TryGetValue(DomainOrder[d], out var input) || !input.Available) continue;

                var seq = 0;
                foreach (var p in input.Problems)
                {
                    candidates.Add((SeverityRank(p.Finding.Severity), d, seq++, DomainOrder[d], p));
                }
            }

            var perDomain = new Dictionary<string, int>(StringComparer.Ordinal);
            var top = new List<Digest.TopItem>();

            foreach (var c in candidates.OrderBy(c => c.Rank).ThenBy(c => c.DomainIndex).ThenBy(c => c.Seq))
            {
                perDomain.TryGetValue(c.Domain, out var used);
                if (used >= MaxPerDomain) continue;

                perDomain[c.Domain] = used + 1;
                top.Add(new Digest.TopItem
                {
                    Domain = c.Domain,
                    Severity = c.Item.Finding.Severity,
                    Tab = c.Item.Tab,
                    Code = c.Item.Finding.Code,
                    Params = c.Item.Finding.Params,
                    ReportRef = c.Item.Finding.ReportRef
                });

                if (top.Count >= MaxTop) break;
            }

            return top;
        }

        public static Digest.Response Build(DateTime asOf, string metal, IReadOnlyCollection<DigestDomainInput> inputs)
        {
            return new Digest.Response
            {
                AsOf = asOf,
                Metal = metal,
                Domains = BuildDomains(inputs),
                Top = BuildTop(inputs)
            };
        }
    }
}
