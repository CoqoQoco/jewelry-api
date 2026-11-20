using jewelry.Model.ProductionPlan.ProductionPlanStatus;
using Jewelry.Data.Models.Jewelry;

namespace Jewelry.Service.ProductionPlan
{
    public static class ProductionPlanGemMatcher
    {
        public static TbtProductionPlanStatusDetailGem? FindOutboundGem(
            IEnumerable<TbtProductionPlanStatusDetailGem> keptOutboundGems,
            GemItem gem)
        {
            if (string.IsNullOrEmpty(gem.OutboundRunning))
            {
                return null;
            }

            var kept = keptOutboundGems.ToList();
            return kept.FirstOrDefault(x => x.OutboundRunning == gem.OutboundRunning && x.ItemNo == gem.ItemNo)
                ?? kept.FirstOrDefault(x => x.OutboundRunning == gem.OutboundRunning && x.GemCode == gem.Code);
        }

        public static void ApplyOutboundGemUpdate(TbtProductionPlanStatusDetailGem existingGem, GemItem gem)
        {
            existingGem.GemPrice = gem.Price;
            existingGem.GemLength = gem.Length;
            existingGem.GemLengthUnit = gem.LengthUnit;
        }
    }
}
