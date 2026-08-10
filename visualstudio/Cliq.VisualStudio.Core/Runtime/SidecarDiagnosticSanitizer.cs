using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Cliq.VisualStudio.Core.Runtime
{
    public static class SidecarDiagnosticSanitizer
    {
        private static readonly Regex BearerToken = new Regex(
            @"(?i)\bBearer\s+[A-Za-z0-9._~+/\-=]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string Sanitize(string message, IEnumerable<string>? secrets = null)
        {
            var sanitized = BearerToken.Replace(message ?? string.Empty, "Bearer [REDACTED]");
            if (secrets == null)
            {
                return sanitized;
            }

            foreach (var secret in secrets)
            {
                if (!string.IsNullOrEmpty(secret))
                {
                    sanitized = sanitized.Replace(secret, "[REDACTED]");
                }
            }

            return sanitized;
        }
    }
}
