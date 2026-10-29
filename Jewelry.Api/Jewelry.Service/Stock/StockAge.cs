using System;

namespace Jewelry.Service.Stock
{
    // อายุสต็อกนับเป็นวันตามปฏิทินเวลาไทย (UTC+7) — วันที่เก็บเป็น timestamptz UTC เที่ยงคืนไทย = 17:00Z ของวันก่อนหน้า
    public static class StockAge
    {
        private const int ThaiOffsetHours = 7;

        public static DateTime ThaiDate(DateTime utc) => utc.AddHours(ThaiOffsetHours).Date;

        public static DateTime ThaiToday(DateTime utcNow) => ThaiDate(utcNow);

        public static int DaysOld(DateTime ageDateUtc, DateTime utcNow)
            => (int)(ThaiToday(utcNow) - ThaiDate(ageDateUtc)).TotalDays;
    }
}
