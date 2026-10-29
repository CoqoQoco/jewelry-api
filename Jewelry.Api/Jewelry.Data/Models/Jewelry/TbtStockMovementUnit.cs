namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockMovementUnit
{
    public long Id { get; set; }

    public long MovementId { get; set; }

    public long UnitId { get; set; }

    public virtual TbtStockMovement Movement { get; set; } = null!;

    public virtual TbtStockUnit Unit { get; set; } = null!;
}
