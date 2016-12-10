using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using PackageFeedManager;

namespace ProjectFileTools
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
            string text = textView.TextBuffer.CurrentSnapshot.GetText();
            bool isCore = text.IndexOf("Microsoft.Net.Sdk", StringComparison.OrdinalIgnoreCase) > -1;

            if (isCore)
            {
                return new PackageIntellisenseController(textView, subjectBuffers, CompletionBroker);
            }

            return null;
        }
    }
}
