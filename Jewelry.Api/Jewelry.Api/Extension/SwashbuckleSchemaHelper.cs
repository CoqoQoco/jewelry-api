using System.Reflection;

namespace Jewelry.Api.Extension
{
    public static class SwashbuckleSchemaHelper
    {
        private static readonly string _rootNamespace;
        private static readonly string _dtoFolder = "Dtos";

        static SwashbuckleSchemaHelper()
        {
            _rootNamespace = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().ManifestModule.Name);
        }

        private static string GetRelativeNamespace(string typeNamespace)
        {
            if (!typeNamespace.StartsWith(_rootNamespace))
            {
                return typeNamespace;
            }

            var relativenamespace = typeNamespace.Substring(_rootNamespace.Length + _dtoFolder.Length + 1).TrimStart('.');
            if (string.IsNullOrEmpty(relativenamespace))
            {
                return string.Empty;
            }

            return $"{relativenamespace}.";
        }

        public static string GetSchemaId(Type type)
        {
            var schemaBase = $"{GetRelativeNamespace(type.Namespace)}{type.Name}";

            if (type.IsGenericType)
            {
                string? schemaGeneric;
                if (type.GenericTypeArguments.Length > 0)
                {
                    var firstItem = type.GenericTypeArguments.First();
                    schemaGeneric = $"<{GetRelativeNamespace(firstItem.Namespace)}{firstItem.Name}>";
                }
                else
                {
                    schemaGeneric = $"<{Guid.NewGuid()}>";
                }

                return $"{schemaBase}{schemaGeneric}";
            }

            return $"{schemaBase}";
        }
    }
}
