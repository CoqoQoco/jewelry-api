using jewelry.Model.AiConnector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jewelry.Service.AiConnector.Registry
{
    // ตรวจ AI DTO: ทุก property ต้องติดป้าย + ห้าม object/dynamic/Dictionary + sensitive ตัวเลขต้อง nullable
    public static class AiDtoValidator
    {
        public static List<string> Validate(Type root)
        {
            var errors = new List<string>();
            Visit(root, new HashSet<Type>(), errors);
            return errors;
        }

        private static void Visit(Type type, HashSet<Type> seen, List<string> errors)
        {
            if (!seen.Add(type)) return;
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var where = $"{type.Name}.{p.Name}";
                var sens = p.GetCustomAttribute<AiSensitiveAttribute>();
                var notSens = p.GetCustomAttribute<AiNotSensitiveAttribute>();
                if (sens == null && notSens == null) errors.Add($"{where}: ไม่มี [AiSensitive]/[AiNotSensitive]");
                if (sens != null && notSens != null) errors.Add($"{where}: ติดป้ายทั้ง sensitive และ not-sensitive");

                if (sens != null && (p.SetMethod == null || !p.SetMethod.IsPublic))
                    errors.Add($"{where}: property sensitive ต้องมี public setter (mask ต้องแก้ค่าบนสำเนาได้)");

                var t = p.PropertyType;
                if (t == typeof(object) || typeof(IDictionary).IsAssignableFrom(t) || IsGenericDictionary(t))
                {
                    errors.Add($"{where}: ห้ามใช้ object/Dictionary ใน AI DTO");
                    continue;
                }

                var elem = ElementType(t);
                var check = elem ?? Nullable.GetUnderlyingType(t) ?? t;
                if (check == typeof(object)) { errors.Add($"{where}: ห้ามใช้ object"); continue; }

                if (sens != null && elem == null && t.IsValueType && Nullable.GetUnderlyingType(t) == null)
                    errors.Add($"{where}: property sensitive ที่เป็นตัวเลขต้องเป็น nullable");

                if (IsSimple(check)) continue;
                if (check.IsClass) Visit(check, seen, errors);
                else errors.Add($"{where}: ชนิด {check.Name} ไม่รองรับใน AI DTO");
            }
        }

        private static bool IsGenericDictionary(Type t) =>
            t.IsGenericType && t.GetInterfaces().Concat(new[] { t }).Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        private static Type? ElementType(Type t)
        {
            if (t == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(t)) return null;
            if (t.IsGenericType) return t.GetGenericArguments()[0];
            return typeof(object);
        }

        private static bool IsSimple(Type t) =>
            t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(Guid);
    }
}
