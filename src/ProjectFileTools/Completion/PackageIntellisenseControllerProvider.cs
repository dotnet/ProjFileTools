using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools.Completion
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("Xml Package Intellisense Controller")]
    [ContentType("XML")]
    internal class PackageIntellisenseControllerProvider : IIntellisenseControllerProvider
    {
        private readonly IPackageSearchManager _searchManager;

        [ImportingConstructor]
        public PackageIntellisenseControllerProvider(ICompletionBroker completionBroker, IPackageSearchManager searchManager)
        {
            CompletionBroker = completionBroker;
            _searchManager = searchManager;
        }

        internal ICompletionBroker CompletionBroker { get; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new PackageIntellisenseController(textView, subjectBuffers, CompletionBroker);
        }
    }
}
