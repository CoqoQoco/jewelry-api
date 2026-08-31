using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Data.Context;

// Additive partial-class extension for tbt_pos_checkout (POS/Checkout idempotency).
// JewelryContext.cs is scaffold-generated and is not edited directly. The model
// mapping for TbtPosCheckout is added to the existing OnModelCreatingPartial()
// implementation in JewelryContextExtensions.cs (only one implementation of that
// partial method is allowed) — this file only adds the DbSet property.
public partial class JewelryContext
{
    public virtual DbSet<TbtPosCheckout> TbtPosCheckout { get; set; }
}
