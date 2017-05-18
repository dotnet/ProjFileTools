using Microsoft.VisualStudio.Text.Tagging;

namespace ProjectFileTools.Adornments
{
    /// <summary>
    /// Each matched string from HighlightWordTagger is contained in a HighlightWordTag.
    /// </summary>
    internal class HighlightWordTag : TextMarkerTag
    {
        public HighlightWordTag() : base("MarkerFormatDefinition/HighlightWordFormatDefinition") { }
    }
}