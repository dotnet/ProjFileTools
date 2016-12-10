using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using PackageFeedManager;

namespace ProjectFileTools
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("XML")]
    [TagType(typeof(IntraTextAdornmentTag))]
    internal class PackageGlyphTaggerProvider : IViewTaggerProvider
    {
        private readonly IBufferTagAggregatorFactoryService _bufferTagAggregatorFactoryService;
        private readonly IPackageSearchManager _searchManager;

        [ImportingConstructor]
        public PackageGlyphTaggerProvider(IPackageSearchManager searchManager, IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService)
        {
            _searchManager = searchManager;
            _bufferTagAggregatorFactoryService = bufferTagAggregatorFactoryService;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer textBuffer)
            where T : ITag
        {
            IIntraTextAdornmentFactory<PackageGlyphTag> factory = new PackageGlyphTagFactory(_searchManager);
            return IntraTextAdornmentTagger<PackageGlyphTag>.GetOrCreate(textView, textBuffer, factory) as ITagger<T>;
        }
    }
}
