using System;
using System.Collections.Generic;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Data.Context;

public partial class JewelryContext : DbContext
{
    public JewelryContext(DbContextOptions<JewelryContext> options)
        : base(options)
    {
    }

    public virtual DbSet<StockFromConvert> StockFromConvert { get; set; }

    public virtual DbSet<TbmAccount> TbmAccount { get; set; }

    public virtual DbSet<TbmCustomer> TbmCustomer { get; set; }

    public virtual DbSet<TbmCustomerType> TbmCustomerType { get; set; }

    public virtual DbSet<TbmDiamondGrade> TbmDiamondGrade { get; set; }

    public virtual DbSet<TbmEmbedMethod> TbmEmbedMethod { get; set; }

    public virtual DbSet<TbmGem> TbmGem { get; set; }

    public virtual DbSet<TbmGemShape> TbmGemShape { get; set; }

    public virtual DbSet<TbmGold> TbmGold { get; set; }

    public virtual DbSet<TbmGoldSize> TbmGoldSize { get; set; }

    public virtual DbSet<TbmProductMoldPlanStatus> TbmProductMoldPlanStatus { get; set; }

    public virtual DbSet<TbmProductType> TbmProductType { get; set; }

    public virtual DbSet<TbmProductionPlanStatus> TbmProductionPlanStatus { get; set; }

    public virtual DbSet<TbmUserRole> TbmUserRole { get; set; }

    public virtual DbSet<TbmWorker> TbmWorker { get; set; }

    public virtual DbSet<TbmZill> TbmZill { get; set; }

    public virtual DbSet<TbtMoldCheckOutList> TbtMoldCheckOutList { get; set; }

    public virtual DbSet<TbtProductMold> TbtProductMold { get; set; }

    public virtual DbSet<TbtProductMoldPlan> TbtProductMoldPlan { get; set; }

    public virtual DbSet<TbtProductMoldPlanCasting> TbtProductMoldPlanCasting { get; set; }

    public virtual DbSet<TbtProductMoldPlanCastingSilver> TbtProductMoldPlanCastingSilver { get; set; }

    public virtual DbSet<TbtProductMoldPlanCutting> TbtProductMoldPlanCutting { get; set; }

    public virtual DbSet<TbtProductMoldPlanDesign> TbtProductMoldPlanDesign { get; set; }

    public virtual DbSet<TbtProductMoldPlanGem> TbtProductMoldPlanGem { get; set; }

    public virtual DbSet<TbtProductMoldPlanResin> TbtProductMoldPlanResin { get; set; }

    public virtual DbSet<TbtProductMoldPlanStore> TbtProductMoldPlanStore { get; set; }

    public virtual DbSet<TbtProductionPlan> TbtProductionPlan { get; set; }

    public virtual DbSet<TbtProductionPlanBom> TbtProductionPlanBom { get; set; }

    public virtual DbSet<TbtProductionPlanCostGold> TbtProductionPlanCostGold { get; set; }

    public virtual DbSet<TbtProductionPlanCostGoldItem> TbtProductionPlanCostGoldItem { get; set; }

    public virtual DbSet<TbtProductionPlanImage> TbtProductionPlanImage { get; set; }

    public virtual DbSet<TbtProductionPlanMaterial> TbtProductionPlanMaterial { get; set; }

    public virtual DbSet<TbtProductionPlanPrice> TbtProductionPlanPrice { get; set; }

    public virtual DbSet<TbtProductionPlanStatusDetail> TbtProductionPlanStatusDetail { get; set; }

    public virtual DbSet<TbtProductionPlanStatusDetailGem> TbtProductionPlanStatusDetailGem { get; set; }

    public virtual DbSet<TbtProductionPlanStatusHeader> TbtProductionPlanStatusHeader { get; set; }

    public virtual DbSet<TbtProductionPlanTransferStatus> TbtProductionPlanTransferStatus { get; set; }

    public virtual DbSet<TbtReceiptStockCreate> TbtReceiptStockCreate { get; set; }

    public virtual DbSet<TbtRunningNumber> TbtRunningNumber { get; set; }

    public virtual DbSet<TbtSaleQuotation> TbtSaleQuotation { get; set; }

    public virtual DbSet<TbtStockGem> TbtStockGem { get; set; }

    public virtual DbSet<TbtStockGemTransection> TbtStockGemTransection { get; set; }

    public virtual DbSet<TbtStockGemTransectionPrice> TbtStockGemTransectionPrice { get; set; }

    public virtual DbSet<TbtStockProduct> TbtStockProduct { get; set; }

    public virtual DbSet<TbtStockProductImage> TbtStockProductImage { get; set; }

    public virtual DbSet<TbtStockProductMaterial> TbtStockProductMaterial { get; set; }

    public virtual DbSet<TbtStockProductReceiptItem> TbtStockProductReceiptItem { get; set; }

    public virtual DbSet<TbtStockProductReceiptPlan> TbtStockProductReceiptPlan { get; set; }

    public virtual DbSet<TbtUser> TbtUser { get; set; }

