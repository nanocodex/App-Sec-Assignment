using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace WebApplication1.Services
{
    public class InputSanitizationService : IInputSanitizationService
    {
        private readonly HtmlEncoder _htmlEncoder;

        public InputSanitizationService(HtmlEncoder htmlEncoder)
        {
            _htmlEncoder = htmlEncoder;
        }

        public string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Trim whitespace
            var sanitized = input.Trim();

            // Remove null characters
            sanitized = sanitized.Replace("\0", string.Empty);

            // Normalize line endings
            sanitized = sanitized.Replace("\r\n", "\n").Replace("\r", "\n");

            // Remove control characters except newline, carriage return, and tab
            sanitized = Regex.Replace(sanitized, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);

            return sanitized;
        }

        public string SanitizeHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            // HTML encode the input to prevent XSS
            return _htmlEncoder.Encode(html);
        }

        public string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            // Remove all HTML tags
            var withoutTags = Regex.Replace(html, @"<[^>]*>", string.Empty);

            // Decode HTML entities
            withoutTags = System.Net.WebUtility.HtmlDecode(withoutTags);

            // Remove script tags and their content
            withoutTags = Regex.Replace(withoutTags, @"<script[^>]*>.*?</script>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Remove style tags and their content
            withoutTags = Regex.Replace(withoutTags, @"<style[^>]*>.*?</style>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return SanitizeInput(withoutTags);
        }

        public bool ContainsPotentialXss(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var xssPatterns = new[]
            {
                @"<script[\s\S]*?>[\s\S]*?</script>",
                @"javascript:",
                @"onerror\s*=",
                @"onload\s*=",
                @"onclick\s*=",
                @"onmouseover\s*=",
                @"<iframe[\s\S]*?>",
                @"<embed[\s\S]*?>",
                @"<object[\s\S]*?>",
                @"eval\s*\(",
                @"expression\s*\(",
                @"vbscript:",
                @"data:text/html"
            };

            return xssPatterns.Any(pattern => 
                Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        public bool ContainsPotentialSqlInjection(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Note: This is a basic check. Entity Framework parameterized queries are the primary defense.
            var sqlInjectionPatterns = new[]
            {
                @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|UNION|DECLARE)\b)",
                @"(--|;|\/\*|\*\/|xp_|sp_)",
                @"('|\"")\s*(OR|AND)\s*('|\"")?\s*\d+\s*=\s*\d+",
                @"'\s*OR\s*'1'\s*=\s*'1",
                @"--\s*$",
                @";\s*(DROP|DELETE|INSERT|UPDATE)"
            };

            return sqlInjectionPatterns.Any(pattern => 
                Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }
    }
}
