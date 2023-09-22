using System;
using System.Collections.Generic;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Data.Context;

public partial class JewelryContext : DbContext
{
    public JewelryContext()
    {
    }

    public JewelryContext(DbContextOptions<JewelryContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TbmAccount> TbmAccount { get; set; }

    public virtual DbSet<TbmProductionPlanStatus> TbmProductionPlanStatus { get; set; }

    public virtual DbSet<TbtProductionPlan> TbtProductionPlan { get; set; }

    public virtual DbSet<TbtProductionPlanImage> TbtProductionPlanImage { get; set; }

    public virtual DbSet<TbtProductionPlanMaterial> TbtProductionPlanMaterial { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Database=jewelry_2;User Id=jewelry2023;Password=pass2023;Trust Server Certificate=true;", x => x.UseNetTopologySuite());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TbmAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbm_account_pk");

            entity.ToTable("tbm_account");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FirstName)
                .HasMaxLength(20)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(20)
                .HasColumnName("last_name");
            entity.Property(e => e.Username)
                .HasMaxLength(10)
                .HasColumnName("username");
        });

        modelBuilder.Entity<TbmProductionPlanStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbm_production_plan_status_pk");

            entity.ToTable("tbm_production_plan_status");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.NameEn)
                .HasColumnType("character varying")
                .HasColumnName("name_en");
            entity.Property(e => e.NameTh)
                .HasColumnType("character varying")
                .HasColumnName("name_th");
        });

        modelBuilder.Entity<TbtProductionPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_production_plan_pk");

            entity.ToTable("tbt_production_plan");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.CustomerNumber)
                .HasColumnType("character varying")
                .HasColumnName("customer_number");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("true")
                .HasColumnName("is_active");
            entity.Property(e => e.Mold)
                .HasComment("เเม่พิมพ์")
                .HasColumnType("character varying")
                .HasColumnName("mold");
            entity.Property(e => e.ProductDetail)
                .HasColumnType("character varying")
                .HasColumnName("product_detail");
            entity.Property(e => e.ProductNumber)
                .HasColumnType("character varying")
                .HasColumnName("product_number");
            entity.Property(e => e.Qty)
                .HasComment("ยอดสั่ง")
                .HasColumnName("qty");
            entity.Property(e => e.QtyCast)
                .HasComment("ยอดหล่อ")
                .HasColumnName("qty_cast");
            entity.Property(e => e.QtyFinish)
                .HasComment("ยอดสำเร็จรูป")
                .HasColumnName("qty_finish");
            entity.Property(e => e.QtySemiFinish)
                .HasComment("ยอดกึ่งสำเร็จรูป")
                .HasColumnName("qty_semi_finish");
            entity.Property(e => e.QtyUnit)
                .HasComment("หน่วย")
                .HasColumnType("character varying")
                .HasColumnName("qty_unit");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.RequestDate)
                .HasComment("วันสร้างใบงาน")
                .HasColumnName("request_date");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
            entity.Property(e => e.Wo)
                .HasComment("เลขใบจ่าย-รับงาน")
                .HasColumnType("character varying")
                .HasColumnName("wo");
            entity.Property(e => e.WoNumber)
                .HasComment("ลำดับใบจ่าย-รับงาน")
                .HasColumnName("wo_number");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.TbtProductionPlan)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_fk");
        });

        modelBuilder.Entity<TbtProductionPlanImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_production_plan_image_pk");

            entity.ToTable("tbt_production_plan_image");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Number).HasColumnName("number");
            entity.Property(e => e.Path)
                .HasColumnType("character varying")
                .HasColumnName("path");
            entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.ProductionPlan).WithMany(p => p.TbtProductionPlanImage)
                .HasForeignKey(d => d.ProductionPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_image_fk");
        });

        modelBuilder.Entity<TbtProductionPlanMaterial>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_production_plan_material_pk");

            entity.ToTable("tbt_production_plan_material");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("true")
                .HasColumnName("is_active");
            entity.Property(e => e.Material)
                .HasColumnType("character varying")
                .HasColumnName("material");
            entity.Property(e => e.MaterialQty)
                .HasColumnType("character varying")
                .HasColumnName("material_qty");
            entity.Property(e => e.MaterialRemark)
                .HasColumnType("character varying")
                .HasColumnName("material_remark");
            entity.Property(e => e.MaterialShape)
                .HasColumnType("character varying")
                .HasColumnName("material_shape");
            entity.Property(e => e.MaterialSize)
                .HasColumnType("character varying")
                .HasColumnName("material_size");
            entity.Property(e => e.MaterialType)
                .HasColumnType("character varying")
                .HasColumnName("material_type");
            entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.ProductionPlan).WithMany(p => p.TbtProductionPlanMaterial)
                .HasForeignKey(d => d.ProductionPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_material_fk");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
