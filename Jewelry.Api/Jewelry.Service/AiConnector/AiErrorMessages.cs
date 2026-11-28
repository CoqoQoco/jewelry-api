namespace Jewelry.Service.AiConnector
{
    public static class AiErrorCodes
    {
        public const string UnknownTool = "unknown_tool";
        public const string ScopeDenied = "scope_denied";
        public const string PermissionDenied = "permission_denied";
        public const string InvalidInput = "invalid_input";
        public const string RateLimited = "rate_limited";
        public const string ToolError = "tool_error";
        public const string Unauthorized = "unauthorized";
        public const string Suspended = "suspended";
    }

    public static class AiErrorMessages
    {
        public const string SuspendedTh = "บัญชีถูกระงับการใช้ AI ติดต่อผู้ดูแลระบบ";

        // เหตุผลภาษาคน — ใช้ทั้งตอบ AI (ข้อความสั้น ไม่มี stack trace) และแสดงในประวัติการดึงข้อมูล
        public static string Thai(string? code) => code switch
        {
            null or "" => "",
            AiErrorCodes.UnknownTool => "ไม่พบเครื่องมือที่ AI เรียก",
            AiErrorCodes.ScopeDenied => "AI ยังไม่ได้รับอนุญาตให้ใช้ข้อมูลส่วนนี้ (ขอบเขตการอนุญาตไม่ครอบคลุม)",
            AiErrorCodes.PermissionDenied => "บัญชีผู้ใช้ไม่มีสิทธิ์เข้าถึงข้อมูลส่วนนี้",
            AiErrorCodes.InvalidInput => "AI ส่งพารามิเตอร์ที่ไม่ถูกต้อง",
            AiErrorCodes.RateLimited => "เรียกถี่เกินกำหนด (จำกัดครั้งต่อนาที) กรุณารอสักครู่",
            AiErrorCodes.ToolError => "ระบบดึงข้อมูลไม่สำเร็จ (ข้อผิดพลาดภายใน)",
            AiErrorCodes.Unauthorized => "การเชื่อมต่อหมดอายุหรือถูกยกเลิก",
            AiErrorCodes.Suspended => SuspendedTh,
            _ => "เกิดข้อผิดพลาด",
        };
    }
}
