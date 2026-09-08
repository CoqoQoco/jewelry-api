using Jewelry.Data.Models.Jewelry;

namespace Jewelry.Service.Stock
{
    public static class StockPieceQtyHelper
    {
        public static decimal Available(TbtStockPiece piece)
        {
            var available = piece.Qty - piece.QtyReserved;
            return available > 0 ? available : 0;
        }

        public static void RecalcStatus(TbtStockPiece piece)
        {
            if (piece.Qty <= 0)
            {
                piece.Status = "SOLD";
            }
            else if (piece.QtyReserved >= piece.Qty)
            {
                piece.Status = "RESERVED";
            }
            else
            {
                piece.Status = "IN_STOCK";
            }
        }
    }
}
