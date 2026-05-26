using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Data.Context;

public partial class JewelryContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // No-op guard: ensure ProductCostDetail stays mapped as "text" even if
        // JewelryContext is re-scaffolded from a (future) jsonb column.
        modelBuilder.Entity<TbtStockPiece>()
            .Property(e => e.ProductCostDetail)
            .HasColumnType("text");

        modelBuilder.Entity<TbtStockPieceCostVersion>()
            .Property(e => e.ProductCostDetail)
            .HasColumnType("text");
    }
}