    public virtual DbSet<TbtUserRole> TbtUserRole { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_catalog", "adminpack");

        modelBuilder.Entity<StockFromConvert>(entity =>
        {
            entity.HasKey(e => e.Recgen).HasName("stock_from_convert_pk");

            entity.ToTable("stock_from_convert");

            entity.Property(e => e.Recgen)
                .ValueGeneratedNever()
                .HasColumnName("recgen");
            entity.Property(e => e.Codeproduct)
                .HasMaxLength(255)
                .HasColumnName("codeproduct");
            entity.Property(e => e.Dateadd)
                .HasMaxLength(255)
                .HasColumnName("dateadd");
            entity.Property(e => e.Dateproduct)
                .HasMaxLength(255)
                .HasColumnName("dateproduct");
            entity.Property(e => e.Descthai)
                .HasMaxLength(255)
                .HasColumnName("descthai");
            entity.Property(e => e.Issueno)
                .HasMaxLength(255)
                .HasColumnName("issueno");
            entity.Property(e => e.Jobno)
                .HasMaxLength(255)
                .HasColumnName("jobno");
            entity.Property(e => e.NoCode)
                .HasMaxLength(255)
                .HasColumnName("no_code");
            entity.Property(e => e.Noproduct)
                .HasMaxLength(255)
                .HasColumnName("noproduct");
            entity.Property(e => e.OriginD)
                .HasMaxLength(255)
                .HasColumnName("origin_d");
            entity.Property(e => e.OriginD1)
                .HasMaxLength(255)
                .HasColumnName("origin_d1");
            entity.Property(e => e.Partno)
                .HasMaxLength(255)
                .HasColumnName("partno");
            entity.Property(e => e.Partnoold)
                .HasMaxLength(255)
                .HasColumnName("partnoold");
            entity.Property(e => e.Pricecost)
                .HasMaxLength(255)
                .HasColumnName("pricecost");
            entity.Property(e => e.Priced).HasColumnName("priced");
            entity.Property(e => e.Priced1).HasColumnName("priced1");
            entity.Property(e => e.Pricee).HasColumnName("pricee");
            entity.Property(e => e.Priceg).HasColumnName("priceg");
            entity.Property(e => e.Pricem).HasColumnName("pricem");
            entity.Property(e => e.Pricer).HasColumnName("pricer");
            entity.Property(e => e.Prices).HasColumnName("prices");
            entity.Property(e => e.Pricesale).HasColumnName("pricesale");
            entity.Property(e => e.Priceus).HasColumnName("priceus");
            entity.Property(e => e.Productname)
                .HasMaxLength(255)
                .HasColumnName("productname");
            entity.Property(e => e.Qtyd).HasColumnName("qtyd");
            entity.Property(e => e.Qtyd1).HasColumnName("qtyd1");
            entity.Property(e => e.Qtye).HasColumnName("qtye");
            entity.Property(e => e.Qtyg).HasColumnName("qtyg");
            entity.Property(e => e.Qtym).HasColumnName("qtym");
            entity.Property(e => e.Qtyr).HasColumnName("qtyr");
            entity.Property(e => e.Qtys).HasColumnName("qtys");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Remark)
                .HasMaxLength(255)
                .HasColumnName("remark");
            entity.Property(e => e.Ringsize)
                .HasMaxLength(255)
                .HasColumnName("ringsize");
            entity.Property(e => e.Sizee)
                .HasMaxLength(255)
                .HasColumnName("sizee");
            entity.Property(e => e.Sizem)
                .HasMaxLength(255)
                .HasColumnName("sizem");
            entity.Property(e => e.Sizer)
                .HasMaxLength(255)
                .HasColumnName("sizer");
            entity.Property(e => e.Sizes)
                .HasMaxLength(255)
                .HasColumnName("sizes");
            entity.Property(e => e.StatusP)
                .HasMaxLength(255)
                .HasColumnName("status_p");
            entity.Property(e => e.Stockid)
                .HasMaxLength(255)
                .HasColumnName("stockid");
            entity.Property(e => e.TypeS).HasMaxLength(255);
            entity.Property(e => e.Typed)
                .HasMaxLength(255)
                .HasColumnName("typed");
            entity.Property(e => e.Typed1)
                .HasMaxLength(255)
                .HasColumnName("typed1");
            entity.Property(e => e.Typedesc)
                .HasMaxLength(255)
                .HasColumnName("typedesc");
            entity.Property(e => e.Typee)
                .HasMaxLength(255)
                .HasColumnName("typee");
            entity.Property(e => e.Typeg)
                .HasMaxLength(255)
                .HasColumnName("typeg");
            entity.Property(e => e.Typejob)
                .HasMaxLength(255)
                .HasColumnName("typejob");
            entity.Property(e => e.Typem)
                .HasMaxLength(255)
                .HasColumnName("typem");
            entity.Property(e => e.Typep)
                .HasMaxLength(255)
                .HasColumnName("typep");
            entity.Property(e => e.Typer)
                .HasMaxLength(255)
                .HasColumnName("typer");
            entity.Property(e => e.Typesil)
                .HasMaxLength(255)
                .HasColumnName("typesil");
            entity.Property(e => e.Unit)
                .HasMaxLength(255)
                .HasColumnName("unit");
            entity.Property(e => e.Unit1)
                .HasMaxLength(255)
                .HasColumnName("unit1");
            entity.Property(e => e.Unit2)
                .HasMaxLength(255)
                .HasColumnName("unit2");
            entity.Property(e => e.Unit3)
                .HasMaxLength(255)
                .HasColumnName("unit3");
            entity.Property(e => e.Unit4)
                .HasMaxLength(255)
                .HasColumnName("unit4");
            entity.Property(e => e.Unit5)
                .HasMaxLength(255)
                .HasColumnName("unit5");
            entity.Property(e => e.Unit6)
                .HasMaxLength(255)
                .HasColumnName("unit6");
            entity.Property(e => e.Unitd1)
                .HasMaxLength(255)
                .HasColumnName("unitd1");
            entity.Property(e => e.Unitsil)
                .HasMaxLength(255)
                .HasColumnName("unitsil");
            entity.Property(e => e.Username)
                .HasMaxLength(255)
                .HasColumnName("username");
            entity.Property(e => e.Wd).HasColumnName("wd");
            entity.Property(e => e.Wd1).HasColumnName("wd1");
            entity.Property(e => e.We).HasColumnName("we");
            entity.Property(e => e.Wg).HasColumnName("wg");
            entity.Property(e => e.Whno)
                .HasMaxLength(255)
                .HasColumnName("whno");
            entity.Property(e => e.Wm).HasColumnName("wm");
            entity.Property(e => e.Wr).HasColumnName("wr");
            entity.Property(e => e.Ws).HasColumnName("ws");
            entity.Property(e => e.Wsil)
                .HasMaxLength(255)
                .HasColumnName("wsil");
        });

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
            entity.Property(e => e.TempPass)
                .HasColumnType("character varying")
                .HasColumnName("temp_pass");
            entity.Property(e => e.Username)
                .HasMaxLength(10)
                .HasColumnName("username");
        });

        modelBuilder.Entity<TbmCustomer>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("tbm_customer_pk");

            entity.ToTable("tbm_customer");

            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.Address)
                .HasColumnType("character varying")
                .HasColumnName("address");
            entity.Property(e => e.ContactName)
                .HasColumnType("character varying")
                .HasColumnName("contact_name");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.NameEn)
                .HasColumnType("character varying")
                .HasColumnName("name_en");
            entity.Property(e => e.NameTh)
                .HasColumnType("character varying")
                .HasColumnName("name_th");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.Telephone1)
                .HasColumnType("character varying")
                .HasColumnName("telephone_1");
            entity.Property(e => e.Telephone2)
                .HasColumnType("character varying")
                .HasColumnName("telephone_2");
            entity.Property(e => e.TypeCode)
                .HasColumnType("character varying")
                .HasColumnName("type_code");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.TypeCodeNavigation).WithMany(p => p.TbmCustomer)
                .HasForeignKey(d => d.TypeCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbm_customer_fk");
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

        modelBuilder.Entity<TbmDiamondGrade>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("tbm_diamond_grade_pk");

            entity.ToTable("tbm_diamond_grade");

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
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
        });

        modelBuilder.Entity<TbmEmbedMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbm_embed_method_pk");

            entity.ToTable("tbm_embed_method");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.MaxLossPercent).HasColumnName("max_loss_percent");
            entity.Property(e => e.NameEn)
                .HasColumnType("character varying")
                .HasColumnName("name_en");
            entity.Property(e => e.NameTh)
                .HasColumnType("character varying")
                .HasColumnName("name_th");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
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

        modelBuilder.Entity<TbmProductMoldPlanStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbm_product_mold_plan_status_pk");

            entity.ToTable("tbm_product_mold_plan_status");

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
            entity.Property(e => e.ProductCode)
                .HasDefaultValueSql("'DK'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("product_code");
            entity.Property(e => e.SilverCode)
                .HasDefaultValueSql("'DK'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("silver_code");
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
            entity.Property(e => e.Reference)
                .HasColumnType("character varying")
                .HasColumnName("reference");
        });

        modelBuilder.Entity<TbmUserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbm_user_role_pk");

            entity.ToTable("tbm_user_role");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
        });

        modelBuilder.Entity<TbmWorker>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("tbm_worker_pk");

            entity.ToTable("tbm_worker");

            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.NameEn)
                .HasColumnType("character varying")
                .HasColumnName("name_en");
            entity.Property(e => e.NameTh)
                .HasColumnType("character varying")
                .HasColumnName("name_th");
            entity.Property(e => e.TypeId).HasColumnName("type_id");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
        });

        modelBuilder.Entity<TbmZill>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("tbm_zill_pk");

            entity.ToTable("tbm_zill");

            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.GoldCode)
                .HasColumnType("character varying")
                .HasColumnName("gold_code");
            entity.Property(e => e.GoldSizeCode)
                .HasColumnType("character varying")
                .HasColumnName("gold_size_code");
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
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.GoldCodeNavigation).WithMany(p => p.TbmZill)
                .HasForeignKey(d => d.GoldCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbm_zill_gold_fk");

            entity.HasOne(d => d.GoldSizeCodeNavigation).WithMany(p => p.TbmZill)
                .HasForeignKey(d => d.GoldSizeCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbm_zill_gold_size_fk");
        });

        modelBuilder.Entity<TbtMoldCheckOutList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_mold_picking_list_pk");

            entity.ToTable("tbt_mold_check_out_list");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('tbt_mold_picking_list_id_seq'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.CheckOutDate).HasColumnName("check_out_date");
            entity.Property(e => e.CheckOutDescription)
                .HasColumnType("character varying")
                .HasColumnName("check_out_description");
            entity.Property(e => e.CheckOutName)
                .HasColumnType("character varying")
                .HasColumnName("check_out_name");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.MoldCode)
                .HasColumnType("character varying")
                .HasColumnName("mold_code");
            entity.Property(e => e.ReturnDateFinish).HasColumnName("return_date_finish");
            entity.Property(e => e.ReturnDateSet).HasColumnName("return_date_set");
            entity.Property(e => e.ReturnDescription)
                .HasColumnType("character varying")
                .HasColumnName("return_description");
            entity.Property(e => e.ReturnName)
                .HasColumnType("character varying")
                .HasColumnName("return_name");
            entity.Property(e => e.Running)
                .HasColumnType("character varying")
                .HasColumnName("running");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.MoldCodeNavigation).WithMany(p => p.TbtMoldCheckOutList)
                .HasForeignKey(d => d.MoldCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_mold_picking_list_fk");
        });

        modelBuilder.Entity<TbtProductMold>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("tbt_product_mold_pk");

            entity.ToTable("tbt_product_mold");

            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.Category)
                .HasColumnType("character varying")
                .HasColumnName("category");
            entity.Property(e => e.CategoryCode)
                .HasColumnType("character varying")
                .HasColumnName("category_code");
            entity.Property(e => e.CodeDraft)
                .HasColumnType("character varying")
                .HasColumnName("code_draft");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.Image)
                .HasColumnType("character varying")
                .HasColumnName("image");
            entity.Property(e => e.ImageDraft1)
                .HasColumnType("character varying")
                .HasColumnName("image_draft_1");
            entity.Property(e => e.ImageDraft2)
                .HasColumnType("character varying")
                .HasColumnName("image_draft_2");
            entity.Property(e => e.ImageDraft3)
                .HasColumnType("character varying")
                .HasColumnName("image_draft_3");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.MoldBy)
                .HasColumnType("character varying")
                .HasColumnName("mold_by");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.ReModelMold)
                .HasColumnType("character varying")
                .HasColumnName("re_model_mold");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("1")
                .HasColumnName("status");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.Plan).WithMany(p => p.TbtProductMold)
                .HasForeignKey(d => d.PlanId)
                .HasConstraintName("tbt_product_mold_fk");
        });

        modelBuilder.Entity<TbtProductMoldPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_product_mold_plan_pk");

            entity.ToTable("tbt_product_mold_plan");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("true")
                .HasColumnName("is_active");
            entity.Property(e => e.NextStatus).HasColumnName("next_status");
            entity.Property(e => e.RemarkUpdate)
                .HasColumnType("character varying")
                .HasColumnName("remark_update");
            entity.Property(e => e.Running)
                .HasColumnType("character varying")
                .HasColumnName("running");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.NextStatusNavigation).WithMany(p => p.TbtProductMoldPlanNextStatusNavigation)
                .HasForeignKey(d => d.NextStatus)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_mold_plan_next_fk");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.TbtProductMoldPlanStatusNavigation)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_mold_plan_fk");
        });

        modelBuilder.Entity<TbtProductMoldPlanCasting>(entity =>
        {
            entity.HasKey(e => new { e.PlanId, e.CodePlan }).HasName("newtabletbt_product_mold_plan_casting_pk");

            entity.ToTable("tbt_product_mold_plan_casting");

            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.CodePlan)
                .HasColumnType("character varying")
                .HasColumnName("code_plan");
            entity.Property(e => e.CastBy)
                .HasColumnType("character varying")
                .HasColumnName("cast_by");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.ImageUrl)
                .HasColumnType("character varying")
                .HasColumnName("image_url");
            entity.Property(e => e.QtyReceive).HasColumnName("qty_receive");
            entity.Property(e => e.QtySend).HasColumnName("qty_send");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.Plan).WithMany(p => p.TbtProductMoldPlanCasting)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("newtabletbt_product_mold_plan_casting_fk");
        });

        modelBuilder.Entity<TbtProductMoldPlanCastingSilver>(entity =>
        {
            entity.HasKey(e => new { e.PlanId, e.CodePlan }).HasName("tbt_product_mold_plan_casting_silver_pk");

            entity.ToTable("tbt_product_mold_plan_casting_silver");

            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.CodePlan)
                .HasColumnType("character varying")
                .HasColumnName("code_plan");
            entity.Property(e => e.CastBy)
                .HasColumnType("character varying")
                .HasColumnName("cast_by");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.ImageUrl)
                .HasColumnType("character varying")
                .HasColumnName("image_url");
            entity.Property(e => e.QtyReceive).HasColumnName("qty_receive");
            entity.Property(e => e.QtySend).HasColumnName("qty_send");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.Plan).WithMany(p => p.TbtProductMoldPlanCastingSilver)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_mold_plan_casting_silver_fk");
        });

        modelBuilder.Entity<TbtProductMoldPlanCutting>(entity =>
        {
            entity.HasKey(e => new { e.PlanId, e.CodePlan, e.Code }).HasName("tbt_product_mold_plan_cutting_pk");

            entity.ToTable("tbt_product_mold_plan_cutting");

            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.CodePlan)
                .HasColumnType("character varying")
                .HasColumnName("code_plan");
            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.CuttingBy)
                .HasColumnType("character varying")
                .HasColumnName("cutting_by");
            entity.Property(e => e.ImageUrl)
                .HasColumnType("character varying")
                .HasColumnName("image_url");
            entity.Property(e => e.QtyReceive).HasColumnName("qty_receive");
            entity.Property(e => e.QtySend).HasColumnName("qty_send");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.Plan).WithMany(p => p.TbtProductMoldPlanCutting)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_mold_plan_cutting_fk");
        });

        modelBuilder.Entity<TbtProductMoldPlanDesign>(entity =>
        {
            entity.HasKey(e => new { e.PlanId, e.CodePlan }).HasName("tbt_product_mold_plan_design_pk");

            entity.ToTable("tbt_product_mold_plan_design");

            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.CodePlan)
                .HasColumnType("character varying")
                .HasColumnName("code_plan");
            entity.Property(e => e.CategoryCode)
                .HasColumnType("character varying")
                .HasColumnName("category_code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.DesignBy)
                .HasColumnType("character varying")
                .HasColumnName("design_by");
            entity.Property(e => e.ImageUrl)
                .HasColumnType("character varying")
                .HasColumnName("image_url");
            entity.Property(e => e.QtyReceive).HasColumnName("qty_receive");
            entity.Property(e => e.QtySend).HasColumnName("qty_send");
            entity.Property(e => e.RemarUpdate)
                .HasColumnType("character varying")
                .HasColumnName("remar_update");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.CategoryCodeNavigation).WithMany(p => p.TbtProductMoldPlanDesign)
                .HasForeignKey(d => d.CategoryCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_mold_plan_design_type_fk");

            entity.HasOne(d => d.Plan).WithMany(p => p.TbtProductMoldPlanDesign)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_mold_plan_design_fk");
        });

        modelBuilder.Entity<TbtProductMoldPlanGem>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.PlanId }).HasName("tbt_product_mold_plan_gem_pk");

            entity.ToTable("tbt_product_mold_plan_gem");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.GemCode)
                .HasColumnType("character varying")
                .HasColumnName("gem_code");
            entity.Property(e => e.GemShapeCode)
                .HasColumnType("character varying")
                .HasColumnName("gem_shape_code");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.Size)
                .HasColumnType("character varying")
                .HasColumnName("size");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.GemCodeNavigation).WithMany(p => p.TbtProductMoldPlanGem)
                .HasForeignKey(d => d.GemCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_mold_plan_gem_code_fk");

            entity.HasOne(d => d.GemShapeCodeNavigation).WithMany(p => p.TbtProductMoldPlanGem)
                .HasForeignKey(d => d.GemShapeCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_mold_plan_gem_shape_code_fk");

            entity.HasOne(d => d.Plan).WithMany(p => p.TbtProductMoldPlanGem)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_mold_plan_gem_fk");
        });

        modelBuilder.Entity<TbtProductMoldPlanResin>(entity =>
        {
            entity.HasKey(e => new { e.PlanId, e.CodePlan }).HasName("tbt_product_mold_plan_resin_pk");

            entity.ToTable("tbt_product_mold_plan_resin");

            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.CodePlan)
                .HasColumnType("character varying")
                .HasColumnName("code_plan");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.ImageUrl)
                .HasColumnType("character varying")
                .HasColumnName("image_url");
            entity.Property(e => e.QtyReceive).HasColumnName("qty_receive");
            entity.Property(e => e.QtySend).HasColumnName("qty_send");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.ResinBy)
                .HasColumnType("character varying")
                .HasColumnName("resin_by");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.Plan).WithMany(p => p.TbtProductMoldPlanResin)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_mold_plan_resin_fk");
        });

        modelBuilder.Entity<TbtProductMoldPlanStore>(entity =>
        {
            entity.HasKey(e => new { e.PlanId, e.CodePlan, e.Code }).HasName("tbt_product_mold_plan_store_pk");

            entity.ToTable("tbt_product_mold_plan_store");

            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.CodePlan)
                .HasColumnType("character varying")
                .HasColumnName("code_plan");
            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.ImageUrl)
                .HasColumnType("character varying")
                .HasColumnName("image_url");
            entity.Property(e => e.Location)
                .HasColumnType("character varying")
                .HasColumnName("location");
            entity.Property(e => e.QtyReceive).HasColumnName("qty_receive");
            entity.Property(e => e.QtySend).HasColumnName("qty_send");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
            entity.Property(e => e.WorkerBy)
                .HasColumnType("character varying")
                .HasColumnName("worker_by");

            entity.HasOne(d => d.Plan).WithMany(p => p.TbtProductMoldPlanStore)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_mold_plan_store_fk");
        });

        modelBuilder.Entity<TbtProductionPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_production_plan_pk");

            entity.ToTable("tbt_production_plan");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompletedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("completed_date");
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
            entity.Property(e => e.IsReceipt).HasColumnName("is_receipt");
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
            entity.Property(e => e.ProductRunning)
                .HasColumnType("character varying")
                .HasColumnName("product_running");
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
            entity.Property(e => e.Type)
                .HasColumnType("character varying")
                .HasColumnName("type");
            entity.Property(e => e.TypeSize)
                .HasColumnType("character varying")
                .HasColumnName("type_size");
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
            entity.Property(e => e.WoText)
                .HasColumnType("character varying")
                .HasColumnName("wo_text");

            entity.HasOne(d => d.CustomerTypeNavigation).WithMany(p => p.TbtProductionPlan)
                .HasForeignKey(d => d.CustomerType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_customer_type_fk");

            entity.HasOne(d => d.ProductTypeNavigation).WithMany(p => p.TbtProductionPlan)
                .HasForeignKey(d => d.ProductType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_product_type_fk");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.TbtProductionPlan)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_status_fk");
        });

        modelBuilder.Entity<TbtProductionPlanBom>(entity =>
        {
            entity.HasKey(e => new { e.Running, e.Wo, e.WoNumber, e.No }).HasName("tbt_production_plan_bom_pk");

            entity.ToTable("tbt_production_plan_bom");

            entity.Property(e => e.Running)
                .HasColumnType("character varying")
                .HasColumnName("running");
            entity.Property(e => e.Wo)
                .HasColumnType("character varying")
                .HasColumnName("wo");
            entity.Property(e => e.WoNumber).HasColumnName("wo_number");
            entity.Property(e => e.No)
                .ValueGeneratedOnAdd()
                .HasColumnName("no");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.DisplayName)
                .HasColumnType("character varying")
                .HasColumnName("display_name");
            entity.Property(e => e.MatchCode)
                .HasColumnType("character varying")
                .HasColumnName("match_code");
            entity.Property(e => e.MatchName)
                .HasColumnType("character varying")
                .HasColumnName("match_name");
            entity.Property(e => e.OriginCode)
                .HasColumnType("character varying")
                .HasColumnName("origin_code");
            entity.Property(e => e.OriginName)
                .HasColumnType("character varying")
                .HasColumnName("origin_name");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.ProductionId).HasColumnName("production_id");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.Type)
                .HasColumnType("character varying")
                .HasColumnName("type");
            entity.Property(e => e.Unit)
                .HasColumnType("character varying")
                .HasColumnName("unit");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
            entity.Property(e => e.WoText)
                .HasColumnType("character varying")
                .HasColumnName("wo_text");

            entity.HasOne(d => d.Production).WithMany(p => p.TbtProductionPlanBom)
                .HasForeignKey(d => d.ProductionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_bom_fk");
        });

        modelBuilder.Entity<TbtProductionPlanCostGold>(entity =>
        {
            entity.HasKey(e => new { e.No, e.BookNo }).HasName("tbt_production_plan_cost_gold_pk");

            entity.ToTable("tbt_production_plan_cost_gold");

            entity.Property(e => e.No)
                .HasColumnType("character varying")
                .HasColumnName("no");
            entity.Property(e => e.BookNo)
                .HasColumnType("character varying")
                .HasColumnName("book_no");
            entity.Property(e => e.AssignBy)
                .HasColumnType("character varying")
                .HasColumnName("assign_by");
            entity.Property(e => e.AssignDate).HasColumnName("assign_date");
            entity.Property(e => e.CastDate).HasColumnName("cast_date");
            entity.Property(e => e.CastWeight).HasColumnName("cast_weight");
            entity.Property(e => e.CastWeightLoss).HasColumnName("cast_weight_loss");
            entity.Property(e => e.CastWeightOver).HasColumnName("cast_weight_over");
            entity.Property(e => e.Cost).HasColumnName("cost");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.GemWeight).HasColumnName("gem_weight");
            entity.Property(e => e.Gold)
                .HasColumnType("character varying")
                .HasColumnName("gold");
            entity.Property(e => e.GoldReceipt)
                .HasColumnType("character varying")
                .HasColumnName("gold_receipt");
            entity.Property(e => e.GoldSize)
                .HasColumnType("character varying")
                .HasColumnName("gold_size");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.MeltDate).HasColumnName("melt_date");
            entity.Property(e => e.MeltWeight).HasColumnName("melt_weight");
            entity.Property(e => e.MeltWeightLoss).HasColumnName("melt_weight_loss");
            entity.Property(e => e.MeltWeightOver).HasColumnName("melt_weight_over");
            entity.Property(e => e.ReceiveBy)
                .HasColumnType("character varying")
                .HasColumnName("receive_by");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.ReturnCastBodyBrokenedWeight).HasColumnName("return_cast_body_brokened_weight");
            entity.Property(e => e.ReturnCastBodyWeightTotal).HasColumnName("return_cast_body_weight_total");
            entity.Property(e => e.ReturnCastMoldWeight).HasColumnName("return_cast_mold_weight");
            entity.Property(e => e.ReturnCastPowderWeight).HasColumnName("return_cast_powder_weight");
            entity.Property(e => e.ReturnCastScrapWeight).HasColumnName("return_cast_scrap_weight");
            entity.Property(e => e.ReturnCastWeight).HasColumnName("return_cast_weight");
            entity.Property(e => e.ReturnMeltScrapWeight).HasColumnName("return_melt_scrap_weight");
            entity.Property(e => e.ReturnMeltWeight).HasColumnName("return_melt_weight");
            entity.Property(e => e.RunningNumber)
                .HasColumnType("character varying")
                .HasColumnName("running_number");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
            entity.Property(e => e.Zill)
                .HasColumnType("character varying")
                .HasColumnName("zill");
            entity.Property(e => e.ZillQty).HasColumnName("zill_qty");

            entity.HasOne(d => d.GoldNavigation).WithMany(p => p.TbtProductionPlanCostGold)
                .HasForeignKey(d => d.Gold)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_cost_gold_code_fk");

            entity.HasOne(d => d.GoldSizeNavigation).WithMany(p => p.TbtProductionPlanCostGold)
                .HasForeignKey(d => d.GoldSize)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_cost_gold_size_fk");
        });

        modelBuilder.Entity<TbtProductionPlanCostGoldItem>(entity =>
        {
            entity.HasKey(e => new { e.No, e.BookNo, e.ProductionPlanId }).HasName("tbt_production_plan_cost_gold_item_pk");

            entity.ToTable("tbt_production_plan_cost_gold_item");

            entity.Property(e => e.No)
                .HasColumnType("character varying")
                .HasColumnName("no");
            entity.Property(e => e.BookNo)
                .HasColumnType("character varying")
                .HasColumnName("book_no");
            entity.Property(e => e.ProductionPlanId)
                .HasColumnType("character varying")
                .HasColumnName("production_plan_id");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.ReturnQty).HasColumnName("return_qty");
            entity.Property(e => e.ReturnWeight).HasColumnName("return_weight");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.TbtProductionPlanCostGold).WithMany(p => p.TbtProductionPlanCostGoldItem)
                .HasForeignKey(d => new { d.No, d.BookNo })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("newtabletbt_production_plan_cost_gold_item_fk");
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

        modelBuilder.Entity<TbtProductionPlanPrice>(entity =>
        {
            entity.HasKey(e => new { e.Running, e.No, e.Wo, e.WoNumber }).HasName("tbt_production_plan_price_pk");

            entity.ToTable("tbt_production_plan_price");

            entity.Property(e => e.Running)
                .HasColumnType("character varying")
                .HasColumnName("running");
            entity.Property(e => e.No).HasColumnName("no");
            entity.Property(e => e.Wo)
                .HasColumnType("character varying")
                .HasColumnName("wo");
            entity.Property(e => e.WoNumber).HasColumnName("wo_number");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.IsManualAdd).HasColumnName("is_manual_add");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.NameDescription)
                .HasColumnType("character varying")
                .HasColumnName("name_description");
            entity.Property(e => e.NameGroup)
                .HasColumnType("character varying")
                .HasColumnName("name_group");
            entity.Property(e => e.ProductionId).HasColumnName("production_id");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.QtyPrice).HasColumnName("qty_price");
            entity.Property(e => e.QtyWeight).HasColumnName("qty_weight");
            entity.Property(e => e.QtyWeightPrice).HasColumnName("qty_weight_price");
            entity.Property(e => e.TotalPrice).HasColumnName("total_price");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
            entity.Property(e => e.WoText)
                .HasColumnType("character varying")
                .HasColumnName("wo_text");

            entity.HasOne(d => d.Production).WithMany(p => p.TbtProductionPlanPrice)
                .HasForeignKey(d => d.ProductionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_price_fk");
        });

        modelBuilder.Entity<TbtProductionPlanStatusDetail>(entity =>
        {
            entity.HasKey(e => new { e.ProductionPlanId, e.ItemNo }).HasName("tbt_production_plan_status_detail_pk");

            entity.ToTable("tbt_production_plan_status_detail");

            entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
            entity.Property(e => e.ItemNo)
                .HasColumnType("character varying")
                .HasColumnName("item_no");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.Gold)
                .HasColumnType("character varying")
                .HasColumnName("gold");
            entity.Property(e => e.GoldQtyCheck).HasColumnName("gold_qty_check");
            entity.Property(e => e.GoldQtySend).HasColumnName("gold_qty_send");
            entity.Property(e => e.GoldWeightCheck).HasColumnName("gold_weight_check");
            entity.Property(e => e.GoldWeightDiff).HasColumnName("gold_weight_diff");
            entity.Property(e => e.GoldWeightDiffPercent).HasColumnName("gold_weight_diff_percent");
            entity.Property(e => e.GoldWeightSend).HasColumnName("gold_weight_send");
            entity.Property(e => e.HeaderId).HasColumnName("header_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.RequestDate).HasColumnName("request_date");
            entity.Property(e => e.TotalWages).HasColumnName("total_wages");
            entity.Property(e => e.Wages).HasColumnName("wages");
            entity.Property(e => e.Worker)
                .HasColumnType("character varying")
                .HasColumnName("worker");
            entity.Property(e => e.WorkerSub)
                .HasColumnType("character varying")
                .HasColumnName("worker_sub");

            entity.HasOne(d => d.Header).WithMany(p => p.TbtProductionPlanStatusDetail)
                .HasForeignKey(d => d.HeaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_status_header_fk");
        });

        modelBuilder.Entity<TbtProductionPlanStatusDetailGem>(entity =>
        {
            entity.HasKey(e => new { e.ProductionPlanId, e.ItemNo }).HasName("tbt_production_plan_status_detail_gem_pk");

            entity.ToTable("tbt_production_plan_status_detail_gem");

            entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
            entity.Property(e => e.ItemNo)
                .HasColumnType("character varying")
                .HasColumnName("item_no");
            entity.Property(e => e.GemCode)
                .HasColumnType("character varying")
                .HasColumnName("gem_code");
            entity.Property(e => e.GemId).HasColumnName("gem_id");
            entity.Property(e => e.GemName)
                .HasColumnType("character varying")
                .HasColumnName("gem_name");
            entity.Property(e => e.GemPrice).HasColumnName("gem_price");
            entity.Property(e => e.GemQty).HasColumnName("gem_qty");
            entity.Property(e => e.GemWeight).HasColumnName("gem_weight");
            entity.Property(e => e.HeaderId).HasColumnName("header_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.OutboundDate).HasColumnName("outbound_date");
            entity.Property(e => e.OutboundName)
                .HasColumnType("character varying")
                .HasColumnName("outbound_name");
            entity.Property(e => e.OutboundRunning)
                .HasColumnType("character varying")
                .HasColumnName("outbound_running");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.RequestDate).HasColumnName("request_date");

            entity.HasOne(d => d.Header).WithMany(p => p.TbtProductionPlanStatusDetailGem)
                .HasForeignKey(d => d.HeaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_status_header_fk");
        });

        modelBuilder.Entity<TbtProductionPlanStatusHeader>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_production_plan_status_header_pk");

            entity.ToTable("tbt_production_plan_status_header");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
            entity.Property(e => e.Remark1)
                .HasColumnType("character varying")
                .HasColumnName("remark_1");
            entity.Property(e => e.Remark2)
                .HasColumnType("character varying")
                .HasColumnName("remark_2");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
            entity.Property(e => e.WagesTotal).HasColumnName("wages_total");
            entity.Property(e => e.WorkerCode)
                .HasColumnType("character varying")
                .HasColumnName("worker_code");
            entity.Property(e => e.WorkerName)
                .HasColumnType("character varying")
                .HasColumnName("worker_name");

            entity.HasOne(d => d.ProductionPlan).WithMany(p => p.TbtProductionPlanStatusHeader)
                .HasForeignKey(d => d.ProductionPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_fk");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.TbtProductionPlanStatusHeader)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_status_fk");
        });

        modelBuilder.Entity<TbtProductionPlanTransferStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_production_plan_transfer_status_pk");

            entity.ToTable("tbt_production_plan_transfer_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.FormerStatus).HasColumnName("former_status");
            entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
            entity.Property(e => e.Running)
                .HasColumnType("character varying")
                .HasColumnName("running");
            entity.Property(e => e.TargetStatus).HasColumnName("target_status");
            entity.Property(e => e.TargetStatusId).HasColumnName("target_status_id");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
            entity.Property(e => e.Wo)
                .HasColumnType("character varying")
                .HasColumnName("wo");
            entity.Property(e => e.WoNumber).HasColumnName("wo_number");
            entity.Property(e => e.WorkerCode)
                .HasColumnType("character varying")
                .HasColumnName("worker_code");
            entity.Property(e => e.WorkerName)
                .HasColumnType("character varying")
                .HasColumnName("worker_name");

            entity.HasOne(d => d.ProductionPlan).WithMany(p => p.TbtProductionPlanTransferStatus)
                .HasForeignKey(d => d.ProductionPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_production_plan_transfer_status_plan_fk");
        });

        modelBuilder.Entity<TbtReceiptStockCreate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_receipt_stock_request_pk");

            entity.ToTable("tbt_receipt_stock_create");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('tbt_receipt_stock_request_id_seq'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Running)
                .HasColumnType("character varying")
                .HasColumnName("running");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
        });

        modelBuilder.Entity<TbtRunningNumber>(entity =>
        {
            entity.HasKey(e => e.Key).HasName("tbt_running_number_pk");

            entity.ToTable("tbt_running_number");

            entity.Property(e => e.Key)
                .HasColumnType("character varying")
                .HasColumnName("key");
            entity.Property(e => e.Number).HasColumnName("number");
        });

        modelBuilder.Entity<TbtSaleQuotation>(entity =>
        {
            entity.HasKey(e => new { e.Running, e.Number }).HasName("tbt_sale_quotation_pk");

            entity.ToTable("tbt_sale_quotation");

            entity.Property(e => e.Running)
                .HasColumnType("character varying")
                .HasColumnName("running");
            entity.Property(e => e.Number)
                .HasColumnType("character varying")
                .HasColumnName("number");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Currency)
                .HasColumnType("character varying")
                .HasColumnName("currency");
            entity.Property(e => e.CurrencyRate).HasColumnName("currency_rate");
            entity.Property(e => e.CustomerAddress)
                .HasColumnType("character varying")
                .HasColumnName("customer_address");
            entity.Property(e => e.CustomerEmail)
                .HasColumnType("character varying")
                .HasColumnName("customer_email");
            entity.Property(e => e.CustomerName)
                .HasColumnType("character varying")
                .HasColumnName("customer_name");
            entity.Property(e => e.CustomerPhone)
                .HasColumnType("character varying")
                .HasColumnName("customer_phone");
            entity.Property(e => e.Data)
                .HasColumnType("character varying")
                .HasColumnName("data");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Discount).HasColumnName("discount");
            entity.Property(e => e.Freight).HasColumnName("freight");
            entity.Property(e => e.MarkUp).HasColumnName("mark_up");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
        });

        modelBuilder.Entity<TbtStockGem>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.Code }).HasName("tbt_stock_gem_pk");

            entity.ToTable("tbt_stock_gem");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Daterec).HasColumnName("daterec");
            entity.Property(e => e.Grade)
                .HasMaxLength(50)
                .HasColumnName("grade");
            entity.Property(e => e.GradeCode)
                .HasColumnType("character varying")
                .HasColumnName("grade_code");
            entity.Property(e => e.GradeDia)
                .HasMaxLength(50)
                .HasColumnName("grade_dia");
            entity.Property(e => e.GroupName)
                .HasMaxLength(50)
                .HasColumnName("group_name");
            entity.Property(e => e.Original)
                .HasMaxLength(50)
                .HasColumnName("original");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.PriceQty).HasColumnName("price_qty");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.QuantityOnProcess).HasColumnName("quantity_on_process");
            entity.Property(e => e.QuantityWeight).HasColumnName("quantity_weight");
            entity.Property(e => e.QuantityWeightOnProcess).HasColumnName("quantity_weight_on_process");
            entity.Property(e => e.Remark1)
                .HasMaxLength(50)
                .HasColumnName("remark_1");
            entity.Property(e => e.Remark2)
                .HasMaxLength(50)
                .HasColumnName("remark_2");
            entity.Property(e => e.Shape)
                .HasMaxLength(50)
                .HasColumnName("shape");
            entity.Property(e => e.Size)
                .HasMaxLength(50)
                .HasColumnName("size");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");
            entity.Property(e => e.UnitCode)
                .HasMaxLength(50)
                .HasColumnName("unit_code");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.GradeCodeNavigation).WithMany(p => p.TbtStockGem)
                .HasForeignKey(d => d.GradeCode)
                .HasConstraintName("tbt_stock_gem_gold_size_fk");

            entity.HasOne(d => d.ShapeNavigation).WithMany(p => p.TbtStockGem)
                .HasForeignKey(d => d.Shape)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_stock_gem_shape_fk");
        });

        modelBuilder.Entity<TbtStockGemTransection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_stock_gem_transection_pk");

            entity.ToTable("tbt_stock_gem_transection");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.JobOrPo)
                .HasColumnType("character varying")
                .HasColumnName("job_or_po");
            entity.Property(e => e.OperatorBy)
                .HasColumnType("character varying")
                .HasColumnName("operator_by");
            entity.Property(e => e.PointRemianQty).HasColumnName("point_remian_qty");
            entity.Property(e => e.PointRemianQtyWeight).HasColumnName("point_remian_qty_weight");
            entity.Property(e => e.PreviousRemainQty).HasColumnName("previous_remain_qty");
            entity.Property(e => e.PreviousRemianQtyWeight).HasColumnName("previous_remian_qty_weight");
            entity.Property(e => e.ProductionPlanMold)
                .HasColumnType("character varying")
                .HasColumnName("production_plan_mold");
            entity.Property(e => e.ProductionPlanWo)
                .HasColumnType("character varying")
                .HasColumnName("production_plan_wo");
            entity.Property(e => e.ProductionPlanWoNumber).HasColumnName("production_plan_wo_number");
            entity.Property(e => e.ProductionPlanWoText)
                .HasColumnType("character varying")
                .HasColumnName("production_plan_wo_text");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.QtyWeight).HasColumnName("qty_weight");
            entity.Property(e => e.Ref2Running)
                .HasColumnType("character varying")
                .HasColumnName("ref2_running");
            entity.Property(e => e.RefRunning)
                .HasColumnType("character varying")
                .HasColumnName("ref_running");
            entity.Property(e => e.Remark1)
                .HasColumnType("character varying")
                .HasColumnName("remark_1");
            entity.Property(e => e.Remark2)
                .HasColumnType("character varying")
                .HasColumnName("remark_2");
            entity.Property(e => e.RequestDate).HasColumnName("request_date");
            entity.Property(e => e.ReturnDate).HasColumnName("return_date");
            entity.Property(e => e.Running)
                .HasColumnType("character varying")
                .HasColumnName("running");
            entity.Property(e => e.Stastus)
                .HasColumnType("character varying")
                .HasColumnName("stastus");
            entity.Property(e => e.SubpplierName)
                .HasColumnType("character varying")
                .HasColumnName("subpplier_name");
            entity.Property(e => e.SupplierCost).HasColumnName("supplier_cost");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
        });

        modelBuilder.Entity<TbtStockGemTransectionPrice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_stock_gem_transection_price_pk");

            entity.ToTable("tbt_stock_gem_transection_price");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.NewPrice).HasColumnName("new_price");
            entity.Property(e => e.NewPriceUnit).HasColumnName("new_price_unit");
            entity.Property(e => e.PreviousPrice).HasColumnName("previous_price");
            entity.Property(e => e.PreviousPriceUnit).HasColumnName("previous_price_unit");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.Unit)
                .HasColumnType("character varying")
                .HasColumnName("unit");
            entity.Property(e => e.UnitCode)
                .HasColumnType("character varying")
                .HasColumnName("unit_code");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
        });

        modelBuilder.Entity<TbtStockProduct>(entity =>
        {
            entity.HasKey(e => e.StockNumber).HasName("tbt_stock_product_pk");

            entity.ToTable("tbt_stock_product");

            entity.Property(e => e.StockNumber)
                .HasColumnType("character varying")
                .HasColumnName("stock_number");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.ImageName)
                .HasColumnType("character varying")
                .HasColumnName("image_name");
            entity.Property(e => e.ImagePath)
                .HasColumnType("character varying")
                .HasColumnName("image_path");
            entity.Property(e => e.Location)
                .HasColumnType("character varying")
                .HasColumnName("location");
            entity.Property(e => e.Mold)
                .HasColumnType("character varying")
                .HasColumnName("mold");
            entity.Property(e => e.MoldDesign)
                .HasColumnType("character varying")
                .HasColumnName("mold_design");
            entity.Property(e => e.PoNumber)
                .HasColumnType("character varying")
                .HasColumnName("po_number");
            entity.Property(e => e.ProductCode)
                .HasColumnType("character varying")
                .HasColumnName("product_code");
            entity.Property(e => e.ProductCost).HasColumnName("product_cost");
            entity.Property(e => e.ProductNameEn)
                .HasColumnType("character varying")
                .HasColumnName("product_name_en");
            entity.Property(e => e.ProductNameTh)
                .HasColumnType("character varying")
                .HasColumnName("product_name_th");
            entity.Property(e => e.ProductNumber)
                .HasColumnType("character varying")
                .HasColumnName("product_number");
            entity.Property(e => e.ProductPrice).HasColumnName("product_price");
            entity.Property(e => e.ProductType)
                .HasComment("รหัสประเภทสินค้า")
                .HasColumnType("character varying")
                .HasColumnName("product_type");
            entity.Property(e => e.ProductTypeName)
                .HasComment("ประเภทสินค้า")
                .HasColumnType("character varying")
                .HasColumnName("product_type_name");
            entity.Property(e => e.ProductionDate).HasColumnName("production_date");
            entity.Property(e => e.ProductionType)
                .HasComment("Gold/Silver")
                .HasColumnType("character varying")
                .HasColumnName("production_type");
            entity.Property(e => e.ProductionTypeSize)
                .HasComment("10 K, 18K ....")
                .HasColumnType("character varying")
                .HasColumnName("production_type_size");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.ReceiptDate).HasColumnName("receipt_date");
            entity.Property(e => e.ReceiptNumber)
                .HasColumnType("character varying")
                .HasColumnName("receipt_number");
            entity.Property(e => e.ReceiptType)
                .HasColumnType("character varying")
                .HasColumnName("receipt_type");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.Size)
                .HasColumnType("character varying")
                .HasColumnName("size");
            entity.Property(e => e.Status)
                .HasColumnType("character varying")
                .HasColumnName("status");
            entity.Property(e => e.StudEarring)
                .HasComment("แป้นต่างหู")
                .HasColumnType("character varying")
                .HasColumnName("stud_earring");
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
            entity.Property(e => e.WoOrigin)
                .HasColumnType("character varying")
                .HasColumnName("wo_origin");
        });

        modelBuilder.Entity<TbtStockProductImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tbt_stock_product_image_pk");

            entity.ToTable("tbt_stock_product_image");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('tbt_stock_product_image_column1_seq'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("true")
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.NamePath)
                .HasColumnType("character varying")
                .HasColumnName("name_path");
            entity.Property(e => e.Remark)
                .HasColumnType("character varying")
                .HasColumnName("remark");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<TbtStockProductMaterial>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.StockNumber }).HasName("tbt_stock_product_material_pk");

            entity.ToTable("tbt_stock_product_material");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.StockNumber)
                .HasColumnType("character varying")
                .HasColumnName("stock_number");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.QtyUnit)
                .HasColumnType("character varying")
                .HasColumnName("qty_unit");
            entity.Property(e => e.Region)
                .HasColumnType("character varying")
                .HasColumnName("region");
            entity.Property(e => e.Size)
                .HasColumnType("character varying")
                .HasColumnName("size");
            entity.Property(e => e.Type)
                .HasColumnType("character varying")
                .HasColumnName("type");
            entity.Property(e => e.TypeBarcode)
                .HasColumnType("character varying")
                .HasColumnName("type_barcode");
            entity.Property(e => e.TypeCode)
                .HasColumnType("character varying")
                .HasColumnName("type_code");
            entity.Property(e => e.TypeName)
                .HasColumnType("character varying")
                .HasColumnName("type_name");
            entity.Property(e => e.TypeOrigin)
                .HasColumnType("character varying")
                .HasColumnName("type_origin");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
            entity.Property(e => e.Weight).HasColumnName("weight");
            entity.Property(e => e.WeightUnit)
                .HasColumnType("character varying")
                .HasColumnName("weight_unit");

            entity.HasOne(d => d.StockNumberNavigation).WithMany(p => p.TbtStockProductMaterial)
                .HasForeignKey(d => d.StockNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_stock_product_material_fk");
        });

        modelBuilder.Entity<TbtStockProductReceiptItem>(entity =>
        {
            entity.HasKey(e => e.StockReceiptNumber).HasName("tbt_stock_product_receipt_pk");

            entity.ToTable("tbt_stock_product_receipt_item");

            entity.Property(e => e.StockReceiptNumber)
                .HasColumnType("character varying")
                .HasColumnName("stock_receipt_number");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.IsReceipt).HasColumnName("is_receipt");
            entity.Property(e => e.Mold)
                .HasColumnType("character varying")
                .HasColumnName("mold");
            entity.Property(e => e.MoldDesign)
                .HasColumnType("character varying")
                .HasColumnName("mold_design");
            entity.Property(e => e.Po)
                .HasColumnType("character varying")
                .HasColumnName("po");
            entity.Property(e => e.ProductType)
                .HasComment("รหัสประเภทสินค้า")
                .HasColumnType("character varying")
                .HasColumnName("product_type");
            entity.Property(e => e.ProductTypeName)
                .HasComment("ประเภทสินค้า")
                .HasColumnType("character varying")
                .HasColumnName("product_type_name");
            entity.Property(e => e.ProductionType)
                .HasComment("Gold/Silver")
                .HasColumnType("character varying")
                .HasColumnName("production_type");
            entity.Property(e => e.ProductionTypeSize)
                .HasComment("10 K, 18K ....")
                .HasColumnType("character varying")
                .HasColumnName("production_type_size");
            entity.Property(e => e.ReceiptDate).HasColumnName("receipt_date");
            entity.Property(e => e.Running)
                .HasColumnType("character varying")
                .HasColumnName("running");
            entity.Property(e => e.StockNumber)
                .HasColumnType("character varying")
                .HasColumnName("stock_number");
            entity.Property(e => e.Type)
                .HasDefaultValueSql("'production'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("type");
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
            entity.Property(e => e.WoText)
                .HasColumnType("character varying")
                .HasColumnName("wo_text");

            entity.HasOne(d => d.RunningNavigation).WithMany(p => p.TbtStockProductReceiptItem)
                .HasForeignKey(d => d.Running)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_stock_product_receipt_item_fk");
        });

        modelBuilder.Entity<TbtStockProductReceiptPlan>(entity =>
        {
            entity.HasKey(e => e.Running).HasName("tbt_stock_product_receipt_plan_pk");

            entity.ToTable("tbt_stock_product_receipt_plan");

            entity.Property(e => e.Running)
                .HasColumnType("character varying")
                .HasColumnName("running");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.IsComplete).HasColumnName("is_complete");
            entity.Property(e => e.IsRunning).HasColumnName("is_running");
            entity.Property(e => e.JsonDraft)
                .HasColumnType("json")
                .HasColumnName("json_draft");
            entity.Property(e => e.PoNumber)
                .HasColumnType("character varying")
                .HasColumnName("po_number");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.QtyRunning).HasColumnName("qty_running");
            entity.Property(e => e.Type)
                .HasDefaultValueSql("'production'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("type");
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
            entity.Property(e => e.WoText)
                .HasColumnType("character varying")
                .HasColumnName("wo_text");
        });

        modelBuilder.Entity<TbtUser>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.Username }).HasName("tbt_user_pk");

            entity.ToTable("tbt_user");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.Username)
                .HasMaxLength(20)
                .HasColumnName("username");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.FirstName)
                .HasColumnType("character varying")
                .HasColumnName("first_name");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsNew).HasColumnName("is_new");
            entity.Property(e => e.LastLogin).HasColumnName("last_login");
            entity.Property(e => e.LastName)
                .HasColumnType("character varying")
                .HasColumnName("last_name");
            entity.Property(e => e.Password)
                .HasColumnType("character varying")
                .HasColumnName("password");
            entity.Property(e => e.Salt)
                .HasColumnType("character varying")
                .HasColumnName("salt");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");
        });

        modelBuilder.Entity<TbtUserRole>(entity =>
        {
            entity.HasKey(e => new { e.Username, e.Role, e.UserId }).HasName("tbt_user_role_pk");

            entity.ToTable("tbt_user_role");

            entity.Property(e => e.Username)
                .HasColumnType("character varying")
                .HasColumnName("username");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreateBy)
                .HasColumnType("character varying")
                .HasColumnName("create_by");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.UpdateBy)
                .HasColumnType("character varying")
                .HasColumnName("update_by");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date");

            entity.HasOne(d => d.RoleNavigation).WithMany(p => p.TbtUserRole)
                .HasForeignKey(d => d.Role)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_user_role_master_fk");

            entity.HasOne(d => d.User).WithMany(p => p.TbtUserRole)
                .HasForeignKey(d => new { d.UserId, d.Username })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tbt_user_role_user_fk");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
