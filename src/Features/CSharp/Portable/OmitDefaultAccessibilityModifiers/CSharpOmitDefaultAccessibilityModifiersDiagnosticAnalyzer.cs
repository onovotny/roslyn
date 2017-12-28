// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.OmitDefaultAccessibilityModifiers;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.OmitDefaultAccessibilityModifiers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpOmitDefaultAccessibilityModifiersDiagnosticAnalyzer
        : AbstractOmitDefaultAccessibilityModifiersDiagnosticAnalyzer<CompilationUnitSyntax>
    {
        protected override void ProcessCompilationUnit(
            SyntaxTreeAnalysisContext context, SyntaxGenerator generator, 
            CodeStyleOption<bool> option, CompilationUnitSyntax compilationUnit)
        {
            ProcessMembers(context, generator, option, compilationUnit.Members);
        }

        private void ProcessMembers(
            SyntaxTreeAnalysisContext context, SyntaxGenerator generator, 
            CodeStyleOption<bool> option, 
            SyntaxList<MemberDeclarationSyntax> members)
        {
            foreach (var memberDeclaration in members)
            {
                ProcessMemberDeclaration(context, generator, option, memberDeclaration);
            }
        }

        private void ProcessMemberDeclaration(
            SyntaxTreeAnalysisContext context, SyntaxGenerator generator,
            CodeStyleOption<bool> option, MemberDeclarationSyntax member)
        {
            if (member.IsKind(SyntaxKind.NamespaceDeclaration, out NamespaceDeclarationSyntax namespaceDeclaration))
            {
                ProcessMembers(context, generator, option, namespaceDeclaration.Members);
            }

            // If we have a class or struct, recurse inwards.
            if (member.IsKind(SyntaxKind.ClassDeclaration, out TypeDeclarationSyntax typeDeclaration) ||
                member.IsKind(SyntaxKind.StructDeclaration, out typeDeclaration))
            {
                ProcessMembers(context, generator, option, typeDeclaration.Members);
            }

#if false
            // Add this once we have the language version for C# that supports accessibility
            // modifiers on interface methods.
            if (option.Value == AccessibilityModifiersRequired.Always &&
                member.IsKind(SyntaxKind.InterfaceDeclaration, out typeDeclaration))
            {
                // Only recurse into an interface if the user wants accessibility modifiers on 
                ProcessTypeDeclaration(context, generator, option, typeDeclaration);
            }
#endif

            // Have to have a name to report the issue on.
            var name = member.GetNameToken();
            if (name.Kind() == SyntaxKind.None)
            {
                return;
            }

            // Certain members never have accessibility. Don't bother reporting on them.
            if (!generator.CanHaveAccessibility(member))
            {
                return;
            }

            // If they already don't have an accessibility, no need to report anything.
            var accessibility = generator.GetAccessibility(member);
            if (accessibility == Accessibility.NotApplicable)
            {
                return;
            }

            // Check for default modifiers in namespace and outside of namespace
            var parentKind = member.Parent.Kind();
            if (parentKind == SyntaxKind.CompilationUnit || 
                parentKind == SyntaxKind.NamespaceDeclaration)
            {
                // Default is internal
                if (accessibility != Accessibility.Internal)
                {
                    return;
                }
            }

            if (parentKind == SyntaxKind.ClassDeclaration ||
                parentKind == SyntaxKind.StructDeclaration )
            {
                // Inside a type, default is private
                if (accessibility != Accessibility.Private)
                {
                    return;
                }
            }


            // Has default accessibility.  Report issue to user.
            var additionalLocations = ImmutableArray.Create(member.GetLocation());
            context.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptorWithSeverity(option.Notification.Value),
                name.GetLocation(),
                additionalLocations: additionalLocations));
        }
    }
}
