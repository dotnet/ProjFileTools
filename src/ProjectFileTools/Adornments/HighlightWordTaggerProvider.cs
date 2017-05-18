using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Operations;

namespace ProjectFileTools.Adornments
{
    // Creates an instance of HighlightWordTagger and provides the tags it finds to the editor.
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("XML")]
    [TagType(typeof(TextMarkerTag))]
    internal class HighlightWordTaggerProvider : IViewTaggerProvider
    {
        [ImportingConstructor]
        public HighlightWordTaggerProvider(ITextSearchService textSearchService)
        {
            TextSearchService = textSearchService;
        }

        internal ITextSearchService TextSearchService { get; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView.TextBuffer != buffer)
                return null;
            return new HighlightWordTagger(textView, buffer, TextSearchService) as ITagger<T>;
        }
    }
}