﻿using System;
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
    }
}