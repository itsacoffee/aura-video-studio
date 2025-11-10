using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aura.Analyzers;

/// <summary>
/// Analyzer to prevent direct provider usage outside orchestrator namespaces
/// Enforces that all provider calls go through the orchestration layer
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DirectProviderUsageAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "AUR001";
    private const string Category = "Architecture";

    private static readonly LocalizableString Title = 
        "Direct provider usage forbidden outside orchestrator layer";
    
    private static readonly LocalizableString MessageFormat = 
        "Direct usage of '{0}' is not allowed. Use orchestrator layer instead.";
    
    private static readonly LocalizableString Description = 
        "All provider calls must go through the unified orchestration layer to ensure middleware is applied.";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    // Provider interfaces that must go through orchestrator
    private static readonly string[] ForbiddenProviderTypes = new[]
    {
        "ILlmProvider",
        "ITtsProvider",
        "IImageProvider",
        "IVideoProvider",
        "IPlannerProvider"
    };

    // Namespaces allowed to use providers directly (orchestrator infrastructure)
    private static readonly string[] AllowedNamespaces = new[]
    {
        "Aura.Core.Orchestration",
        "Aura.Core.Orchestrator",
        "Aura.Core.AI.Orchestration",  // AI orchestration infrastructure
        "Aura.Providers",
        "Aura.Api.Startup",  // DI registration is allowed
        "Aura.Api.Program",  // Main program DI registration
        "Aura.Tests",  // Tests can mock providers
        "Aura.E2E"  // E2E tests can use providers
    };

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Analyze field declarations for provider injection
        context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        
        // Analyze parameters for provider injection in constructors
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
        
        // Analyze object creation for direct provider instantiation
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
        var variableDeclaration = fieldDeclaration.Declaration;

        if (variableDeclaration?.Type == null)
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(variableDeclaration.Type);
        if (typeInfo.Type == null)
            return;

        var typeName = typeInfo.Type.Name;

        if (!IsForbiddenProviderType(typeName))
            return;

        if (IsInAllowedNamespace(context))
            return;

        var diagnostic = Diagnostic.Create(Rule, fieldDeclaration.GetLocation(), typeName);
        context.ReportDiagnostic(diagnostic);
    }

    private void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var parameter = (ParameterSyntax)context.Node;

        if (parameter.Type == null)
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(parameter.Type);
        if (typeInfo.Type == null)
            return;

        var typeName = typeInfo.Type.Name;

        if (!IsForbiddenProviderType(typeName))
            return;

        if (IsInAllowedNamespace(context))
            return;

        // Allow if parameter is in a constructor for DI registration classes
        if (IsInDependencyInjectionContext(parameter))
            return;

        var diagnostic = Diagnostic.Create(Rule, parameter.GetLocation(), typeName);
        context.ReportDiagnostic(diagnostic);
    }

    private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        if (objectCreation.Type == null)
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation.Type);
        if (typeInfo.Type == null)
            return;

        var typeName = typeInfo.Type.Name;

        if (!IsForbiddenProviderType(typeName))
            return;

        if (IsInAllowedNamespace(context))
            return;

        var diagnostic = Diagnostic.Create(Rule, objectCreation.GetLocation(), typeName);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsForbiddenProviderType(string typeName)
    {
        return ForbiddenProviderTypes.Any(forbidden => 
            typeName.Equals(forbidden, StringComparison.Ordinal) ||
            typeName.EndsWith(forbidden, StringComparison.Ordinal));
    }

    private static bool IsInAllowedNamespace(SyntaxNodeAnalysisContext context)
    {
        var namespaceSymbol = context.ContainingSymbol?.ContainingNamespace;
        if (namespaceSymbol == null)
            return false;

        var namespaceName = namespaceSymbol.ToDisplayString();

        return AllowedNamespaces.Any(allowed => 
            namespaceName.StartsWith(allowed, StringComparison.Ordinal));
    }

    private static bool IsInDependencyInjectionContext(ParameterSyntax parameter)
    {
        // Check if parameter is in a method that looks like DI registration
        var method = parameter.Parent?.Parent as MethodDeclarationSyntax;
        if (method == null)
            return false;

        var methodName = method.Identifier.Text;
        
        // Common DI registration method patterns
        return methodName.IndexOf("Services", StringComparison.Ordinal) >= 0 ||
               methodName.IndexOf("Configure", StringComparison.Ordinal) >= 0 ||
               methodName.IndexOf("Register", StringComparison.Ordinal) >= 0 ||
               methodName.IndexOf("Add", StringComparison.Ordinal) >= 0 && 
               (methodName.IndexOf("Provider", StringComparison.Ordinal) >= 0 || 
                methodName.IndexOf("Dependency", StringComparison.Ordinal) >= 0);
    }
}
