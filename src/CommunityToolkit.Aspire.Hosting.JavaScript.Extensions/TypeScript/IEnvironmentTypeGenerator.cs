namespace CommunityToolkit.Aspire.Hosting.JavaScript;

/// <summary>
/// Strategy interface for generating TypeScript declaration files
/// from Aspire environment variables.
/// </summary>
public interface IEnvironmentTypeGenerator
{
    /// <summary>
    /// Gets the file path, relative to the resource working directory,
    /// where the declaration file will be written.
    /// </summary>
    string OutputPath { get; }

    /// <summary>
    /// Generates a TypeScript declaration file (.d.ts) for the given
    /// environment variables.
    /// </summary>
    /// <param name="workingDirectory">
    /// Absolute path to the JavaScript/TypeScript project root.
    /// </param>
    /// <param name="environmentVariables">
    /// Filtered environment variable names to declare.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task GenerateAsync(
        string workingDirectory,
        IReadOnlyList<string> environmentVariables,
        CancellationToken cancellationToken = default);
}
