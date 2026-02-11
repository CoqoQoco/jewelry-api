using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Constant
{
    public static class JobStatus
    {
        public static int Pending = 10;
        public static int Assigned = 20;
        public static int Started = 30;
        public static int InProgress = 40;
        public static int OnHold = 50;
        public static int Completed = 100;
        public static int Cancelled = 500;

        /// <summary>
        /// Get status name from status code
        /// </summary>
        /// <param name="statusCode">Status code</param>
        /// <returns>Status name in Thai</returns>
        public static string GetStatusName(int statusCode)
        {
            return statusCode switch
            {
                10 => "รอดำเนินการ",
                20 => "มอบหมายแล้ว",
                30 => "เริ่มงาน",
                40 => "กำลังดำเนินการ",
                50 => "พักงาน",
                100 => "เสร็จสิ้น",
                500 => "ยกเลิก",
                _ => "ไม่ระบุสถานะ"
            };
        }

        /// <summary>
        /// Get status name in English from status code
        /// </summary>
        /// <param name="statusCode">Status code</param>
        /// <returns>Status name in English</returns>
        public static string GetStatusNameEn(int statusCode)
        {
            return statusCode switch
            {
                10 => "Pending",
                20 => "Assigned",
                30 => "Started",
                40 => "In Progress",
                50 => "On Hold",
                100 => "Completed",
                500 => "Cancelled",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get all statuses as dictionary (for dropdown, etc.)
        /// </summary>
        /// <returns>Dictionary of status code and name</returns>
        public static Dictionary<int, string> GetAllStatuses()
        {
            return new Dictionary<int, string>
            {
                { Pending, "รอดำเนินการ" },
                { Assigned, "มอบหมายแล้ว" },
                { Started, "เริ่มงาน" },
                { InProgress, "กำลังดำเนินการ" },
                { OnHold, "พักงาน" },
                { Completed, "เสร็จสิ้น" },
                { Cancelled, "ยกเลิก" }
            };
        }
    }

    public enum JobStatusEnum
    {
        Pending = 10,
        Assigned = 20,
        Started = 30,
        InProgress = 40,
        OnHold = 50,
        Completed = 100,
        Cancelled = 500
    }
}
