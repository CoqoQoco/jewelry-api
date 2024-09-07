using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Constant
{
    public static class ProductionPlanStatus
    {
        public static int Designed = 10;
        public static int Casting = 50; // การเเต่ง
        public static int Polishing = 60; // การขัด
        public static int GemPick = 70; // คัดพลอย
        public static int Embed = 80; // ฝัง
        public static int Plating = 90;
        public static int Price = 95;
        public static int Completed = 100;
    }
}
