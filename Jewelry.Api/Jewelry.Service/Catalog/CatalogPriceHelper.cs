using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Catalog
{
    public static class CatalogPriceHelper
    {
        public const string WritePermissionCode = "sale:view-margin";

        public static bool CanWritePrice(JewelryContext context, IEnumerable<string> userRoles)
        {
            var roles = userRoles.ToList();
            if (!roles.Any())
                return false;

            return (from rp in context.TbtRolePermission
                    join role in context.TbmUserRole on rp.RoleId equals role.Id
                    join perm in context.TbmPermission on rp.PermissionId equals perm.Id
                    where roles.Contains(role.Name)
                        && role.IsActive
                        && perm.IsActive
                        && perm.Code == WritePermissionCode
                    select perm.Id)
                .Any();
        }

        public static (decimal? Full, decimal? Sale, string Currency) Normalize(decimal? priceFull, decimal? priceSale, string? priceCurrency)
        {
            if (priceFull.HasValue && priceFull.Value < 0)
                throw new HandleException("ราคาขายต้องไม่ติดลบ");
            if (priceSale.HasValue && priceSale.Value < 0)
                throw new HandleException("ราคาหลังลดต้องไม่ติดลบ");

            var full = priceFull.HasValue ? Math.Round(priceFull.Value, 2, MidpointRounding.AwayFromZero) : (decimal?)null;
            var sale = priceSale.HasValue ? Math.Round(priceSale.Value, 2, MidpointRounding.AwayFromZero) : (decimal?)null;

            if (full.HasValue && sale.HasValue && sale.Value > full.Value)
                throw new HandleException("ราคาหลังลดต้องไม่เกินราคาขาย");

            var currency = string.IsNullOrWhiteSpace(priceCurrency) ? "THB" : priceCurrency.Trim().ToUpperInvariant();
            if (currency != "THB" && currency != "USD")
                throw new HandleException("สกุลเงินต้องเป็น THB หรือ USD เท่านั้น");

            return (full, sale, currency);
        }
    }
}
