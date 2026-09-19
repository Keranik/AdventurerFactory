using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ForgeFlow.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for <see cref="LocalizationKeyAnalyzer"/> (diagnostic FF0001).
/// Verifies that TranslationService.Get / GetFormatted enforce LocalizationKeys usage.
/// Uses raw Roslyn APIs (CSharpCompilation + CompilationWithAnalyzers) to avoid the
/// heavy Microsoft.CodeAnalysis.*.Testing packages and their VS-component prompts.
/// </summary>
public sealed class LocalizationAnalyzerTests
{
    // Minimal stub — namespace + type names must match what the analyzer checks.
    private const string Stubs = """
        namespace ForgeFlow.Core.Localization
        {
            public sealed class TranslationService
            {
                public string Get(string key) => key;
                public string GetFormatted(string key, params object[] args) => key;
                public string GetFormatted(string key, object arg0) => key;
            }

            public static class LocalizationKeys
            {
                public const string SomeKey = "some.key";
            }
        }
        """;

    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(string userSource)
    {
        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(Stubs),
            CSharpSyntaxTree.ParseText(userSource),
        };

        // Reference every assembly already loaded in the test AppDomain so the compilation
        // can resolve `object`, `string`, attributes, etc. on .NET 8.
        var references = System.AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location));

        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerTestAsm",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new LocalizationKeyAnalyzer());
        var withAnalyzers = compilation.WithAnalyzers(analyzers);
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Only return FF0001 (the analyzer under test); ignore unrelated compile warnings.
        return diagnostics
            .Where(d => d.Id == LocalizationKeyAnalyzer.DiagnosticId)
            .ToImmutableArray();
    }

    // ─── Violation cases ──────────────────────────────────────────────────────

    [Fact]
    public async Task Get_WithRawStringLiteral_EmitsFF0001()
    {
        const string source = """
            using ForgeFlow.Core.Localization;
            class C
            {
                void M(TranslationService ts) => ts.Get("raw.key");
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task GetFormatted_WithRawStringLiteral_EmitsFF0001()
    {
        const string source = """
            using ForgeFlow.Core.Localization;
            class C
            {
                void M(TranslationService ts) => ts.GetFormatted("raw.key", 42);
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Get_WithConstFromOtherClass_EmitsFF0001()
    {
        const string source = """
            using ForgeFlow.Core.Localization;
            static class OtherKeys { public const string Foo = "other.foo"; }
            class C
            {
                void M(TranslationService ts) => ts.Get(OtherKeys.Foo);
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Single(diagnostics);
    }

    // ─── Clean cases ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_WithLocalizationKeysConst_NoDiagnostic()
    {
        const string source = """
            using ForgeFlow.Core.Localization;
            class C
            {
                void M(TranslationService ts) => ts.Get(LocalizationKeys.SomeKey);
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GetFormatted_WithLocalizationKeysConst_NoDiagnostic()
    {
        const string source = """
            using ForgeFlow.Core.Localization;
            class C
            {
                void M(TranslationService ts) => ts.GetFormatted(LocalizationKeys.SomeKey, 1);
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Get_WithMethodParameter_NoDiagnostic()
    {
        const string source = """
            using ForgeFlow.Core.Localization;
            class C
            {
                void M(TranslationService ts, string dynamicKey) => ts.Get(dynamicKey);
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Get_WithVariable_NoDiagnostic()
    {
        const string source = """
            using ForgeFlow.Core.Localization;
            class C
            {
                void M(TranslationService ts)
                {
                    string key = "dynamic";
                    ts.Get(key);
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Get_OnUnrelatedType_NoDiagnostic()
    {
        const string source = """
            class AnotherService
            {
                public string Get(string key) => key;
            }
            class C
            {
                void M(AnotherService s) => s.Get("anything");
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }
}
