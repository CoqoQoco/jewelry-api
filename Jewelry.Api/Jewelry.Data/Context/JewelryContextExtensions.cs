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

        modelBuilder.Entity<TbtPosCheckout>(entity =>
        {
            entity.HasKey(e => e.IdempotencyKey).HasName("tbt_pos_checkout_pk");

            entity.ToTable("tbt_pos_checkout");

            entity.Property(e => e.IdempotencyKey)
                .HasColumnType("character varying")
                .HasColumnName("idempotency_key");
            entity.Property(e => e.SoNumber)
                .HasColumnType("character varying")
                .HasColumnName("so_number");
            entity.Property(e => e.InvoiceNumber)
                .HasColumnType("character varying")
                .HasColumnName("invoice_number");
            entity.Property(e => e.GrandTotal).HasColumnName("grand_total");
            entity.Property(e => e.PaidAmount).HasColumnName("paid_amount");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
        });
    }
}
