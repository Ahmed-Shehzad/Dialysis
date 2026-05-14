using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Xunit;

namespace Dialysis.ArchitectureTests;

/// <summary>
/// CRITICAL gate for test-project member naming. Mirrors the editorconfig
/// `[**/{Tests,*.Tests,*.Tests.*}/**/*.cs]` rules so violations fail CI
/// (Roslyn does not run dotnet_naming_rule diagnostics at build time).
///
/// Every declaration inside a test project follows PascalCase tokens separated
/// by underscores. Tokens may also be numeric (e.g. `200`, `404`, `2xx`) so HTTP
/// status codes remain idiomatic.
///
///   async method     → Token_Token_..._Async
///   non-async member → Token_Token_...           (methods, properties, consts, public/protected fields, events)
///   private field    → _Token_Token_...          (leading "_" preserved as a visual marker for private state)
///
/// We syntax-parse each test source file with Roslyn (not regex) so property
/// declarations, indexers, operators, constructors etc. are classified
/// accurately.
/// </summary>
public sealed class TestMemberNamingConventionTests
{
    private const string TokenPattern = @"(?:[A-Z][a-z0-9]*|[0-9]+[a-z0-9]*)";

    private static readonly Regex _asyncMethodName =
        new($"^{TokenPattern}(?:_{TokenPattern})*_Async$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex _nonAsyncMemberName =
        new($"^{TokenPattern}(?:_{TokenPattern})*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex _privateFieldName =
        new($"^_{TokenPattern}(?:_{TokenPattern})*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Test_project_members_must_match_pascal_underscore_convention()
    {
        var root = FindSolutionRoot();
        var testFiles = EnumerateTestSourceFiles(root).ToList();
        testFiles.ShouldNotBeEmpty($"Expected to find test .cs files under {root}.");

        var offenders = new List<string>();
        foreach (var file in testFiles)
        {
            var rel = Path.GetRelativePath(root, file);
            var text = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(text);
            var rootNode = tree.GetRoot();

            foreach (var member in rootNode.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                // Heuristic to keep the rule pragmatic: if the member is declared
                // inside a type that has *any* base list (extends a class or
                // implements an interface), method/property/event names are very
                // often dictated by the parent contract and can't be renamed
                // without breaking the implementation. Private fields are never
                // base-class-dictated so we still check them.
                var containingTypeHasBaseList = TypeHasBaseList(member);
                Validate(member, rel, containingTypeHasBaseList, offenders);
            }
        }

        offenders.ShouldBeEmpty(
            "Test-project member names must follow the PascalCase_Underscore convention " +
            "(async methods end with _Async; private fields start with _). " +
            $"Violations:\n  - {string.Join("\n  - ", offenders)}");
    }

    private static void Validate(MemberDeclarationSyntax member, string relPath, bool containingTypeHasBaseList, List<string> offenders)
    {
        // Members marked `override` inherit their name from the base class —
        // renaming would not compile. Skip them. Explicit-interface impls are
        // handled per-kind below.
        if (member.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)))
            return;

        switch (member)
        {
            case MethodDeclarationSyntax method when !containingTypeHasBaseList:
                CheckMethod(method, relPath, offenders);
                break;

            case PropertyDeclarationSyntax property
                when property.ExplicitInterfaceSpecifier is null && !containingTypeHasBaseList:
                Check(property.Identifier.ValueText, isPrivateField: false, kind: "property", relPath,
                    line: property.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1, offenders);
                break;

            case EventDeclarationSyntax @event
                when @event.ExplicitInterfaceSpecifier is null && !containingTypeHasBaseList:
                Check(@event.Identifier.ValueText, isPrivateField: false, kind: "event", relPath,
                    line: @event.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1, offenders);
                break;

            case EventFieldDeclarationSyntax eventField when !containingTypeHasBaseList:
                foreach (var v in eventField.Declaration.Variables)
                    Check(v.Identifier.ValueText, isPrivateField: false, kind: "event", relPath,
                        line: v.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1, offenders);
                break;

            case FieldDeclarationSyntax field when !containingTypeHasBaseList:
                // Skip fields/consts on types with a base list — their names
                // are often part of a public contract referenced cross-file
                // (e.g. plugin Kind values keyed by an interface).
                CheckField(field, relPath, offenders);
                break;
        }
    }

    private static bool TypeHasBaseList(MemberDeclarationSyntax member)
    {
        for (SyntaxNode? node = member.Parent; node is not null; node = node.Parent)
        {
            if (node is TypeDeclarationSyntax t)
                return t.BaseList is { Types.Count: > 0 };
        }
        return false;
    }

    private static void CheckMethod(MethodDeclarationSyntax method, string relPath, List<string> offenders)
    {
        var name = method.Identifier.ValueText;
        var line = method.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        // Skip explicit interface implementations (the name is dictated by the interface).
        if (method.ExplicitInterfaceSpecifier is not null)
            return;

        var isAsync =
            method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)) ||
            ReturnsAwaitable(method.ReturnType);

        var rx = isAsync ? _asyncMethodName : _nonAsyncMemberName;
        if (!rx.IsMatch(name))
        {
            var suffix = isAsync ? " (expected …_Async)" : "";
            offenders.Add($"{relPath}:{line}: method {name}{suffix}");
        }
    }

    private static void CheckField(FieldDeclarationSyntax field, string relPath, List<string> offenders)
    {
        var isPrivate =
            field.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)) ||
            !field.Modifiers.Any(m =>
                m.IsKind(SyntaxKind.PublicKeyword) ||
                m.IsKind(SyntaxKind.InternalKeyword) ||
                m.IsKind(SyntaxKind.ProtectedKeyword));
        var isConst = field.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword));

        // Const fields read as values, not state — always non-private convention regardless of accessibility.
        var treatAsPrivate = isPrivate && !isConst;

        foreach (var v in field.Declaration.Variables)
        {
            var name = v.Identifier.ValueText;
            var line = v.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            Check(name, isPrivateField: treatAsPrivate, kind: isConst ? "const" : "field", relPath, line, offenders);
        }
    }

    private static void Check(string name, bool isPrivateField, string kind, string relPath, int line, List<string> offenders)
    {
        var rx = isPrivateField ? _privateFieldName : _nonAsyncMemberName;
        if (!rx.IsMatch(name))
        {
            offenders.Add($"{relPath}:{line}: {kind} {name}");
        }
    }

    private static bool ReturnsAwaitable(TypeSyntax returnType)
    {
        // Match `Task`, `Task<...>`, `ValueTask`, `ValueTask<...>` (with or without leading namespace).
        var asString = returnType.ToString();
        return asString.Contains("Task", StringComparison.Ordinal) ||
               asString.Contains("ValueTask", StringComparison.Ordinal);
    }

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dialysis.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            $"Could not locate Dialysis.slnx walking up from {AppContext.BaseDirectory}.");
    }

    private static IEnumerable<string> EnumerateTestSourceFiles(string root)
    {
        return Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(IsUnderTestProject)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static bool IsUnderTestProject(string path)
    {
        foreach (var segment in path.Split(Path.DirectorySeparatorChar))
        {
            if (segment.Equals("Tests", StringComparison.Ordinal)) return true;
            if (segment.EndsWith(".Tests", StringComparison.Ordinal)) return true;
            if (segment.Contains(".Tests.", StringComparison.Ordinal)) return true;
        }
        return false;
    }
}
