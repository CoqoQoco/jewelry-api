using Jewelry.Service.AiConnector.Sensitivity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.AiConnector.Registry
{
    // รวบรวม tool ที่ scan มา + validate ตอน startup — ไม่ผ่าน = throw (แอปไม่ขึ้น ดีกว่าปล่อย tool ผิดกติกา)
    public class AiToolRegistry
    {
        private readonly Dictionary<string, IAiTool> _tools;

        public AiToolRegistry(IEnumerable<IAiTool> tools)
        {
            var list = tools.ToList();
            var errors = Validate(list);
            if (errors.Count > 0)
                throw new InvalidOperationException("AI tool registry ไม่ผ่านการตรวจ: " + string.Join("; ", errors));
            _tools = list.ToDictionary(t => t.Name, StringComparer.Ordinal);
        }

        public IReadOnlyCollection<IAiTool> Tools => _tools.Values;

        public IAiTool? Find(string? name) => name != null && _tools.TryGetValue(name, out var t) ? t : null;

        public static List<string> Validate(IReadOnlyCollection<IAiTool> tools)
        {
            var errors = new List<string>();
            foreach (var dup in tools.GroupBy(t => t.Name).Where(g => g.Count() > 1))
                errors.Add($"ชื่อ tool ซ้ำ: {dup.Key}");

            foreach (var t in tools)
            {
                if (string.IsNullOrWhiteSpace(t.Name)) errors.Add($"{t.GetType().Name}: ไม่มีชื่อ");
                if (t.IsWrite) errors.Add($"{t.Name}: tool เขียนข้อมูลไม่ได้รับอนุญาตในรอบนี้ (IsWrite=true)");
                if (t.PermissionsAny == null || t.PermissionsAny.Count == 0) errors.Add($"{t.Name}: ไม่ได้ระบุ permission");
                if (t.RequiredScopes == null || t.RequiredScopes.Count == 0) errors.Add($"{t.Name}: ไม่ได้ระบุ scope");
                if (string.IsNullOrWhiteSpace(t.TitleTh)) errors.Add($"{t.Name}: ไม่มี TitleTh");
                if (string.IsNullOrWhiteSpace(t.WrapsEndpoint)) errors.Add($"{t.Name}: ไม่ได้ระบุ WrapsEndpoint");
                if (t.InputType.GetConstructor(Type.EmptyTypes) == null) errors.Add($"{t.Name}: input type ต้องมี constructor ไม่มี parameter");
                foreach (var e in AiDtoValidator.Validate(t.OutputType)) errors.Add($"{t.Name}: {e}");
            }
            return errors;
        }
    }
}
