using CommunityToolkit.Aspire.Hosting.JavaScript;

namespace CommunityToolkit.Aspire.Hosting.JavaScript.Extensions.Tests;

public class SvelteKitEnvironmentTypeGeneratorTests
{
    [Fact]
    public async Task GeneratesCorrectModulesForMixedVariables()
    {
        var generator = new SvelteKitEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            string[] envVars =
            [
                "DATABASE_URL",
                "SECRET_KEY",
                "PUBLIC_API_URL",
                "PUBLIC_APP_NAME",
            ];

            await generator.GenerateAsync(tempDir, envVars);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            Assert.Contains("declare module '$env/static/private'", output);
            Assert.Contains("declare module '$env/static/public'", output);
            Assert.Contains("declare module '$env/dynamic/private'", output);
            Assert.Contains("declare module '$env/dynamic/public'", output);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task PrivateModuleContainsPrivateVariables()
    {
        var generator = new SvelteKitEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            string[] envVars = ["DATABASE_URL", "PUBLIC_API_URL"];

            await generator.GenerateAsync(tempDir, envVars);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            var privateStaticBlock = ExtractModuleBlock(output, "$env/static/private");
            Assert.Contains("DATABASE_URL", privateStaticBlock);
            Assert.DoesNotContain("PUBLIC_API_URL", privateStaticBlock);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task PublicModuleContainsPublicVariables()
    {
        var generator = new SvelteKitEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            string[] envVars = ["DATABASE_URL", "PUBLIC_API_URL"];

            await generator.GenerateAsync(tempDir, envVars);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            var publicStaticBlock = ExtractModuleBlock(output, "$env/static/public");
            Assert.DoesNotContain("DATABASE_URL", publicStaticBlock);
            Assert.Contains("PUBLIC_API_URL", publicStaticBlock);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GeneratesExportConstForStaticModules()
    {
        var generator = new SvelteKitEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            string[] envVars = ["MY_VAR"];

            await generator.GenerateAsync(tempDir, envVars);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            var privateStaticBlock = ExtractModuleBlock(output, "$env/static/private");
            Assert.Contains("export const MY_VAR: string;", privateStaticBlock);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GeneratesEnvObjectForDynamicModules()
    {
        var generator = new SvelteKitEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            string[] envVars = ["MY_VAR"];

            await generator.GenerateAsync(tempDir, envVars);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            var privateDynamicBlock = ExtractModuleBlock(output, "$env/dynamic/private");
            Assert.Contains("export const env:", privateDynamicBlock);
            Assert.Contains("MY_VAR: string;", privateDynamicBlock);
            Assert.Contains("[key: string]: string | undefined;", privateDynamicBlock);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task HandlesEmptyEnvironmentVariables()
    {
        var generator = new SvelteKitEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            await generator.GenerateAsync(tempDir, []);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            Assert.Contains("declare module '$env/static/private'", output);
            Assert.Contains("declare module '$env/static/public'", output);
            Assert.Contains("declare module '$env/dynamic/private'", output);
            Assert.Contains("declare module '$env/dynamic/public'", output);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task RespectsCustomPublicPrefix()
    {
        var generator = new SvelteKitEnvironmentTypeGenerator(publicPrefix: "APP_PUBLIC_");
        var tempDir = CreateTempDirectory();

        try
        {
            string[] envVars = ["APP_PUBLIC_URL", "PRIVATE_SECRET"];

            await generator.GenerateAsync(tempDir, envVars);

            var output = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "aspire-env.d.ts"));

            var publicBlock = ExtractModuleBlock(output, "$env/static/public");
            Assert.Contains("APP_PUBLIC_URL", publicBlock);
            Assert.DoesNotContain("PRIVATE_SECRET", publicBlock);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task RespectsCustomOutputPath()
    {
        var generator = new SvelteKitEnvironmentTypeGenerator(outputPath: "custom/types.d.ts");
        var tempDir = CreateTempDirectory();

        try
        {
            await generator.GenerateAsync(tempDir, ["MY_VAR"]);

            Assert.True(File.Exists(Path.Combine(tempDir, "custom", "types.d.ts")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CreatesDirectoryIfNotExists()
    {
        var generator = new SvelteKitEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            // The src/ directory doesn't exist yet
            var srcDir = Path.Combine(tempDir, "src");
            Assert.False(Directory.Exists(srcDir));

            await generator.GenerateAsync(tempDir, ["MY_VAR"]);

            Assert.True(Directory.Exists(srcDir));
            Assert.True(File.Exists(Path.Combine(srcDir, "aspire-env.d.ts")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task OverwritesExistingFile()
    {
        var generator = new SvelteKitEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            // First generate
            await generator.GenerateAsync(tempDir, ["FIRST_VAR"]);

            var outputPath = Path.Combine(tempDir, "src", "aspire-env.d.ts");
            var firstContent = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("FIRST_VAR", firstContent);

            // Second generate should overwrite
            await generator.GenerateAsync(tempDir, ["SECOND_VAR"]);

            var secondContent = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("SECOND_VAR", secondContent);
            Assert.DoesNotContain("FIRST_VAR", secondContent);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task OutputContainsAutoGeneratedComment()
    {
        var generator = new SvelteKitEnvironmentTypeGenerator();
        var tempDir = CreateTempDirectory();

        try
        {
            await generator.GenerateAsync(tempDir, ["MY_VAR"]);

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

    private static string ExtractModuleBlock(string content, string moduleName)
    {
        var startMarker = $"declare module '{moduleName}'";
        var startIndex = content.IndexOf(startMarker, StringComparison.Ordinal);
        if (startIndex < 0)
        {
            return string.Empty;
        }

        // Find the matching closing brace
        var braceStart = content.IndexOf('{', startIndex);
        if (braceStart < 0)
        {
            return string.Empty;
        }

        var depth = 0;
        var endIndex = braceStart;

        for (var i = braceStart; i < content.Length; i++)
        {
            if (content[i] == '{')
            {
                depth++;
            }
            else if (content[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    endIndex = i;
                    break;
                }
            }
        }

        return content[startIndex..(endIndex + 1)];
    }
}
