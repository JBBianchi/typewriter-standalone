using System;
using System.Linq;
using Typewriter.CodeModel;

namespace Typewriter.Extensions.WebApi
{
    /// <summary>
    /// Extension methods for extracting Web API request body parameters.
    /// </summary>
    public static class RequestDataExtensions
    {
        /// <summary>
        /// Creates an object literal containing request-body parameters for a Web API request.
        /// </summary>
        public static string RequestData(this Method method)
        {
            return RequestData(method, UrlExtensions.DefaultRoute);
        }

        /// <summary>
        /// Creates an object literal containing request-body parameters for a Web API request.
        /// </summary>
        public static string RequestData(this Method method, string route)
        {
            var url = method.Url(route);

            var dataParameters = method.Parameters
                .Where(x => !x.Type.Name.Equals("CancellationToken", StringComparison.OrdinalIgnoreCase))
                .Where(p => !url.Contains($"${{{UrlExtensions.GetParameterValue(method, p.Name)}}}", StringComparison.Ordinal))
                .ToList();

            if (dataParameters.Count == 1)
            {
                return dataParameters[0].Name;
            }

            if (dataParameters.Count > 1)
            {
                return $"{{ {string.Join(", ", dataParameters.Select(p => $"{p.Name}: {p.Name}"))} }}";
            }

            return "null";
        }
    }
}
