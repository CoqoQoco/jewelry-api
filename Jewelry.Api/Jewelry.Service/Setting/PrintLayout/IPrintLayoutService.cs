using jewelry.Model.Setting.PrintLayout;

namespace Jewelry.Service.Setting.PrintLayout;

public interface IPrintLayoutService
{
    Task<PrintLayoutDto?> GetAsync(string key);
    Task SaveAsync(string key, string layoutJson);
}
