using System;
using System.Collections.Generic;
using System.Linq;
using Typewriter.Metadata;

namespace Typewriter.CodeModel
{
    public static class Helpers
    {
        private static readonly Dictionary<string, string> _primitiveTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "System.Boolean", "bool" },
            { "System.Byte", "byte" },
            { "System.Char", "char" },
            { "System.Decimal", "decimal" },
            { "System.Double", "double" },
            { "System.Int16", "short" },
            { "System.Int32", "int" },
            { "System.Int64", "long" },
            { "System.SByte", "sbyte" },
            { "System.Single", "float" },
            { "System.String", "string" },
            { "System.UInt32", "uint" },
            { "System.UInt16", "ushort" },
            { "System.UInt64", "ulong" },
            { "System.DateTime", "DateTime" },
            { "System.DateTimeOffset", "DateTimeOffset" },
            { "System.Guid", "Guid" },
            { "System.TimeSpan", "TimeSpan" },
        };

        public static string CamelCase(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            if (!char.IsUpper(s[0]))
            {
                return s;
            }

            var chars = s.ToCharArray();

            for (var i = 0; i < chars.Length; i++)
            {
                if (i == 1 && !char.IsUpper(chars[i]))
                {
                    break;
                }

                var hasNext = (i + 1) < chars.Length;
                if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                {
                    break;
                }

                chars[i] = char.ToLowerInvariant(chars[i]);
            }

            return new string(chars);
        }

        public static string GetTypeScriptName(ITypeMetadata metadata, Settings settings)
        {
            if (metadata == null)
            {
                return "any";
            }

            if (metadata.IsDictionary)
            {
                var typeArguments = metadata.TypeArguments.ToList();
                var key = GetTypeScriptName(typeArguments[0], settings);
                var value = GetTypeScriptName(typeArguments[1], settings);

                return metadata.IsNullable && settings.StrictNullGeneration
                    ? $"Record<{key}, {value}> | null"
                    : $"Record<{key}, {value}>";
            }

            if (metadata.IsDynamic)
            {
                return "any";
            }

            if (metadata.IsEnumerable)
            {
                var typeArguments = metadata.TypeArguments.ToList();

                if (typeArguments.Count == 0)
                {
                    if (metadata.BaseClass != null && metadata.BaseClass.IsGeneric)
                    {
                        typeArguments = metadata.BaseClass.TypeArguments.ToList();
                    }
                    else
                    {
                        var genericInterface = metadata.Interfaces.FirstOrDefault(i => i.IsGeneric);
                        if (genericInterface != null)
                        {
                            typeArguments = genericInterface.TypeArguments.ToList();
                        }
                    }

                    if (typeArguments.Exists(t => string.Equals(t.FullName, metadata.FullName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return "any[]";
                    }
                }

                if (typeArguments.Count == 1)
                {
                    var typeName = GetTypeScriptName(typeArguments[0], settings);
                    if (typeName.Contains("|"))
                    {
                        typeName = $"({typeName})";
                    }

                    return metadata.IsNullable && settings.StrictNullGeneration
                        ? $"{typeName}[] | null"
                        : $"{typeName}[]";
                }

                if (typeArguments.Count == 2)
                {
                    var key = GetTypeScriptName(typeArguments[0], settings);
                    var value = GetTypeScriptName(typeArguments[1], settings);

                    return metadata.IsNullable && settings.StrictNullGeneration
                        ? $"Record<{key}, {value}> | null"
                        : $"Record<{key}, {value}>";
                }

                return "any[]";
            }

            if (metadata.IsValueTuple)
            {
                var types = string.Join(", ", metadata.TupleElements.Select(p => $"{p.Name}: {GetTypeScriptName(p.Type, settings)}"));
                return $"{{ {types} }}";
            }

            if (metadata.IsGeneric)
            {
                return string.Concat(
                    metadata.Name,
                    "<",
                    string.Join(", ", metadata.TypeArguments.Select(m => GetTypeScriptName(m, settings))),
                    ">");
            }

            return ExtractTypeScriptName(metadata, settings);
        }

        public static string GetOriginalName(ITypeMetadata metadata)
        {
            var name = metadata.Name;
            var fullName = metadata.IsNullable ? metadata.FullName.TrimEnd('?') : metadata.FullName;

            if (_primitiveTypes.TryGetValue(fullName, out var type))
            {
                name = string.Concat(type, metadata.IsNullable ? "?" : string.Empty);
            }

            return name;
        }

        public static bool IsPrimitive(ITypeMetadata metadata)
        {
            var fullName = metadata.FullName;

            if (metadata.IsNullable)
            {
                fullName = fullName.TrimEnd('?');
            }
            else if (metadata.IsEnumerable)
            {
                var innerType = metadata.TypeArguments.FirstOrDefault();
                if (innerType != null)
                {
                    fullName = innerType.IsNullable ? innerType.FullName.TrimEnd('?') : innerType.FullName;
                }
                else
                {
                    return false;
                }
            }

            return metadata.IsEnum || _primitiveTypes.ContainsKey(fullName);
        }

        private static string ExtractTypeScriptName(ITypeMetadata metadata, Settings settings)
        {
            var fullName = metadata.IsNullable ? metadata.FullName.TrimEnd('?') : metadata.FullName;

            switch (fullName)
            {
                case "System.Boolean":
                    return metadata.IsNullable && settings.StrictNullGeneration ? "boolean | null" : "boolean";
                case "System.String":
                case "System.Char":
                case "System.Guid":
                case "System.TimeSpan":
                    return metadata.IsNullable && settings.StrictNullGeneration ? "string | null" : "string";
                case "System.Byte":
                case "System.SByte":
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.UInt16":
                case "System.UInt32":
                case "System.UInt64":
                case "System.Single":
                case "System.Double":
                case "System.Decimal":
                    return metadata.IsNullable && settings.StrictNullGeneration ? "number | null" : "number";
                case "System.DateTime":
                case "System.DateTimeOffset":
                    return metadata.IsNullable && settings.StrictNullGeneration ? "Date | null" : "Date";
                case "System.Void":
                    return "void";
                case "System.Object":
                case "dynamic":
                    return "any";
            }

            return metadata.IsNullable ?
                settings.StrictNullGeneration
                    ? $"{metadata.Name.TrimEnd('?')} | null"
                    : $"{metadata.Name.TrimEnd('?')}"
                : metadata.Name;
        }
    }
}
