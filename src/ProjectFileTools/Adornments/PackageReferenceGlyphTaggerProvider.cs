using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools.Adornments
{
    //[Export(typeof(IViewTaggerProvider))]
    //[ContentType("XML")]
    //[TagType(typeof(IntraTextAdornmentTag))]
    //internal class PackageReferenceGlyphTaggerProvider : IViewTaggerProvider
    //{
    //    private readonly IPackageSearchManager _searchManager;

    //    [ImportingConstructor]
    //    public PackageReferenceGlyphTaggerProvider(IPackageSearchManager searchManager)
    //    {
    //        _searchManager = searchManager;
    //    }

    //    public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer textBuffer)
    //        where T : ITag
    //    {
    //        IIntraTextAdornmentFactory<PackageGlyphTag> factory = new PackageGlyphTagFactory(_searchManager);
    //        return IntraTextAdornmentTagger<PackageGlyphTag>.GetOrCreate(textView, textBuffer, factory, "PackageReference") as ITagger<T>;
    //    }
    //}
}
