using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace ProjectFileTools.Adornments
{
    /// <summary>
    /// Used by HighLightWordTag to get the acquired tags' background color.
    /// </summary>
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