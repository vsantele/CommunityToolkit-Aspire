using CommunityToolkit.Aspire.Hosting.JavaScript;

namespace CommunityToolkit.Aspire.Hosting.JavaScript.Extensions.Tests;

public class EnvironmentVariableFilterTests
{
    [Fact]
    public void Apply_FiltersOtelVariables()
    {
        string[] input = ["OTEL_SERVICE_NAME", "MY_VAR", "OTEL_EXPORTER_ENDPOINT"];
        var result = EnvironmentVariableFilter.Apply(input);
        Assert.DoesNotContain("OTEL_SERVICE_NAME", result);
        Assert.DoesNotContain("OTEL_EXPORTER_ENDPOINT", result);
        Assert.Contains("MY_VAR", result);
    }

    [Fact]
    public void Apply_FiltersDotnetVariables()
    {
        string[] input = ["DOTNET_RUNNING_IN_CONTAINER", "MY_VAR", "DOTNET_GC_HEAP_LIMIT"];
        var result = EnvironmentVariableFilter.Apply(input);
        Assert.DoesNotContain("DOTNET_RUNNING_IN_CONTAINER", result);
        Assert.DoesNotContain("DOTNET_GC_HEAP_LIMIT", result);
        Assert.Contains("MY_VAR", result);
    }

    [Fact]
    public void Apply_FiltersAspNetCoreVariables()
    {
        string[] input = ["ASPNETCORE_URLS", "MY_VAR"];
        var result = EnvironmentVariableFilter.Apply(input);
        Assert.DoesNotContain("ASPNETCORE_URLS", result);
        Assert.Contains("MY_VAR", result);
    }

    [Fact]
    public void Apply_FiltersAspireVariables()
    {
        string[] input = ["ASPIRE_ALLOW_UNSECURED_TRANSPORT", "MY_VAR"];
        var result = EnvironmentVariableFilter.Apply(input);
        Assert.DoesNotContain("ASPIRE_ALLOW_UNSECURED_TRANSPORT", result);
        Assert.Contains("MY_VAR", result);
    }

    [Fact]
    public void Apply_FiltersServicesDoubleUnderscoreVariables()
    {
        string[] input = ["services__api__https__0", "MY_VAR"];
        var result = EnvironmentVariableFilter.Apply(input);
        Assert.DoesNotContain("services__api__https__0", result);
        Assert.Contains("MY_VAR", result);
    }

    [Fact]
    public void Apply_FiltersConnectionStringsVariables()
    {
        string[] input = ["ConnectionStrings__db", "MY_VAR"];
        var result = EnvironmentVariableFilter.Apply(input);
        Assert.DoesNotContain("ConnectionStrings__db", result);
        Assert.Contains("MY_VAR", result);
    }

    [Fact]
    public void Apply_FiltersInvalidIdentifiers()
    {
        string[] input = ["MY-VAR", "MY.VAR", "123VAR", "MY_VAR"];
        var result = EnvironmentVariableFilter.Apply(input);
        Assert.DoesNotContain("MY-VAR", result);
        Assert.DoesNotContain("MY.VAR", result);
        Assert.DoesNotContain("123VAR", result);
        Assert.Contains("MY_VAR", result);
    }

    [Fact]
    public void Apply_KeepsValidVariables()
    {
        string[] input = ["DATABASE_URL", "PORT", "NODE_ENV", "PUBLIC_API_URL"];
        var result = EnvironmentVariableFilter.Apply(input);
        Assert.Contains("DATABASE_URL", result);
        Assert.Contains("PORT", result);
        Assert.Contains("NODE_ENV", result);
        Assert.Contains("PUBLIC_API_URL", result);
    }

    [Fact]
    public void Apply_SortsAlphabetically()
    {
        string[] input = ["ZEBRA_VAR", "ALPHA_VAR", "MIDDLE_VAR"];
        var result = EnvironmentVariableFilter.Apply(input);
        var list = result.ToList();
        Assert.Equal("ALPHA_VAR", list[0]);
        Assert.Equal("MIDDLE_VAR", list[1]);
        Assert.Equal("ZEBRA_VAR", list[2]);
    }

    [Fact]
    public void Apply_AppliesAdditionalExcludePrefixes()
    {
        string[] input = ["MY_SECRET_KEY", "MY_SECRET_PASS", "PUBLIC_VAR"];
        var result = EnvironmentVariableFilter.Apply(input, ["MY_SECRET_"]);
        Assert.DoesNotContain("MY_SECRET_KEY", result);
        Assert.DoesNotContain("MY_SECRET_PASS", result);
        Assert.Contains("PUBLIC_VAR", result);
    }

    [Fact]
    public void Apply_EmptyInput_ReturnsEmpty()
    {
        var result = EnvironmentVariableFilter.Apply([]);
        Assert.Empty(result);
    }

    [Fact]
    public void IsValidTypeScriptIdentifier_ValidIdentifiers()
    {
        Assert.True(EnvironmentVariableFilter.IsValidTypeScriptIdentifier("MY_VAR"));
        Assert.True(EnvironmentVariableFilter.IsValidTypeScriptIdentifier("myVar"));
        Assert.True(EnvironmentVariableFilter.IsValidTypeScriptIdentifier("_private"));
        Assert.True(EnvironmentVariableFilter.IsValidTypeScriptIdentifier("$var"));
        Assert.True(EnvironmentVariableFilter.IsValidTypeScriptIdentifier("VAR123"));
    }

    [Fact]
    public void IsValidTypeScriptIdentifier_InvalidIdentifiers()
    {
        Assert.False(EnvironmentVariableFilter.IsValidTypeScriptIdentifier("123var"));
        Assert.False(EnvironmentVariableFilter.IsValidTypeScriptIdentifier("my-var"));
        Assert.False(EnvironmentVariableFilter.IsValidTypeScriptIdentifier("my.var"));
        Assert.False(EnvironmentVariableFilter.IsValidTypeScriptIdentifier(""));
        Assert.False(EnvironmentVariableFilter.IsValidTypeScriptIdentifier("my var"));
    }
}
