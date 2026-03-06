using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Typewriter.CodeModel;

namespace Typewriter.Extensions.WebApi
{
    /// <summary>
    /// Extension methods for extracting Web API routes and URLs.
    /// </summary>
    public static class UrlExtensions
    {
        internal const string DefaultRoute = "api/{controller}/{id?}";

        /// <summary>
        /// Returns the URL for the Web API action based on route attributes.
        /// </summary>
        public static string Url(this Method method)
        {
            return Url(method, DefaultRoute);
        }

        /// <summary>
        /// Returns the URL for the Web API action based on route attributes or convention route.
        /// </summary>
        public static string Url(this Method method, string route)
        {
            route = Route(method, route);
            route = RemoveUnmatchedOptionalParameters(method, route);
            route = ReplaceSpecialParameters(method, route);
            route = ConvertRouteParameters(method, route);
            route = AppendQueryString(method, route);

            return route;
        }

        /// <summary>
        /// Returns the route for the Web API action based on route attributes.
        /// </summary>
        public static string Route(this Method method)
        {
            return Route(method, DefaultRoute);
        }

        /// <summary>
        /// Returns the route for the Web API action based on route attributes or convention route.
        /// </summary>
        public static string Route(this Method method, string route)
        {
            var routeAttribute = method.Attributes.FirstOrDefault(a => a.Name.Equals(nameof(Route), StringComparison.OrdinalIgnoreCase))
                                 ?? method.Attributes.FirstOrDefault(a => a.Name.StartsWith("Http", StringComparison.OrdinalIgnoreCase));

            var routePrefix = GetRoutePrefix(method.Parent as Class);

            if (routeAttribute != null)
            {
                var value = ParseAttributeValue(routeAttribute.Value);

                if (string.IsNullOrEmpty(value))
                {
                    route = routePrefix ?? route;
                }
                else if (value.StartsWith("~/", StringComparison.OrdinalIgnoreCase))
                {
                    route = value.Remove(0, 2);
                }
                else if (routePrefix == null)
                {
                    route = value;
                }
                else
                {
                    route = string.Concat(routePrefix, "/", value);
                }
            }
            else if (routePrefix != null)
            {
                route = string.Concat(routePrefix, "/", route);
            }

            return route;
        }

        internal static string GetParameterValue(Method method, string name)
        {
            var parameter = method.Parameters.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (parameter != null)
            {
                if (parameter.Type.Name.Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    return $"encodeURIComponent({name})";
                }

                if (parameter.Type.Name.Equals("string | null", StringComparison.OrdinalIgnoreCase))
                {
                    return $"encodeURIComponent({name})";
                }

                if (parameter.Type.IsDate)
                {
                    return $"encodeURIComponent(String({name}))";
                }
            }

            return name;
        }

        private static string? GetRoutePrefix(Class? @class)
        {
            if (@class == null)
            {
                return null;
            }

            var routePrefix = @class.Attributes
                .FirstOrDefault(a => a.Name.Equals("RoutePrefix", StringComparison.OrdinalIgnoreCase))?.Value
                ?.TrimEnd('/');

            if (string.IsNullOrEmpty(routePrefix))
            {
                routePrefix = @class.Attributes
                    .FirstOrDefault(a => a.Name.Equals(nameof(Route), StringComparison.OrdinalIgnoreCase))?.Value
                    ?.TrimEnd('/');
            }

            if (string.IsNullOrEmpty(routePrefix) && @class.BaseClass != null)
            {
                routePrefix = GetRoutePrefix(@class.BaseClass);
            }

            return routePrefix;
        }

        private static string ParseAttributeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (!value.StartsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            var expression = new Regex("""(?<=")(?:\\.|[^"\\])*(?=")""", RegexOptions.None, TimeSpan.FromSeconds(5));
            return expression.Match(value).Value;
        }

        private static string RemoveUnmatchedOptionalParameters(Method method, string route)
        {
#pragma warning disable MA0026
            // TODO: Handle {parameter:regex(...)?} containing ? and/or }
#pragma warning restore MA0026
#pragma warning disable MA0023 // Add RegexOptions.ExplicitCapture
            var parameters = Regex.Matches(route, @"\{(\w+):*\w*\?\}", RegexOptions.None, TimeSpan.FromSeconds(5))
                .Cast<Match>().Select(m => m.Groups[1].Value);
#pragma warning restore MA0023 // Add RegexOptions.ExplicitCapture
            var unmatchedParameters = parameters.Where(o => !method.Parameters.Any(p => p.Name.Equals(o, StringComparison.OrdinalIgnoreCase))).ToList();

            foreach (var parameter in unmatchedParameters)
            {
                route = Regex.Replace(route, $@"\{{{parameter}:*\w*\?\}}", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(5));
            }

            return route;
        }

        private static string ReplaceSpecialParameters(Method method, string route)
        {
            if ((route.Contains("{controller}", StringComparison.Ordinal) || route.Contains("[controller]", StringComparison.Ordinal))
                && !method.Parameters.Any(p => p.name.Equals("controller", StringComparison.OrdinalIgnoreCase))
                && method.Parent is Class parent)
            {
                var controller = parent.Name;
                if (controller.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                {
                    controller = controller[..^10];
                }

                route = route.Replace("{controller}", controller, StringComparison.Ordinal)
                    .Replace("[controller]", controller, StringComparison.Ordinal);
            }

            if ((route.Contains("{action}", StringComparison.Ordinal) || route.Contains("[action]", StringComparison.Ordinal))
                && !method.Parameters.Any(p => p.name.Equals("action", StringComparison.OrdinalIgnoreCase)))
            {
                var action =
                    method.Attributes.FirstOrDefault(a => a.Name.Equals("ActionName", StringComparison.OrdinalIgnoreCase))?.Value
                    ?? method.name;

                route = route.Replace("{action}", action, StringComparison.Ordinal)
                    .Replace("[action]", action, StringComparison.Ordinal);
            }

            return route;
        }

        private static string ConvertRouteParameters(Method method, string route)
        {
#pragma warning disable MA0023 // Add RegexOptions.ExplicitCapture
            return Regex.Replace(
                route,
                @"\{\*?(\w+):?\w*\??\}",
                m => $"${{{GetParameterValue(method, m.Groups[1].Value)}}}",
                RegexOptions.None,
                TimeSpan.FromSeconds(5));
#pragma warning restore MA0023 // Add RegexOptions.ExplicitCapture
        }

        private static string AppendQueryString(Method method, string route)
        {
#pragma warning disable MA0026
            // TODO: Add support for FromUri attribute
#pragma warning restore MA0026
            var prefix = route.Contains("?", StringComparison.Ordinal) ? "&" : "?";
            var sb = new StringBuilder(route);
            foreach (var parameterName in method.Parameters
                         .Where(p => p.Type.IsPrimitive && !p.Attributes.Any(a => a.Name.Equals("FromBody", StringComparison.OrdinalIgnoreCase)))
                         .Select(p => p.Name))
            {
                if (route.Contains($"${{{GetParameterValue(method, parameterName)}}}", StringComparison.Ordinal))
                {
                    continue;
                }

                sb.Append(prefix).Append(parameterName).Append("=${").Append(GetParameterValue(method, parameterName)).Append('}');
                prefix = "&";
            }

            return sb.ToString();
        }
    }
}
