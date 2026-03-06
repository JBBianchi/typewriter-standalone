using System;
using System.Linq;
using Typewriter.CodeModel;

namespace Typewriter.Extensions.WebApi
{
    /// <summary>
    /// Extension methods for extracting Web API HTTP methods.
    /// </summary>
    public static class HttpMethodExtensions
    {
        private static readonly string[] ValidVerbs = ["get", "post", "put", "delete", "patch", "head", "options"];

        /// <summary>
        /// Returns the HTTP method for a Web API action based on attributes or naming convention.
        /// </summary>
        public static string HttpMethod(this Method method)
        {
            var httpAttributes = method.Attributes.Where(a => a.Name.StartsWith("Http", StringComparison.OrdinalIgnoreCase));
            var acceptAttribute = method.Attributes.FirstOrDefault(a => a.Name.Equals("AcceptVerbs", StringComparison.OrdinalIgnoreCase));

            var verbs = httpAttributes.Select(a => a.Name.Remove(0, 4).ToLowerInvariant()).ToList();
            if (acceptAttribute != null)
            {
                verbs.AddRange(acceptAttribute.Value.Split(',').Select(v => v.Trim().Trim('"').ToLowerInvariant()));
            }

            if (verbs.Contains("post", StringComparer.OrdinalIgnoreCase))
            {
                return "post";
            }

            if (verbs.Count > 0)
            {
                return verbs[0];
            }

            var methodName = method.Name.ToLowerInvariant();
            return Array.Find(ValidVerbs, v => methodName.StartsWith(v, StringComparison.OrdinalIgnoreCase)) ?? "post";
        }
    }
}
