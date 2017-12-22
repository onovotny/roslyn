' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.OmitDefaultAccessibilityModifiers
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.OmitDefaultAccessibilityModifiers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicOmitDefaultAccessibilityModifiersDiagnosticAnalyzer
        Inherits AbstractOmitDefaultAccessibilityModifiersDiagnosticAnalyzer(Of CompilationUnitSyntax)

        Protected Overrides Sub ProcessCompilationUnit(
                context As SyntaxTreeAnalysisContext, generator As SyntaxGenerator,
                [option] As CodeStyleOption(Of Boolean), compilationUnit As CompilationUnitSyntax)

            ProcessMembers(context, generator, [option], compilationUnit.Members)
        End Sub

        Private Sub ProcessMembers(context As SyntaxTreeAnalysisContext, generator As SyntaxGenerator,
                                   [option] As CodeStyleOption(Of Boolean), members As SyntaxList(Of StatementSyntax))
            For Each member In members
                ProcessMember(context, generator, [option], member)
            Next
        End Sub

        Private Sub ProcessMember(context As SyntaxTreeAnalysisContext, generator As SyntaxGenerator,
                              [option] As CodeStyleOption(Of Boolean), member As StatementSyntax)


            If member.Kind() = SyntaxKind.NamespaceBlock Then
                Dim namespaceBlock = DirectCast(member, NamespaceBlockSyntax)
                ProcessMembers(context, generator, [option], namespaceBlock.Members)
            End If

            ' If we have a class or struct or module, recurse inwards.
            If member.IsKind(SyntaxKind.ClassBlock) OrElse
               member.IsKind(SyntaxKind.StructureBlock) OrElse
               member.IsKind(SyntaxKind.ModuleBlock) Then

                Dim typeBlock = DirectCast(member, TypeBlockSyntax)
                ProcessMembers(context, generator, [option], typeBlock.Members)
            End If

            ' Have to have a name to report the issue on.
            Dim name = member.GetNameToken()
            If name.Kind() = SyntaxKind.None Then
                Return
            End If

            ' Certain members never have accessibility. Don't bother reporting on them.
            If Not generator.CanHaveAccessibility(member) Then
                Return
            End If

            ' If they already don't have an accessibility, no need to report anything.
            Dim Accessibility = generator.GetAccessibility(member)
            If Accessibility = Accessibility.NotApplicable Then
                Return
            End If

            ' TODO: Add checks for default

            ' Has default accessibility.  Report issue to user.
            Dim additionalLocations = ImmutableArray.Create(member.GetLocation())
            context.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptorWithSeverity([option].Notification.Value),
                name.GetLocation(),
                additionalLocations:=additionalLocations))
        End Sub
    End Class
End Namespace
