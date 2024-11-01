using jewelry.Model.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.ProductionPlan
{
    public static class ProductionPlanStatusExtensions
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

        public static bool IsUpdateByWatingStatus(this ProductionPlanStatusEnum status, ProductionPlanStatusEnum targetStatus)
        {
            return (status, targetStatus) switch
            {
                (ProductionPlanStatusEnum.WaitCasting, ProductionPlanStatusEnum.Casting) => true,
                (ProductionPlanStatusEnum.WaitScrub, ProductionPlanStatusEnum.Scrub) => true,
                (ProductionPlanStatusEnum.WaitGems, ProductionPlanStatusEnum.Gems) => true,
                (ProductionPlanStatusEnum.WaitEmbed, ProductionPlanStatusEnum.Embed) => true,
                (ProductionPlanStatusEnum.WaitCVD, ProductionPlanStatusEnum.CVD) => true,
                (ProductionPlanStatusEnum.WaitPlated, ProductionPlanStatusEnum.Plated) => true,
                (ProductionPlanStatusEnum.WaitPrice, ProductionPlanStatusEnum.Price) => true,
                _ => false
            };
        }
    }
}
