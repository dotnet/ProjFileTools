using Microsoft.VisualStudio.Text.Tagging;

namespace ProjectFileTools.Adornments
{
    // Each matched string from HighlightWordTagger is contained in a HighlightWordTag.
    internal class HighlightWordTag : TextMarkerTag
    {
        public HighlightWordTag() : base("MarkerFormatDefinition/HighlightWordFormatDefinition") { }
    }
}
