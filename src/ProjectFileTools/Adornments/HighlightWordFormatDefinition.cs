using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
        /// <summary>
        /// Refers to MEF Items category (where the Highlighted Reference item is located)
        /// </summary>
        private readonly Guid _textEditorCategory = new Guid("{75A05685-00A8-4DED-BAE5-E7A50BFA929A}");

        /// <summary>
        /// Refers to the Highlighted Reference item
        /// </summary>
        private readonly string _itemName = "MarkerFormatDefinition/HighlightedReference";

        public HighlightWordFormatDefinition()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Default the BackgroundColor to a transparent grey
            BackgroundColor = Color.FromArgb(127, 170, 170, 170);
            DisplayName = "Highlight Word";
            ZOrder = 5;

            // If possible, set the Background color to match the Highlighted Reference color
            IVsFontAndColorStorage colorStorage = ServiceUtil.GetService<IVsFontAndColorStorage>();
            ColorableItemInfo[] itemInfoOut = new ColorableItemInfo[1];
            if(colorStorage.OpenCategory(ref _textEditorCategory, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS) ) == VSConstants.S_OK)
            {
                if (colorStorage.GetItem(_itemName, itemInfoOut) == VSConstants.S_OK)
                {
                    uint hexColor = itemInfoOut[0].crBackground;
                    BackgroundColor = Color.FromArgb(255, (byte)hexColor, (byte)(hexColor >> 8), (byte)(hexColor >> 16));
                }
                colorStorage.CloseCategory();
            }
        }
    }
}