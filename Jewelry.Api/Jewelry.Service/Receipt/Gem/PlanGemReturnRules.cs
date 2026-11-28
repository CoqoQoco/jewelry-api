using jewelry.Model.Exceptions;

using System.Text.RegularExpressions;

namespace Jewelry.Service.Receipt.Gem
{
    public static class PlanGemReturnRules
    {
        private static readonly Regex PlanRefRegex = new Regex(@"(\d{7})\s*[-/]\s*(\d+)", RegexOptions.Compiled);

        public static (string Wo, int WoNumber)? ParsePlanRef(string? jobOrPo)
        {
            if (string.IsNullOrEmpty(jobOrPo)) return null;
            var m = PlanRefRegex.Match(jobOrPo);
            if (m.Success && int.TryParse(m.Groups[2].Value, out var no))
            {
                return (m.Groups[1].Value, no);
            }
            return null;
        }

        public static bool MatchesPlan(string? jobOrPo, string wo, int woNumber)
        {
            var r = ParsePlanRef(jobOrPo);
            return r.HasValue && r.Value.Wo == wo && r.Value.WoNumber == woNumber;
        }

        public static void CheckLinkPlan(string? jobOrPo, string wo, int woNumber)
        {
            var r = ParsePlanRef(jobOrPo);
            if (r.HasValue && (r.Value.Wo != wo || r.Value.WoNumber != woNumber))
            {
                throw new HandleException($"ใบรับคืนนี้อ้างใบงานอื่น ({jobOrPo})");
            }
        }

        public const int StatusCompleted = 100;
        public const int StatusMelted = 500;

        public const string ModeReturnNow = "RETURN_NOW";
        public const string ModeLinkExisting = "LINK_EXISTING";

        public static readonly string[] Reasons = { "DUPLICATE", "WRONG_PLAN", "OVER", "DAMAGED", "OTHER" };

        public static string ReasonText(string reason) => reason switch
        {
            "DUPLICATE" => "เบิกซ้ำ",
            "WRONG_PLAN" => "เบิกผิดใบงาน",
            "OVER" => "เบิกเกิน",
            "DAMAGED" => "พลอยเสียหาย",
            _ => "อื่นๆ"
        };

        public readonly record struct Result(bool IsFull, decimal RemainQty, decimal RemainWeight);

        public static Result Validate(
            int planStatus, bool rowActive, string? outboundRunning, string? reason, string? mode,
            decimal rowQty, decimal rowWeight, decimal returnQty, decimal returnWeight)
        {
            if (planStatus == StatusCompleted || planStatus == StatusMelted)
            {
                throw new HandleException("ใบงานสำเร็จแล้ว คืนพลอยไม่ได้");
            }
            if (!rowActive || string.IsNullOrEmpty(outboundRunning))
            {
                throw new HandleException("ไม่พบรายการพลอยที่เบิกในใบงานนี้");
            }
            if (mode != ModeReturnNow && mode != ModeLinkExisting)
            {
                throw new HandleException("รูปแบบการคืนไม่ถูกต้อง");
            }
            if (string.IsNullOrEmpty(reason) || !Reasons.Contains(reason))
            {
                throw new HandleException("กรุณาเลือกเหตุผลที่คืนพลอย");
            }
            if (returnQty < 0 || returnWeight < 0)
            {
                throw new HandleException("จำนวนหรือน้ำหนักที่คืนต้องไม่ติดลบ");
            }
            if (returnQty == 0 && returnWeight == 0)
            {
                throw new HandleException("กรุณาระบุจำนวนหรือน้ำหนักที่คืน");
            }
            if (returnQty > rowQty)
            {
                throw new HandleException("จำนวนที่คืนมากกว่าจำนวนที่เบิกในใบงาน");
            }
            if (returnWeight > rowWeight)
            {
                throw new HandleException("น้ำหนักที่คืนมากกว่าน้ำหนักที่เบิกในใบงาน");
            }

            var remainQty = rowQty - returnQty;
            var remainWeight = rowWeight - returnWeight;
            var hasQty = rowQty > 0;
            var hasWeight = rowWeight > 0;
            if (hasQty && hasWeight && (remainQty == 0) != (remainWeight == 0))
            {
                throw new HandleException("จำนวนกับน้ำหนักต้องคืนครบทั้งคู่ หรือคืนบางส่วนทั้งคู่");
            }
            var isFull = (!hasQty || remainQty == 0) && (!hasWeight || remainWeight == 0);
            return new Result(isFull, remainQty, remainWeight);
        }

        public static void CheckLinkRemaining(
            decimal lineQty, decimal lineWeight, decimal usedQty, decimal usedWeight,
            decimal returnQty, decimal returnWeight)
        {
            if (lineQty - usedQty < returnQty || lineWeight - usedWeight < returnWeight)
            {
                throw new HandleException("รายการรับเข้านี้เหลือไม่พอสำหรับผูก");
            }
        }
    }
}
