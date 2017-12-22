// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.OmitDefaultAccessibilityModifiers
{
    internal abstract class AbstractOmitDefaultAccessibilityModifiersDiagnosticAnalyzer<TCompilationUnitSyntax>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TCompilationUnitSyntax : SyntaxNode
    {
        protected AbstractOmitDefaultAccessibilityModifiersDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.OmitDefaultAccessibilityModifiersDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Omit_default_accessibility_modifiers), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Remove_default_accessibility_modifier), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxAnalysis;

        public sealed override bool OpenFileOnly(Workspace workspace)
            => false;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var syntaxTree = context.Tree;

            var workspaceAnalyzerOptions = context.Options as WorkspaceAnalyzerOptions;
            if (workspaceAnalyzerOptions == null)
            {
                return;
            }

            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var language = syntaxTree.Options.Language;
            var option = optionSet.GetOption(CodeStyleOptions.OmitDefaultAccessibilityModifiers, language);
            if (option.Value == false)
            {
                return;
            }

            var generator = SyntaxGenerator.GetGenerator(workspaceAnalyzerOptions.Services.Workspace, language);
            ProcessCompilationUnit(context, generator, option, (TCompilationUnitSyntax)syntaxTree.GetRoot(cancellationToken));
        }

        protected abstract void ProcessCompilationUnit(SyntaxTreeAnalysisContext context, SyntaxGenerator generator, CodeStyleOption<bool> option, TCompilationUnitSyntax compilationUnitSyntax);
    }
}
