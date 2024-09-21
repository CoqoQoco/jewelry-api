using jewelry.Model.Mold.PlanCasting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Exceptions
{
    public static class ErrorMessage
    {
        public static string PermissionFail = "ขออภัย คุณไม่มีสิทธิ์ที่จะดำเนินการรายการนี้";
        public static string NotFound = "ไม่พบข้อมูล";

        public static string InvalidRequest = "ข้อมูลที่ส่งมาไม่ถูกต้อง";

        public static string QtyLessThanAction = "จำนวนคงคลังไม่เพียงพอ";
        public static string QtyWeightLessThanAction = "น้ำหนักคงคลังไม่เพียงพอ";
        public static string InvalidQty = "จำนวนที่ส่งมาไม่ถูกต้อง";

        public static string PlanCompleted = "แผนการผลิตนี้ได้ทำการผลิตเสร็จสิ้นแล้ว";
        public static string PlanMelted = "แผนการผลิตนี้ได้ทำการหลอมเเล้ว";

        public static string PickReturned = "รายการนี้ได้ทำการคืนเเล้ว";
    }
}
