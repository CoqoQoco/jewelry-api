using System.Collections.Generic;

namespace jewelry.Model.Constant
{
    public static class GoldStockTransactionType
    {
        public const int Inbound = 1;          // รับเข้าคลัง [ซื้อ/รับใหม่]
        public const int OpeningBalance = 2;   // ตั้งยอดยกมา
        public const int ReturnIn = 3;         // คืนเข้าคลัง [จากใบเบิกผสมทอง]
        public const int Outbound = 4;         // เบิกออกคลัง [ใบเบิกผสมทอง]
        public const int AdjustIncrease = 5;   // ปรับยอดเพิ่ม
        public const int AdjustDecrease = 6;   // ปรับยอดลด
        public const int ReversalIncrease = 7; // กลับรายการเพิ่ม [แก้ไขรายการ] (reversal ที่ทำให้ยอดเพิ่ม)
        public const int ReversalDecrease = 8; // กลับรายการลด [แก้ไขรายการ] (reversal ที่ทำให้ยอดลด)

        private static readonly HashSet<int> InboundTypes = new() { Inbound, OpeningBalance, ReturnIn, AdjustIncrease, ReversalIncrease };
        private static readonly HashSet<int> OutboundTypes = new() { Outbound, AdjustDecrease, ReversalDecrease };

        public static bool IsInbound(int type) => InboundTypes.Contains(type);

        public static bool IsOutbound(int type) => OutboundTypes.Contains(type);

        public static bool IsValid(int type) => InboundTypes.Contains(type) || OutboundTypes.Contains(type);
    }

    public static class GoldStockTransactionStatus
    {
        public const string Completed = "completed";
        public const string Reversed = "reversed";
    }
}
