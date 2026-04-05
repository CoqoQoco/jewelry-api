using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Data.Context;

public partial class JewelryContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Override ProductCostDetail จาก "jsonb" → "text"
        // เพื่อให้ Npgsql อ่านเป็น plain string แทนการ validate JSON
        // รองรับแถวที่มี invalid JSON โดย C# จัดการ deserialization เอง
        modelBuilder.Entity<TbtStockProduct>()
            .Property(e => e.ProductCostDetail)
            .HasColumnType("text");

        modelBuilder.Entity<TbtStockCostVersion>()
            .Property(e => e.ProductCostDetail)
            .HasColumnType("text");
    }
}
