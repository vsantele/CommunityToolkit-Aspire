using Aspire.Hosting.ApplicationModel;
using CommunityToolkit.Aspire.Hosting.JavaScript;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for generating TypeScript environment type
/// declarations from Aspire resource environment variables.
/// </summary>
public static class EnvironmentTypeExtensions
{
    /// <summary>
    /// Generates TypeScript type declarations for the environment
    /// variables configured on this resource. The declarations are
    /// written to a <c>.d.ts</c> file in the resource's working directory
    /// before the resource starts.
    /// </summary>
    /// <typeparam name="T">The resource type, which must support environment variables.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="generator">
    /// The strategy that determines the output format
    /// (SvelteKit, Vite, Node, or a custom implementation).
    /// </param>
    /// <param name="excludePrefixes">
    /// Optional additional variable prefixes to exclude (e.g. <c>"MY_INTERNAL_"</c>).
    /// The default exclusions (<c>OTEL_</c>, <c>DOTNET_</c>, etc.) always apply.
    /// </param>
    public static IResourceBuilder<T> WithEnvironmentTypes<T>(
        this IResourceBuilder<T> builder,
        IEnvironmentTypeGenerator generator,
        string[]? excludePrefixes = null)
        where T : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(generator);

        var executionContext = builder.ApplicationBuilder.ExecutionContext;

        builder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(
            builder.Resource,
            async (evt, ct) =>
            {
                // Only generate types during local development (not publish/deploy)
                if (executionContext.IsPublishMode)
                {
                    return;
                }

                // Collect variable names by executing all environment callbacks
                var callbackContext = new EnvironmentCallbackContext(executionContext);

                foreach (var annotation in builder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
                {
                    await annotation.Callback(callbackContext).ConfigureAwait(false);
                }

                var filteredNames = EnvironmentVariableFilter.Apply(
                    callbackContext.EnvironmentVariables.Keys,
                    excludePrefixes);

                var workingDirectory = ResolveWorkingDirectory(builder.Resource);

                await generator.GenerateAsync(workingDirectory, filteredNames, ct).ConfigureAwait(false);
            });

        return builder;
    }

    /// <summary>
    /// Generates SvelteKit-compatible TypeScript declarations (modules
    /// <c>$env/static/private</c>, <c>$env/static/public</c>,
    /// <c>$env/dynamic/private</c>, and <c>$env/dynamic/public</c>)
    /// from the environment variables configured on this resource.
    /// </summary>
    /// <typeparam name="T">The resource type, which must support environment variables.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="publicPrefix">
    /// Variable name prefix that marks a variable as public (default: <c>"PUBLIC_"</c>).
    /// </param>
    /// <param name="outputPath">
    /// Path of the generated file, relative to the working directory
    /// (default: <c>"src/aspire-env.d.ts"</c>).
    /// </param>
    /// <param name="excludePrefixes">
    /// Optional additional variable prefixes to exclude in addition to
    /// the built-in defaults (<c>OTEL_</c>, <c>DOTNET_</c>, etc.).
    /// </param>
    public static IResourceBuilder<T> WithSvelteKitEnvironmentTypes<T>(
        this IResourceBuilder<T> builder,
        string publicPrefix = "PUBLIC_",
        string outputPath = "src/aspire-env.d.ts",
        string[]? excludePrefixes = null)
        where T : IResourceWithEnvironment
    {
        return builder.WithEnvironmentTypes(
            new SvelteKitEnvironmentTypeGenerator(publicPrefix, outputPath),
            excludePrefixes);
    }

    /// <summary>
    /// Generates a Vite-compatible TypeScript declaration file that augments
    /// <c>ImportMetaEnv</c> with the <c>VITE_</c>-prefixed environment variables
    /// configured on this resource.
    /// </summary>
    /// <typeparam name="T">The resource type, which must support environment variables.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="vitePrefix">
    /// Variable name prefix for client-exposed variables (default: <c>"VITE_"</c>).
    /// </param>
    /// <param name="outputPath">
    /// Path of the generated file, relative to the working directory
    /// (default: <c>"src/aspire-env.d.ts"</c>).
    /// </param>
    /// <param name="excludePrefixes">
    /// Optional additional variable prefixes to exclude in addition to
    /// the built-in defaults.
    /// </param>
    public static IResourceBuilder<T> WithViteEnvironmentTypes<T>(
        this IResourceBuilder<T> builder,
        string vitePrefix = "VITE_",
        string outputPath = "src/aspire-env.d.ts",
        string[]? excludePrefixes = null)
        where T : IResourceWithEnvironment
    {
        return builder.WithEnvironmentTypes(
            new ViteEnvironmentTypeGenerator(vitePrefix, outputPath),
            excludePrefixes);
    }

    /// <summary>
    /// Generates a TypeScript declaration file that augments
    /// <c>NodeJS.ProcessEnv</c> with the environment variables
    /// configured on this resource.
    /// </summary>
    /// <typeparam name="T">The resource type, which must support environment variables.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="outputPath">
    /// Path of the generated file, relative to the working directory
    /// (default: <c>"aspire-env.d.ts"</c>).
    /// </param>
    /// <param name="excludePrefixes">
    /// Optional additional variable prefixes to exclude in addition to
    /// the built-in defaults.
    /// </param>
    public static IResourceBuilder<T> WithNodeEnvironmentTypes<T>(
        this IResourceBuilder<T> builder,
        string outputPath = "aspire-env.d.ts",
        string[]? excludePrefixes = null)
        where T : IResourceWithEnvironment
    {
        return builder.WithEnvironmentTypes(
            new NodeEnvironmentTypeGenerator(outputPath),
            excludePrefixes);
    }

    private static string ResolveWorkingDirectory(IResource resource)
    {
        if (resource is ExecutableResource execResource)
        {
            return execResource.WorkingDirectory;
        }

        throw new InvalidOperationException(
            $"Cannot determine the working directory for resource '{resource.Name}' " +
            $"(type: {resource.GetType().Name}). " +
            "Only resources derived from ExecutableResource are supported by " +
            nameof(WithEnvironmentTypes) + ".");
    }
}
