using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ForgeFlow.Analyzers;

/// <summary>
/// Roslyn analyzer that enforces the Localization Rule from .github/copilot-instructions.md §4:
/// "Always use <c>TranslationService.Get(key)</c> for user-facing text.
/// All string keys must be defined in <c>LocalizationKeys.cs</c>."
/// <para>
/// Emits <c>FF0001</c> when:
/// <list type="bullet">
/// <item>A raw string literal is passed as the first argument to
/// <c>TranslationService.Get</c> or <c>TranslationService.GetFormatted</c>.</item>
/// <item>A <c>const</c> field from a class other than <c>LocalizationKeys</c>
/// is used as the key (catches keys copied to ad-hoc local constants).</item>
/// </list>
/// </para>
/// <para>
/// Suppress for justified dynamic-key cases with:
/// <code>[SuppressMessage("ForgeFlow", "FF0001", Justification = "...")]</code>
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LocalizationKeyAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic ID emitted by this analyzer.</summary>
    public const string DiagnosticId = "FF0001";

    private const string TranslationServiceTypeName = "TranslationService";
    private const string TranslationServiceNamespace = "ForgeFlow.Core.Localization";
    private const string LocalizationKeysTypeName = "LocalizationKeys";

    private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Use a LocalizationKeys constant",
        messageFormat: "TranslationService.{0}() key argument should be a LocalizationKeys constant, not {1}. " +
                       "Add a constant to LocalizationKeys.cs and use that instead.",
        category: "ForgeFlow.Localization",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "All localization keys referenced from code must be declared as public const fields in " +
            "LocalizationKeys.cs. This ensures typos are caught at compile time and all keys are " +
            "audited by LocalizationValidationTests.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(_rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Must be a member-access call — e.g. _translation.Get("key")
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        string methodName = memberAccess.Name.Identifier.Text;
        if (methodName != "Get" && methodName != "GetFormatted")
        {
            return;
        }

        // Confirm the resolved symbol belongs to TranslationService in the correct namespace
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol method)
        {
            return;
        }

        if (method.ContainingType.Name != TranslationServiceTypeName)
        {
            return;
        }

        if (!string.Equals(
                method.ContainingType.ContainingNamespace?.ToDisplayString(),
                TranslationServiceNamespace,
                System.StringComparison.Ordinal))
        {
            return;
        }

        // Examine the first argument (the key)
        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 0)
        {
            return;
        }

        var firstArgExpr = args[0].Expression;

        // Case 1: raw string literal — always a violation
        if (firstArgExpr.IsKind(SyntaxKind.StringLiteralExpression) ||
            firstArgExpr.IsKind(SyntaxKind.InterpolatedStringExpression))
        {
            ReportDiagnostic(context, firstArgExpr, methodName, "a raw string literal");
            return;
        }

        // Case 2: verbatim string literal
        if (firstArgExpr.IsKind(SyntaxKind.StringLiteralExpression))
        {
            ReportDiagnostic(context, firstArgExpr, methodName, "a raw string literal");
            return;
        }

        // Case 3: a const field reference — accept ONLY if it is from LocalizationKeys
        var argSymbol = context.SemanticModel.GetSymbolInfo(firstArgExpr, context.CancellationToken).Symbol;
        if (argSymbol is IFieldSymbol field && field.IsConst)
        {
            if (field.ContainingType.Name == LocalizationKeysTypeName)
            {
                // Correct usage — pass
                return;
            }

            // Const from some other class — violation
            ReportDiagnostic(context, firstArgExpr, methodName,
                $"a const from {field.ContainingType.Name} (use LocalizationKeys instead)");
            return;
        }

        // Case 4: local const variable — check if its initializer is a LocalizationKeys member
        // This is a best-effort check; we don't flag non-const variables to avoid false positives.
        if (argSymbol is ILocalSymbol localSym && localSym.IsConst)
        {
            // We can't easily trace back to LocalizationKeys at this point, so emit a warning
            // to encourage moving the constant to LocalizationKeys.
            ReportDiagnostic(context, firstArgExpr, methodName,
                "a local const (declare it in LocalizationKeys.cs instead)");
        }

        // Variables, method parameters, etc. — do not flag (dynamic keys are valid in services/tests)
    }

    private static void ReportDiagnostic(
        SyntaxNodeAnalysisContext context,
        SyntaxNode location,
        string methodName,
        string keyDescription)
    {
        var diagnostic = Diagnostic.Create(
            _rule,
            location.GetLocation(),
            methodName,
            keyDescription);
        context.ReportDiagnostic(diagnostic);
    }
}
