namespace WebApplication1.Services
{
    public interface IInputSanitizationService
    {
        string SanitizeInput(string input);
        string SanitizeHtml(string html);
        string AggressiveHtmlEncode(string input);
        string StripHtml(string html);
        bool ContainsPotentialXss(string input);
        bool ContainsPotentialSqlInjection(string input);
    }
}
