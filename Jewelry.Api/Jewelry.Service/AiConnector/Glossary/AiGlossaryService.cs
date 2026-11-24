using jewelry.Model.AiConnector;
using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Glossary
{
    public interface IAiGlossaryService
    {
        Task<List<AiGlossaryItem>> Get(string? module, string? metric);
        Task<List<AiGlossaryItem>> GetByCodes(IReadOnlyCollection<string> metricCodes);
    }

    public class AiGlossaryService : IAiGlossaryService
    {
        private readonly JewelryContext _context;

        public AiGlossaryService(JewelryContext context)
        {
            _context = context;
        }

        public async Task<List<AiGlossaryItem>> Get(string? module, string? metric)
        {
            var q = _context.TbmAiGlossary.AsNoTracking().Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(module)) q = q.Where(x => x.Module == module);
            if (!string.IsNullOrWhiteSpace(metric)) q = q.Where(x => x.MetricCode == metric);
            return await Project(q);
        }

        public async Task<List<AiGlossaryItem>> GetByCodes(IReadOnlyCollection<string> metricCodes)
        {
            if (metricCodes.Count == 0) return new List<AiGlossaryItem>();
            var codes = metricCodes.ToList();
            return await Project(_context.TbmAiGlossary.AsNoTracking().Where(x => x.IsActive && codes.Contains(x.MetricCode)));
        }

        private static Task<List<AiGlossaryItem>> Project(IQueryable<Jewelry.Data.Models.Jewelry.TbmAiGlossary> q) =>
            q.OrderBy(x => x.Module).ThenBy(x => x.MetricCode)
                .Select(x => new AiGlossaryItem
                {
                    MetricCode = x.MetricCode,
                    Module = x.Module,
                    NameTh = x.NameTh,
                    DefinitionTh = x.DefinitionTh,
                    CaveatTh = x.CaveatTh,
                })
                .ToListAsync();
    }
}
