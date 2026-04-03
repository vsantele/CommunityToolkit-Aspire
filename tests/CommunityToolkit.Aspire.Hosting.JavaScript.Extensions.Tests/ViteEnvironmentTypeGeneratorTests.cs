using CommunityToolkit.Aspire.Hosting.JavaScript;

namespace CommunityToolkit.Aspire.Hosting.JavaScript.Extensions.Tests;

public class ViteEnvironmentTypeGeneratorTests
{
    [Fact]
    public async Task GeneratesImportMetaEnvInterface()
    {
        var generator = new ViteEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            string[] envVars = ["VITE_API_URL", "VITE_APP_TITLE", "BACKEND_SECRET"];

            await generator.GenerateAsync(tempDir, envVars);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            Assert.Contains("interface ImportMetaEnv {", output);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task OnlyIncludesVitePrefixedVariables()
    {
        var generator = new ViteEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            string[] envVars = ["VITE_API_URL", "BACKEND_SECRET", "DATABASE_URL"];

            await generator.GenerateAsync(tempDir, envVars);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            Assert.Contains("VITE_API_URL", output);
            Assert.DoesNotContain("BACKEND_SECRET", output);
            Assert.DoesNotContain("DATABASE_URL", output);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task IncludesViteClientReference()
    {
        var generator = new ViteEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            await generator.GenerateAsync(tempDir, ["VITE_VAR"]);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            Assert.Contains("/// <reference types=\"vite/client\" />", output);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task IncludesImportMetaInterface()
    {
        var generator = new ViteEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            await generator.GenerateAsync(tempDir, ["VITE_VAR"]);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            Assert.Contains("interface ImportMeta {", output);
            Assert.Contains("readonly env: ImportMetaEnv;", output);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task VariablesAreReadonly()
    {
        var generator = new ViteEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            await generator.GenerateAsync(tempDir, ["VITE_API_URL"]);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            Assert.Contains("readonly VITE_API_URL: string;", output);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task HandlesEmptyEnvironmentVariables()
    {
        var generator = new ViteEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            await generator.GenerateAsync(tempDir, []);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            Assert.Contains("interface ImportMetaEnv {", output);
            Assert.Contains("}", output);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task RespectsCustomVitePrefix()
    {
        var generator = new ViteEnvironmentTypeGenerator(vitePrefix: "PUBLIC_");
        var tempDir = CreateTempDirectory();

        try
        {
            string[] envVars = ["PUBLIC_API_URL", "PRIVATE_SECRET"];

            await generator.GenerateAsync(tempDir, envVars);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            Assert.Contains("PUBLIC_API_URL", output);
            Assert.DoesNotContain("PRIVATE_SECRET", output);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task RespectsCustomOutputPath()
    {
        var generator = new ViteEnvironmentTypeGenerator(outputPath: "custom/env.d.ts");
        var tempDir = CreateTempDirectory();

        try
        {
            await generator.GenerateAsync(tempDir, ["VITE_VAR"]);

            Assert.True(File.Exists(Path.Combine(tempDir, "custom", "env.d.ts")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CreatesDirectoryIfNotExists()
    {
        var generator = new ViteEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            var srcDir = Path.Combine(tempDir, "src");
            Assert.False(Directory.Exists(srcDir));

            await generator.GenerateAsync(tempDir, ["VITE_VAR"]);

            Assert.True(File.Exists(Path.Combine(srcDir, "aspire-env.d.ts")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task OutputContainsAutoGeneratedComment()
    {
        var generator = new ViteEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            await generator.GenerateAsync(tempDir, ["VITE_VAR"]);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            Assert.Contains("Auto-generated by Aspire", output);
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
