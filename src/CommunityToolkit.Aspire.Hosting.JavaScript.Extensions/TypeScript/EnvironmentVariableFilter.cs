using System.Text.RegularExpressions;

namespace CommunityToolkit.Aspire.Hosting.JavaScript;

/// <summary>
/// Filters environment variables before they are passed to a
/// TypeScript declaration generator.
/// </summary>
internal static partial class EnvironmentVariableFilter
{
    // Aspire-internal prefixes that are not relevant to application code
    private static readonly string[] DefaultExcludePrefixes =
    [
        "OTEL_",
        "DOTNET_",
        "ASPNETCORE_",
        "ASPIRE_",
        "LOGGING__",
        "services__",
        "ConnectionStrings__",
    ];

    /// <summary>
    /// Returns a filtered, sorted list of environment variable names
    /// suitable for TypeScript declarations.
    /// </summary>
    /// <param name="environmentVariables">Raw variable names from the resource.</param>
    /// <param name="additionalExcludePrefixes">
    /// Optional caller-supplied prefixes to exclude in addition to
    /// the built-in defaults.
    /// </param>
    internal static IReadOnlyList<string> Apply(
        IEnumerable<string> environmentVariables,
        string[]? additionalExcludePrefixes = null)
    {
        string[] excludePrefixes = additionalExcludePrefixes is { Length: > 0 }
            ? [.. DefaultExcludePrefixes, .. additionalExcludePrefixes]
            : DefaultExcludePrefixes;

        return [.. environmentVariables
            .Where(name => !excludePrefixes.Any(prefix =>
                name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Where(IsValidTypeScriptIdentifier)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="name"/> is a
    /// valid TypeScript (and JavaScript) identifier.
    /// </summary>
    internal static bool IsValidTypeScriptIdentifier(string name) =>
        !string.IsNullOrEmpty(name) && ValidIdentifierRegex().IsMatch(name);

    [GeneratedRegex(@"^[A-Za-z_$][A-Za-z0-9_$]*$")]
    private static partial Regex ValidIdentifierRegex();
}
