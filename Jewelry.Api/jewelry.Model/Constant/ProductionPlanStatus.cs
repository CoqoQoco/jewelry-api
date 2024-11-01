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

        public static int WaitCasting = 49; 
        public static int Casting = 50;


        public static int WatingScrubb = 59;
        public static int Scrubb = 60;

        public static int WaitGems = 69;
        public static int Gems = 70;

        public static int WaitEmbedd =79;
        public static int Embedd = 80;

        public static int WaitCVD = 84;
        public static int CVD = 85;

        public static int WaitPlated = 89;
        public static int Plated = 90;

        public static int WaitPrice = 94;
        public static int Price = 95;

        public static int Completed = 100;
        public static int Melted = 500;
    }

    public enum ProductionPlanStatusEnum
    {
        Designed = 10,

        WaitCasting = 49,
        Casting = 50,


        WaitScrub = 59,
        Scrub = 60,

        WaitGems = 69,
        Gems = 70,

        WaitEmbed = 79,
        Embed = 80,

        WaitCVD = 84,
        CVD = 85,

        WaitPlated = 89,
        Plated = 90,

        WaitPrice = 94,
        Price = 95,

        Completed = 100,
        Melted = 500
    }
}
