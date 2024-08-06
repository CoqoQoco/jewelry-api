using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold.PlanGems
{
    public class PlanGemsRequest
    {
        public int Id { get; set; }
        public List<ModelGem> Gems { get; set; }
    }
}
