using jewelry.Model.Stock.Product.PriceOutlierList;
using Jewelry.Service.Helper;
using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Stock.PriceSanity
{
    public static class PriceOutlierQuery
    {
        public static DataSourceResult FilterSortPage(IEnumerable<OutlierItem> items, Request request)
        {
            var q = items;
            var s = request?.Search;
            if (s != null)
            {
                if (!string.IsNullOrWhiteSpace(s.Text))
                {
                    var text = s.Text.Trim();
                    var norm = StockNumberSearch.Normalize(text);
                    q = q.Where(x =>
                        Contains(x.StockNumber, text) || Contains(x.StockNumberOrigin, text)
                        || (norm.Length > 0 && (Contains(StockNumberSearch.Normalize(x.StockNumber), norm)
                            || Contains(StockNumberSearch.Normalize(x.StockNumberOrigin), norm)))
                        || Contains(x.SkuCode, text) || Contains(x.ProductNumber, text) || Contains(x.ProductNameTh, text));
                }
                if (!string.IsNullOrWhiteSpace(s.ProductTypeName))
                    q = q.Where(x => string.Equals(x.ProductTypeName?.Trim(), s.ProductTypeName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(s.ProductionType))
                    q = q.Where(x => string.Equals(x.ProductionType?.Trim(), s.ProductionType.Trim(), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(s.ReasonCode))
                    q = q.Where(x => x.Reasons.Any(r => string.Equals(r.Code, s.ReasonCode.Trim(), StringComparison.OrdinalIgnoreCase)));
            }

            var review = s?.ReviewStatus?.Trim().ToUpperInvariant();
            if (review == "ACKED")
                q = q.Where(x => x.IsAcked);
            else if (review != "ALL")
                q = q.Where(x => !x.IsAcked);

            var list = q.ToList();
            var sort = request?.Sort?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Field));
            var desc = sort == null || !string.Equals(sort.Dir, "asc", StringComparison.OrdinalIgnoreCase);
            IOrderedEnumerable<OutlierItem> ordered;
            switch (sort?.Field?.Trim().ToLowerInvariant())
            {
                case "price":
                    ordered = desc ? list.OrderByDescending(x => x.Price) : list.OrderBy(x => x.Price);
                    break;
                case "stocknumber":
                    ordered = desc ? list.OrderByDescending(x => x.StockNumber, StringComparer.Ordinal) : list.OrderBy(x => x.StockNumber, StringComparer.Ordinal);
                    break;
                case "stocknumberorigin":
                    ordered = StrSort(list, x => x.StockNumberOrigin, desc);
                    break;
                case "productnumber":
                    ordered = StrSort(list, x => x.ProductNumber, desc);
                    break;
                case "producttypename":
                    ordered = StrSort(list, x => x.ProductTypeName, desc);
                    break;
                case "wo":
                    ordered = StrSort(list, x => x.Wo, desc);
                    ordered = desc
                        ? ordered.ThenByDescending(x => x.WoNumber.HasValue ? 0 : 1).ThenByDescending(x => x.WoNumber ?? 0)
                        : ordered.ThenBy(x => x.WoNumber.HasValue ? 0 : 1).ThenBy(x => x.WoNumber ?? 0);
                    break;
                default:
                    ordered = (sort == null || sort.Field.Trim().ToLowerInvariant() == "score") && !desc
                        ? list.OrderBy(x => x.Score)
                        : list.OrderByDescending(x => x.Score);
                    break;
            }
            var sorted = ordered.ThenBy(x => x.StockNumber, StringComparer.Ordinal).ToList();

            var skip = Math.Max(request?.Skip ?? 0, 0);
            var take = request?.Take ?? 0;
            IEnumerable<OutlierItem> page = sorted.Skip(skip);
            if (take > 0) page = page.Take(take);

            return new DataSourceResult { Data = page.ToList(), Total = sorted.Count };
        }

        private static IOrderedEnumerable<OutlierItem> StrSort(List<OutlierItem> list, Func<OutlierItem, string?> key, bool desc)
        {
            var o = list.OrderBy(x => string.IsNullOrEmpty(key(x)) ? 1 : 0);
            return desc
                ? o.ThenByDescending(x => key(x) ?? string.Empty, StringComparer.Ordinal)
                : o.ThenBy(x => key(x) ?? string.Empty, StringComparer.Ordinal);
        }

        private static bool Contains(string? source, string value)
            => source != null && source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}
