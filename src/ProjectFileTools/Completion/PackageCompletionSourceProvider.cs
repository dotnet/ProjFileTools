using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools.Completion
{
    [Export(typeof(ICompletionSourceProvider))]
    [Name("Xml Package Intellisense Controller")]
    [ContentType("XML")]
    internal class PackageCompletionSourceProvider : ICompletionSourceProvider
    {
        private readonly IClassifierAggregatorService _classifier;
        private readonly ICompletionBroker _completionBroker;
        private readonly IPackageSearchManager _searchManager;

        [ImportingConstructor]
        public PackageCompletionSourceProvider(ICompletionBroker completionBroker, IPackageSearchManager searchManager, IClassifierAggregatorService classifier)
        {
            _classifier = classifier;
            _completionBroker = completionBroker;
            _searchManager = searchManager;
        }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            string text = textBuffer.CurrentSnapshot.GetText();
            bool isCore = text.IndexOf("Microsoft.Net.Sdk", StringComparison.OrdinalIgnoreCase) > -1;

            if (isCore)
            {
                return PackageCompletionSource.GetOrAddCompletionSource(textBuffer, _completionBroker, _classifier, _searchManager);
            }

            return null;
        }
    }
}
