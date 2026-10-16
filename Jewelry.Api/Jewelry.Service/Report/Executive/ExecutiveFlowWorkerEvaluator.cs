using Jewelry.Service.Production.Insight;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Report.Executive
{
    // Pure — จัดประเภท "ใบงานที่อยู่กับช่าง" ของแผนก (ไม่แตะ DB)
    // precedence: มีช่าง TEST อย่างน้อย 1 -> Test; มีช่างจริงอย่างน้อย 1 -> Named; มีแต่รหัสคิว "รอ…" -> Queue; ไม่มีรหัสช่างเลย -> None
    public static class ExecutiveFlowWorkerEvaluator
    {
        public enum HolderClass
        {
            Named,
            Queue,
            None,
            Test
        }

        public class PlanHolderResult
        {
            public HolderClass Class { get; set; }
            public List<string> NamedWorkers { get; set; } = new List<string>();
        }

        public static Dictionary<int, PlanHolderResult> Classify(
            IEnumerable<int> planIds,
            IEnumerable<(int PlanId, string? Worker)> holderRows,
            IReadOnlyDictionary<string, string> workerNames)
        {
            var byPlan = holderRows
                .GroupBy(r => r.PlanId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Worker?.Trim()).Where(w => !string.IsNullOrEmpty(w)).Select(w => w!).Distinct().ToList());

            var result = new Dictionary<int, PlanHolderResult>();
            foreach (var planId in planIds)
            {
                byPlan.TryGetValue(planId, out var workers);
                workers ??= new List<string>();

                var item = new PlanHolderResult();
                if (workers.Any(w => ProductionGoldStageEvaluator.IsTestWorker(w, workerNames)))
                {
                    item.Class = HolderClass.Test;
                }
                else
                {
                    item.NamedWorkers = workers.Where(w => !ProductionGoldStageEvaluator.IsQueueOrEmptyWorker(w, workerNames)).ToList();
                    if (item.NamedWorkers.Count > 0) item.Class = HolderClass.Named;
                    else if (workers.Count > 0) item.Class = HolderClass.Queue;
                    else item.Class = HolderClass.None;
                }

                result[planId] = item;
            }

            return result;
        }
    }
}
