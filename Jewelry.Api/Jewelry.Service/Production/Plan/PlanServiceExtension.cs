using jewelry.Model.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Plan
{
    public static class PlanServiceExtension
    {
        public static int GetNextStatus(this ProductionPlanStatusEnum currentStatus)
        {
            return currentStatus switch
            {
                ProductionPlanStatusEnum.WaitCasting => (int)ProductionPlanStatusEnum.Casting,
                ProductionPlanStatusEnum.WaitScrub => (int)ProductionPlanStatusEnum.Scrub,
                ProductionPlanStatusEnum.WaitGems => (int)ProductionPlanStatusEnum.Gems,
                ProductionPlanStatusEnum.WaitEmbed => (int)ProductionPlanStatusEnum.Embed,
                ProductionPlanStatusEnum.WaitPlated => (int)ProductionPlanStatusEnum.Plated,
                ProductionPlanStatusEnum.WaitPrice => (int)ProductionPlanStatusEnum.Price,
                _ => (int)currentStatus
            };
        }

        public static int GetWatingStatus(this ProductionPlanStatusEnum currentStatus)
        {
            return currentStatus switch
            {
                ProductionPlanStatusEnum.Casting => (int)ProductionPlanStatusEnum.WaitCasting,
                ProductionPlanStatusEnum.Scrub => (int)ProductionPlanStatusEnum.WaitScrub,
                ProductionPlanStatusEnum.Gems => (int)ProductionPlanStatusEnum.WaitGems,
                ProductionPlanStatusEnum.Embed => (int)ProductionPlanStatusEnum.WaitEmbed,
                ProductionPlanStatusEnum.Plated => (int)ProductionPlanStatusEnum.WaitPlated,
                ProductionPlanStatusEnum.Price => (int)ProductionPlanStatusEnum.WaitPrice,
                _ => (int)currentStatus
            };
        }

        public static bool IsWaitingStatus(this ProductionPlanStatusEnum status)
        {
            return status switch
            {
                ProductionPlanStatusEnum.WaitCasting or
                ProductionPlanStatusEnum.WaitScrub or
                ProductionPlanStatusEnum.WaitGems or
                ProductionPlanStatusEnum.WaitEmbed or
                ProductionPlanStatusEnum.WaitPlated or
                ProductionPlanStatusEnum.WaitPrice => true,
                _ => false
            };
        }
    }
}
