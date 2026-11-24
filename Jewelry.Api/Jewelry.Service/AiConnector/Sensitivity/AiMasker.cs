using jewelry.Model.AiConnector;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jewelry.Service.AiConnector.Sensitivity
{
    public sealed class MaskResult<T>
    {
        public T Value { get; set; } = default!;
        public int MaskedCount { get; set; }
        public List<string> Notes { get; set; } = new List<string>();
    }

    // mask ทำบน "สำเนา" (deep clone) เสมอ — ห้ามแก้ object ต้นทางที่อาจมาจาก InsightCache
    public static class AiMasker
    {
        public static MaskResult<T> Mask<T>(T source, Func<SensitiveCategory, bool> isAllowed) where T : class
        {
            var clone = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source))!;
            var state = new State(isAllowed);
            Walk(clone, state);

            var result = new MaskResult<T> { Value = clone, MaskedCount = state.Total };
            foreach (var kv in state.CountByCategory.OrderBy(k => k.Key))
            {
                var rule = SensitiveCategoryPolicy.For(kv.Key);
                result.Notes.Add($"ซ่อนข้อมูลหมวด \"{rule.NameTh}\" จำนวน {kv.Value} ค่า เพราะยังไม่ได้รับอนุญาต (ต้องมี scope {rule.Scope} และสิทธิ์ {rule.Permission}) — ค่าที่ซ่อนเป็น null หรือรหัสแทน อย่าเดาค่าจริง");
            }
            return result;
        }

        private sealed class State
        {
            public State(Func<SensitiveCategory, bool> isAllowed) { IsAllowed = isAllowed; }
            public Func<SensitiveCategory, bool> IsAllowed { get; }
            public int Total { get; set; }
            public Dictionary<SensitiveCategory, int> CountByCategory { get; } = new Dictionary<SensitiveCategory, int>();
            public Dictionary<(SensitiveCategory, string), string> Aliases { get; } = new Dictionary<(SensitiveCategory, string), string>();
            public Dictionary<SensitiveCategory, int> AliasSeq { get; } = new Dictionary<SensitiveCategory, int>();

            public void Count(SensitiveCategory c, int n = 1)
            {
                Total += n;
                CountByCategory[c] = CountByCategory.TryGetValue(c, out var v) ? v + n : n;
            }

            public string Alias(SensitiveCategory c, string original)
            {
                if (Aliases.TryGetValue((c, original), out var a)) return a;
                var seq = (AliasSeq.TryGetValue(c, out var s) ? s : 0) + 1;
                AliasSeq[c] = seq;
                var prefix = c == SensitiveCategory.CustomerName ? "ลูกค้า" : c == SensitiveCategory.WorkerName ? "ช่าง" : "ข้อมูล";
                return Aliases[(c, original)] = $"{prefix} #{seq}";
            }
        }

        private static void Walk(object? obj, State state)
        {
            if (obj == null) return;
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite || prop.GetIndexParameters().Length > 0) continue;
                var type = prop.PropertyType;
                var sens = prop.GetCustomAttribute<AiSensitiveAttribute>();

                if (sens != null && !state.IsAllowed(sens.Category))
                {
                    Hide(obj, prop, sens.Category, state);
                    continue;
                }

                var value = prop.GetValue(obj);
                if (value == null || type == typeof(string) || type.IsPrimitive || type.IsEnum || type == typeof(decimal)) continue;
                if (value is IEnumerable list)
                {
                    foreach (var item in list)
                    {
                        if (item != null && IsDto(item.GetType())) Walk(item, state);
                    }
                }
                else if (IsDto(type))
                {
                    Walk(value, state);
                }
            }
        }

        private static bool IsDto(Type t) => t.IsClass && t != typeof(string);

        private static void Hide(object obj, PropertyInfo prop, SensitiveCategory cat, State state)
        {
            var type = prop.PropertyType;
            var current = prop.GetValue(obj);
            if (current == null) return;

            if (type == typeof(string))
            {
                var s = (string)current;
                if (s.Length == 0) return;
                prop.SetValue(obj, state.Alias(cat, s));
                state.Count(cat);
            }
            else if (current is IList list)
            {
                var n = list.Count;
                if (n == 0) return;
                list.Clear();
                state.Count(cat, n);
            }
            else if (Nullable.GetUnderlyingType(type) != null)
            {
                prop.SetValue(obj, null);
                state.Count(cat);
            }
            else if (type.IsValueType)
            {
                prop.SetValue(obj, Activator.CreateInstance(type));
                state.Count(cat);
            }
            else
            {
                prop.SetValue(obj, null);
                state.Count(cat);
            }
        }
    }
}
