using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Helper
{
    public static class Datetime
    {
        public static DateTime EndOfDay(this DateTime source)
        {
            return source.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
        }

        public static DateTimeOffset EndOfDay(this DateTimeOffset source)
        {
            return new DateTimeOffset(source.Date.AddHours(23).AddMinutes(59).AddSeconds(59), source.Offset);
        }


        public static DateTimeOffset EndOfDayUtc(this DateTimeOffset source)
        {
            return EndOfDay(source).UtcDateTime;
        }

        public static DateTimeOffset StartOfDayUtc(this DateTimeOffset source)
        {
            return StartOfDay(source).UtcDateTime;
        }

        public static DateTimeOffset StartOfDay(this DateTimeOffset source)
        {
            return new DateTimeOffset(source.Date.AddHours(0).AddMinutes(0).AddSeconds(0), source.Offset);
        }

        public static DateTime? StartOfDay(this DateTime? source)
        {
            if (!source.HasValue)
                return null;

            return source.Value.Date;
        }
        public static DateTime? EndOfDay(this DateTime? source)
        {
            if (!source.HasValue)
                return null;
            return source.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
        }

        public static DateTimeOffset? EndOfDay(this DateTimeOffset? source)
        {
            if (!source.HasValue)
                return null;

            return source.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
        }

        public static DateTime Max(DateTime a, DateTime b)
        {
            return a > b ? a : b;
        }

        public static DateTime Max(DateTime a, DateTime? b)
        {
            if (!b.HasValue)
                return a;

            return a > b.Value ? a : b.Value;
        }

        public static string ToStringWithTimeZone(this DateTime dateTime, string format, string timeZoneId = null)
        {
            //https://stackoverflow.com/questions/14149346/what-value-should-i-pass-into-timezoneinfo-findsystemtimezonebyidstring
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId ?? "SE Asia Standard Time");
            var newDateTimeZone = TimeZoneInfo.ConvertTime(dateTime, tzi);
            return newDateTimeZone.ToString(format);
        }

        public static DateTime ToTimeZone(this DateTime dateTime, string timeZoneId = null)
        {
            //https://stackoverflow.com/questions/14149346/what-value-should-i-pass-into-timezoneinfo-findsystemtimezonebyidstring
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId ?? "SE Asia Standard Time");

            return TimeZoneInfo.ConvertTime(dateTime, tzi);
            //var newDateTimeZone = TimeZoneInfo.ConvertTime(dateTime, tzi);
            //return newDateTimeZone.ToString(format);
        }

        public static DateTime ClearLast4DigitTicksOfDateTime(this DateTime dateTime)
        {
            var decTrick = (decimal)dateTime.Ticks;
            var newTrick = (long)(Math.Floor(decTrick / 10000) * 10000);
            return new DateTime(newTrick, dateTime.Kind);
        }
    }
}
