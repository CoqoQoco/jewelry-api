using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Data.Context;

public partial class JewelryContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // No-op guard: ensure ProductCostDetail stays mapped as "text" even if
        // JewelryContext is re-scaffolded from a (future) jsonb column. DB column
        // is natively "text" after migration alter_product_cost_detail_to_text.sql.
        modelBuilder.Entity<TbtStockProduct>()
            .Property(e => e.ProductCostDetail)
            .HasColumnType("text");

        modelBuilder.Entity<TbtStockCostVersion>()
            .Property(e => e.ProductCostDetail)
            .HasColumnType("text");
    }
}
