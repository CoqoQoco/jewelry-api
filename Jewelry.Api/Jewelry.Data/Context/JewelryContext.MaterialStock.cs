using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Data.Context;

// Additive partial-class extension for tbm_material_stock_policy / tbm_material_stock_min (MaterialInsight).
// JewelryContext.cs is scaffold-generated and is not edited directly. The mapping lives in
// ConfigureMaterialStock() and is invoked from the single OnModelCreatingPartial() implementation
// in JewelryContextExtensions.cs (only one implementation of that partial method is allowed).
public partial class JewelryContext
{
    public virtual DbSet<TbmMaterialStockPolicy> TbmMaterialStockPolicy { get; set; }

    public virtual DbSet<TbmMaterialStockMin> TbmMaterialStockMin { get; set; }

    private static void ConfigureMaterialStock(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TbmMaterialStockPolicy>(entity =>
        {
            entity.HasKey(e => e.MaterialKind).HasName("tbm_material_stock_policy_pk");

            entity.ToTable("tbm_material_stock_policy");

            entity.Property(e => e.MaterialKind)
                .HasColumnType("character varying")
                .HasColumnName("material_kind");
            entity.Property(e => e.WindowMonths).HasColumnName("window_months");
            entity.Property(e => e.LeadTimeDays).HasColumnName("lead_time_days");
            entity.Property(e => e.SafetyDays).HasColumnName("safety_days");
            entity.Property(e => e.FloorQty).HasColumnName("floor_qty");
            entity.Property(e => e.NearPercent).HasColumnName("near_percent");
            entity.Property(e => e.DeadStockDays).HasColumnName("dead_stock_days");
            entity.Property(e => e.ForecastMonths).HasColumnName("forecast_months");
            entity.Property(e => e.OnProcessStaleDays).HasColumnName("on_process_stale_days");
            entity.Property(e => e.PriceChangePercent).HasColumnName("price_change_percent");
            entity.Property(e => e.ConcentrationPercent).HasColumnName("concentration_percent");
            entity.Property(e => e.MinHistoryMonths).HasColumnName("min_history_months");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
        });

        modelBuilder.Entity<TbmMaterialStockMin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbm_material_stock_min_pk");

            entity.ToTable("tbm_material_stock_min");

            entity.HasIndex(e => new { e.MaterialKind, e.ItemKey }).HasDatabaseName("tbm_material_stock_min_kind_item_uq").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.MaterialKind)
                .HasColumnType("character varying")
                .HasColumnName("material_kind");
            entity.Property(e => e.ItemKey)
                .HasColumnType("character varying")
                .HasColumnName("item_key");
            entity.Property(e => e.MinQty).HasColumnName("min_qty");
            entity.Property(e => e.LeadTimeDays).HasColumnName("lead_time_days");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
        });
    }
}
