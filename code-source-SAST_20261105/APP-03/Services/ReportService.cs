using System.Text.RegularExpressions;
using Tresoria.Models;

namespace Tresoria.Services;

/// <summary>
/// Service de reporting. En profil `prod` (SQL Server), la requete reelle serait :
///   SELECT ... FROM transfers WHERE company=@c [GROUP BY <groupBy>] ORDER BY <sort>
/// Le parametre `sort` est VOLONTAIREMENT concatene (GT: APP03-V008) ; `groupBy` est, lui,
/// mappe en LISTE BLANCHE (NC: APP03-N007). En memoire, on reproduit fidelement l'effet
/// observable d'une injection ORDER BY (retard WAITFOR, inversion conditionnelle via CASE).
/// </summary>
public sealed class ReportService
{
    private readonly IStore _store;
    public ReportService(IStore store) => _store = store;

    // Colonnes autorisees pour groupBy (liste blanche - pas de concatenation SQL). NC N007.
    private static readonly Dictionary<string, string> GroupByWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        ["company"] = "company",
        ["account"] = "accountId",
        ["currency"] = "currency",
        ["status"] = "status",
    };

    public object Build(string company, string? sort, string? metric, string? accountId, string? groupBy)
    {
        var rows = _store.Transfers().Where(t => t.Company == company);
        if (!string.IsNullOrEmpty(accountId))
            rows = rows.Where(t => t.AccountId == accountId);

        var list = rows.ToList();

        // -------- metric daily_total : cumul du jour (utilise par V003). --------
        if (string.Equals(metric, "daily_total", StringComparison.OrdinalIgnoreCase))
        {
            var acc = accountId ?? "A-001";
            return new { metric = "daily_total", accountId = acc, total = _store.DailyTotal(acc) };
        }

        // -------- tri (ORDER BY) --------
        list = ApplyVulnerableSort(list, sort);

        // -------- regroupement (GROUP BY) via liste blanche (NC N007) --------
        object data;
        if (!string.IsNullOrEmpty(groupBy))
        {
            // NC: APP03-N007 - groupBy mappe en liste blanche ; valeur hors liste ignoree (pas de SQL concatene).
            if (!GroupByWhitelist.TryGetValue(groupBy, out var col))
                throw new ArgumentException("groupBy invalide");

            data = col switch
            {
                "company"   => list.GroupBy(t => t.Company).Select(g => new { key = g.Key, count = g.Count(), total = g.Sum(x => x.Amount) }),
                "accountId" => list.GroupBy(t => t.AccountId).Select(g => new { key = g.Key, count = g.Count(), total = g.Sum(x => x.Amount) }),
                "currency"  => list.GroupBy(t => t.Currency).Select(g => new { key = g.Key, count = g.Count(), total = g.Sum(x => x.Amount) }),
                _           => list.GroupBy(t => t.Status).Select(g => new { key = g.Key, count = g.Count(), total = g.Sum(x => x.Amount) }),
            };
        }
        else
        {
            data = list.Select(t => new { t.Id, t.AccountId, t.Amount, t.Currency, t.Label, t.Status, date = t.CreatedAt });
        }

        return new { company, sort, groupBy, rows = data };
    }

    /// <summary>
    /
    /// On reproduit l'effet observable d'une injection SQL Server dans ORDER BY :
    ///   - charge temporelle ...WAITFOR DELAY '0:0:5'...  -> reponse retardee
    ///   - charge conditionnelle (CASE WHEN ...)          -> ordre modifie selon la condition
    /// Le code SUR equivalent (non present ici) mapperait `sort` sur une liste blanche de colonnes.
    /// </summary>
    private static List<Transfer> ApplyVulnerableSort(List<Transfer> list, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return list.OrderBy(t => t.Id).ToList();

        // Simulation fidele d'un "ORDER BY " + sort concatene.
        var orderBy = "ORDER BY " + sort;

        // 1) WAITFOR DELAY 'h:m:s' -> retard mesurable (comportement SQL Server).
        var m = Regex.Match(orderBy, @"WAITFOR\s+DELAY\s+'(\d+):(\d+):(\d+)'", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var secs = int.Parse(m.Groups[1].Value) * 3600
                     + int.Parse(m.Groups[2].Value) * 60
                     + int.Parse(m.Groups[3].Value);
            secs = Math.Min(secs, 10); // garde-fou laboratoire
            Thread.Sleep(secs * 1000);
            return list.OrderBy(t => t.Id).ToList();
        }

        // 2) (CASE WHEN <cond> THEN amount ELSE id END) -> ordre pilote par la condition injectee.
        var caseMatch = Regex.Match(orderBy, @"CASE\s+WHEN\s+(.+?)\s+THEN\s+(\w+)\s+ELSE\s+(\w+)\s+END", RegexOptions.IgnoreCase);
        if (caseMatch.Success)
        {
            bool cond = EvalTrivialCondition(caseMatch.Groups[1].Value);
            var col = cond ? caseMatch.Groups[2].Value : caseMatch.Groups[3].Value;
            return SortByColumn(list, col);
        }

        // 3) colonne (+ direction) brute injectee.
        var token = sort.Split(';', ',').First().Trim();
        var parts = token.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var column = parts.Length > 0 ? parts[0] : "id";
        bool desc = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        var sorted = SortByColumn(list, column);
        if (desc) sorted.Reverse();
        return sorted;
    }

    private static bool EvalTrivialCondition(string cond)
    {
        // Reproduit l'evaluation serveur d'une tautologie/antilogie injectee.
        cond = cond.Trim();
        var eq = Regex.Match(cond, @"^(\d+)\s*=\s*(\d+)$");
        if (eq.Success) return eq.Groups[1].Value == eq.Groups[2].Value;
        return cond.Equals("1=1");
    }

    private static List<Transfer> SortByColumn(List<Transfer> list, string column) => column.ToLowerInvariant() switch
    {
        "amount" => list.OrderBy(t => t.Amount).ToList(),
        "date" or "createdat" => list.OrderBy(t => t.CreatedAt).ToList(),
        "label" => list.OrderBy(t => t.Label).ToList(),
        _ => list.OrderBy(t => t.Id).ToList(),
    };
}
