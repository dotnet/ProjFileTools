using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace ProjectFileTools.Adornments
{
    // Used by HighLightWordTag to get the acquired tags' background color.
    [Export(typeof(EditorFormatDefinition))]
    [Name("MarkerFormatDefinition/HighlightWordFormatDefinition")]
    internal class HighlightWordFormatDefinition : MarkerFormatDefinition
    {
        public HighlightWordFormatDefinition()
        {
            this.BackgroundColor = Color.FromArgb(127, 170, 170, 170);
            this.DisplayName = "Highlight Word";
            this.ZOrder = 5;
        }
    }
}