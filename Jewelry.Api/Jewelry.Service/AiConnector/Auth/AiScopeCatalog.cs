using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Auth
{
    public interface IAiScopeCatalog
    {
        Task<List<TbmAiScope>> Active();
    }

    public class AiScopeCatalog : IAiScopeCatalog
    {
        private readonly JewelryContext _context;

        public AiScopeCatalog(JewelryContext context)
        {
            _context = context;
        }

        public Task<List<TbmAiScope>> Active() =>
            _context.TbmAiScope.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Sort).ToListAsync();
    }
}
