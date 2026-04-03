using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using CommunityToolkit.Aspire.Hosting.JavaScript;
using CommunityToolkit.Aspire.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityToolkit.Aspire.Hosting.JavaScript.Extensions.Tests;

public class EnvironmentTypeExtensionsTests
{
    [Fact]
    public async Task WithSvelteKitEnvironmentTypes_GeneratesFileBeforeResourceStarts()
    {
        var builder = DistributedApplication.CreateBuilder();
        var tempDir = CreateTempDirectory();

        try
        {
            var nx = builder.AddNxApp("frontend", workingDirectory: tempDir);
            nx.AddApp("app")
                .WithEnvironment("PUBLIC_API_URL", "https://api.test")
                .WithEnvironment("DATABASE_URL", "postgres://test")
                .WithSvelteKitEnvironmentTypes();

            using var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            var resource = appModel.Resources.OfType<NxAppResource>().Single(r => r.Name == "app");

            await builder.Eventing.PublishAsync(
                new BeforeResourceStartedEvent(resource, app.Services),
                CancellationToken.None);

            var outputFile = Path.Combine(tempDir, "src", "aspire-env.d.ts");
            Assert.True(File.Exists(outputFile));

            var content = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("declare module '$env/static/public'", content);
            Assert.Contains("PUBLIC_API_URL", content);
            Assert.Contains("declare module '$env/static/private'", content);
            Assert.Contains("DATABASE_URL", content);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task WithViteEnvironmentTypes_GeneratesFileBeforeResourceStarts()
    {
        var builder = DistributedApplication.CreateBuilder();
        var tempDir = CreateTempDirectory();

        try
        {
            var nx = builder.AddNxApp("frontend", workingDirectory: tempDir);
            nx.AddApp("app")
                .WithEnvironment("VITE_API_URL", "https://api.test")
                .WithEnvironment("SECRET", "secret-value")
                .WithViteEnvironmentTypes();

            using var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            var resource = appModel.Resources.OfType<NxAppResource>().Single(r => r.Name == "app");

            await builder.Eventing.PublishAsync(
                new BeforeResourceStartedEvent(resource, app.Services),
                CancellationToken.None);

            var outputFile = Path.Combine(tempDir, "src", "aspire-env.d.ts");
            Assert.True(File.Exists(outputFile));

            var content = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("interface ImportMetaEnv", content);
            Assert.Contains("VITE_API_URL", content);
            Assert.DoesNotContain("SECRET", content);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task WithNodeEnvironmentTypes_GeneratesFileBeforeResourceStarts()
    {
        var builder = DistributedApplication.CreateBuilder();
        var tempDir = CreateTempDirectory();

        try
        {
            var nx = builder.AddNxApp("frontend", workingDirectory: tempDir);
            nx.AddApp("app")
                .WithEnvironment("DATABASE_URL", "postgres://test")
                .WithEnvironment("PORT", "3000")
                .WithNodeEnvironmentTypes();

            using var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            var resource = appModel.Resources.OfType<NxAppResource>().Single(r => r.Name == "app");

            await builder.Eventing.PublishAsync(
                new BeforeResourceStartedEvent(resource, app.Services),
                CancellationToken.None);

            var outputFile = Path.Combine(tempDir, "aspire-env.d.ts");
            Assert.True(File.Exists(outputFile));

            var content = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("declare namespace NodeJS", content);
            Assert.Contains("DATABASE_URL", content);
            Assert.Contains("PORT", content);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task WithEnvironmentTypes_FiltersOutAspireInternalVariables()
    {
        var builder = DistributedApplication.CreateBuilder();
        var tempDir = CreateTempDirectory();

        try
        {
            var nx = builder.AddNxApp("frontend", workingDirectory: tempDir);
            nx.AddApp("app")
                .WithEnvironment("MY_APP_VAR", "value")
                .WithSvelteKitEnvironmentTypes();

            // Register OTEL_ variable via annotation directly
            using var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            var resource = appModel.Resources.OfType<NxAppResource>().Single(r => r.Name == "app");

            await builder.Eventing.PublishAsync(
                new BeforeResourceStartedEvent(resource, app.Services),
                CancellationToken.None);

            var outputFile = Path.Combine(tempDir, "src", "aspire-env.d.ts");
            var content = await File.ReadAllTextAsync(outputFile);

            Assert.Contains("MY_APP_VAR", content);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task WithEnvironmentTypes_WithCustomExcludePrefixes_FiltersCorrectly()
    {
        var builder = DistributedApplication.CreateBuilder();
        var tempDir = CreateTempDirectory();

        try
        {
            var nx = builder.AddNxApp("frontend", workingDirectory: tempDir);
            nx.AddApp("app")
                .WithEnvironment("SECRET_KEY", "value")
                .WithEnvironment("PUBLIC_URL", "https://example.com")
                .WithSvelteKitEnvironmentTypes(excludePrefixes: ["SECRET_"]);

            using var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            var resource = appModel.Resources.OfType<NxAppResource>().Single(r => r.Name == "app");

            await builder.Eventing.PublishAsync(
                new BeforeResourceStartedEvent(resource, app.Services),
                CancellationToken.None);

            var outputFile = Path.Combine(tempDir, "src", "aspire-env.d.ts");
            var content = await File.ReadAllTextAsync(outputFile);

            Assert.DoesNotContain("SECRET_KEY", content);
            Assert.Contains("PUBLIC_URL", content);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task WithEnvironmentTypes_WithCustomGenerator_UsesGeneratorOutputPath()
    {
        var builder = DistributedApplication.CreateBuilder();
        var tempDir = CreateTempDirectory();

        try
        {
            var generator = new NodeEnvironmentTypeGenerator(outputPath: "custom/types.d.ts");

            var nx = builder.AddNxApp("frontend", workingDirectory: tempDir);
            nx.AddApp("app")
                .WithEnvironment("MY_VAR", "value")
                .WithEnvironmentTypes(generator);

            using var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            var resource = appModel.Resources.OfType<NxAppResource>().Single(r => r.Name == "app");

            await builder.Eventing.PublishAsync(
                new BeforeResourceStartedEvent(resource, app.Services),
                CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(tempDir, "custom", "types.d.ts")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }
}
