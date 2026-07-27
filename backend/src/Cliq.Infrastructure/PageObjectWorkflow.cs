namespace Cliq.Infrastructure.Generation;

public static class PageObjectWorkflow
{
    public static string DeriveClassName(string url)
    {
        var uri = new Uri(url);
        var lastSegment = uri.Segments.LastOrDefault(segment => segment != "/")?.Trim('/');
        var basis = string.IsNullOrEmpty(lastSegment) ? uri.Host : $"{uri.Host}_{lastSegment}";
        var sanitized = IdentifierNaming.Sanitize(basis, "generated");
        return IdentifierNaming.ToPascalCase(sanitized) + "Page";
    }
}
