using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Data.Context;

public partial class JewelryContext
{
    public virtual DbSet<TbtProductionPlanGemReturn> TbtProductionPlanGemReturn { get; set; }

    private static void ConfigurePlanGemReturn(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TbtProductionPlanGemReturn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_production_plan_gem_return_pk");

            entity.ToTable("tbt_production_plan_gem_return");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
            entity.Property(e => e.HeaderId).HasColumnName("header_id");
            entity.Property(e => e.ItemNo).HasColumnType("character varying").HasColumnName("item_no");
            entity.Property(e => e.OutboundRunning).HasColumnType("character varying").HasColumnName("outbound_running");
            entity.Property(e => e.GemCode).HasColumnType("character varying").HasColumnName("gem_code");
            entity.Property(e => e.ReturnQty).HasColumnName("return_qty");
            entity.Property(e => e.ReturnWeight).HasColumnName("return_weight");
            entity.Property(e => e.Mode).HasColumnType("character varying").HasColumnName("mode");
            entity.Property(e => e.InboundTransectionId).HasColumnName("inbound_transection_id");
            entity.Property(e => e.InboundRunning).HasColumnType("character varying").HasColumnName("inbound_running");
            entity.Property(e => e.Reason).HasColumnType("character varying").HasColumnName("reason");
            entity.Property(e => e.Remark).HasColumnType("character varying").HasColumnName("remark");
            entity.Property(e => e.CreateBy).HasColumnType("character varying").HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
        });
    }
}
