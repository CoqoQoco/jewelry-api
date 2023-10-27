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

    public virtual DbSet<TbmCustomerType> TbmCustomerType { get; set; }

    public virtual DbSet<TbmGem> TbmGem { get; set; }

    public virtual DbSet<TbmGemShape> TbmGemShape { get; set; }

    public virtual DbSet<TbmGold> TbmGold { get; set; }

    public virtual DbSet<TbmGoldSize> TbmGoldSize { get; set; }

    public virtual DbSet<TbmProductType> TbmProductType { get; set; }

    public virtual DbSet<TbmProductionPlanStatus> TbmProductionPlanStatus { get; set; }

    public virtual DbSet<TbtProductionPlan> TbtProductionPlan { get; set; }

    public virtual DbSet<TbtProductionPlanImage> TbtProductionPlanImage { get; set; }

    public virtual DbSet<TbtProductionPlanMaterial> TbtProductionPlanMaterial { get; set; }

    public virtual DbSet<TbtProductionPlanStatusDetail> TbtProductionPlanStatusDetail { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    { }
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
//    => optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Database=jewelry_2;User Id=jewelry2023;Password=pass2023;Trust Server Certificate=true;", x => x.UseNetTopologySuite());

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

        modelBuilder.Entity<TbmCustomerType>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("tbm_customer_type_pk");

            entity.ToTable("tbm_customer_type");

            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.NameEn)
                .HasColumnType("character varying")
                .HasColumnName("name_en");
            entity.Property(e => e.NameTh)
                .HasColumnType("character varying")
                .HasColumnName("name_th");
        });

        modelBuilder.Entity<TbmGem>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("tbm_gem_pk");

            entity.ToTable("tbm_gem");

            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.NameEn)
                .HasColumnType("character varying")
                .HasColumnName("name_en");
            entity.Property(e => e.NameTh)
                .HasColumnType("character varying")
                .HasColumnName("name_th");
        });

        modelBuilder.Entity<TbmGemShape>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("tbm_gem_shape_pk");

            entity.ToTable("tbm_gem_shape");

            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.NameEn)
                .HasColumnType("character varying")
                .HasColumnName("name_en");
            entity.Property(e => e.NameTh)
                .HasColumnType("character varying")
                .HasColumnName("name_th");
        });

        modelBuilder.Entity<TbmGold>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("tbm_gold_pk");

            entity.ToTable("tbm_gold");

            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.NameEn)
                .HasColumnType("character varying")
                .HasColumnName("name_en");
            entity.Property(e => e.NameTh)
                .HasColumnType("character varying")
                .HasColumnName("name_th");
        });

        modelBuilder.Entity<TbmGoldSize>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("tbm_gold_size_pk");

            entity.ToTable("tbm_gold_size");

            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.NameEn)
                .HasColumnType("character varying")
                .HasColumnName("name_en");
            entity.Property(e => e.NameTh)
                .HasColumnType("character varying")
                .HasColumnName("name_th");
        });

        modelBuilder.Entity<TbmProductType>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("tbm_product_type_pk");

            entity.ToTable("tbm_product_type");

            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.NameEn)
                .HasColumnType("character varying")
                .HasColumnName("name_en");
            entity.Property(e => e.NameTh)
                .HasColumnType("character varying")
                .HasColumnName("name_th");
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
            entity.Property(e => e.CustomerType)
                .HasColumnType("character varying")
                .HasColumnName("customer_type");
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
            entity.Property(e => e.ProductName)
                .HasColumnType("character varying")
                .HasColumnName("product_name");
            entity.Property(e => e.ProductNumber)
                .HasColumnType("character varying")
                .HasColumnName("product_number");
            entity.Property(e => e.ProductQty).HasColumnName("product_qty");
            entity.Property(e => e.ProductQtyUnit)
                .HasColumnType("character varying")
                .HasColumnName("product_qty_unit");
            entity.Property(e => e.ProductType)
                .HasColumnType("character varying")
                .HasColumnName("product_type");
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

            entity.HasOne(d => d.ProductTypeNavigation).WithMany(p => p.TbtProductionPlan)
                .HasForeignKey(d => d.ProductType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_type_fk");

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
            entity.Property(e => e.DiamondQty).HasColumnName("diamond_qty");
            entity.Property(e => e.DiamondQuality)
                .HasColumnType("character varying")
                .HasColumnName("diamond_quality");
            entity.Property(e => e.DiamondSize)
                .HasColumnType("character varying")
                .HasColumnName("diamond_size");
            entity.Property(e => e.DiamondUnit)
                .HasColumnType("character varying")
                .HasColumnName("diamond_unit");
            entity.Property(e => e.DiamondWeight)
                .HasColumnType("character varying")
                .HasColumnName("diamond_weight");
            entity.Property(e => e.DiamondWeightUnit)
                .HasColumnType("character varying")
                .HasColumnName("diamond_weight_unit");
            entity.Property(e => e.Gem)
                .HasColumnType("character varying")
                .HasColumnName("gem");
            entity.Property(e => e.GemQty).HasColumnName("gem_qty");
            entity.Property(e => e.GemShape)
                .HasColumnType("character varying")
                .HasColumnName("gem_shape");
            entity.Property(e => e.GemSize)
                .HasColumnType("character varying")
                .HasColumnName("gem_size");
            entity.Property(e => e.GemUnit)
                .HasColumnType("character varying")
                .HasColumnName("gem_unit");
            entity.Property(e => e.GemWeight)
                .HasColumnType("character varying")
                .HasColumnName("gem_weight");
            entity.Property(e => e.GemWeightUnit)
                .HasColumnType("character varying")
                .HasColumnName("gem_weight_unit");
            entity.Property(e => e.Gold)
                .HasColumnType("character varying")
                .HasColumnName("gold");
            entity.Property(e => e.GoldQty).HasColumnName("gold_qty");
            entity.Property(e => e.GoldSize)
                .HasColumnType("character varying")
                .HasColumnName("gold_size");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("true")
                .HasColumnName("is_active");
            entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.GemNavigation).WithMany(p => p.TbtProductionPlanMaterial)
                .HasForeignKey(d => d.Gem)
                .HasConstraintName("tbt_gem_fk");

            entity.HasOne(d => d.GemShapeNavigation).WithMany(p => p.TbtProductionPlanMaterial)
                .HasForeignKey(d => d.GemShape)
                .HasConstraintName("tbt_gem_shape_fk");

            entity.HasOne(d => d.GoldNavigation).WithMany(p => p.TbtProductionPlanMaterial)
                .HasForeignKey(d => d.Gold)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_gold_fk");

            entity.HasOne(d => d.GoldSizeNavigation).WithMany(p => p.TbtProductionPlanMaterial)
                .HasForeignKey(d => d.GoldSize)
                .HasConstraintName("tbt_gold_size_fk");

            entity.HasOne(d => d.ProductionPlan).WithMany(p => p.TbtProductionPlanMaterial)
                .HasForeignKey(d => d.ProductionPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_material_fk");
        });

        modelBuilder.Entity<TbtProductionPlanStatusDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_production_plan_status_detail_pk");

            entity.ToTable("tbt_production_plan_status_detail");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignBy)
                .HasColumnType("character varying")
                .HasColumnName("assign_by");
            entity.Property(e => e.AssignDate).HasColumnName("assign_date");
            entity.Property(e => e.AssignDetail)
                .HasColumnType("character varying")
                .HasColumnName("assign_detail");
            entity.Property(e => e.AssignTo)
                .HasColumnType("character varying")
                .HasColumnName("assign_to");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
            entity.Property(e => e.ReceiveBy)
                .HasColumnType("character varying")
                .HasColumnName("receive_by");
            entity.Property(e => e.ReceiveDate).HasColumnName("receive_date");
            entity.Property(e => e.ReceiveDetail)
                .HasColumnType("character varying")
                .HasColumnName("receive_detail");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.ProductionPlan).WithMany(p => p.TbtProductionPlanStatusDetail)
                .HasForeignKey(d => d.ProductionPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_fk");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.TbtProductionPlanStatusDetail)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_status_fk");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
