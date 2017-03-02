using System.Collections.Generic;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;

namespace ProjectFileTools.Completion
{
    public class PackageCompletion : Completion4
    {
        public PackageCompletion(string displayText, string insertionText, string description, ImageMoniker iconMoniker, string iconAutomationText = null, IEnumerable<CompletionIcon2> attributeIcons = null, string suffix = null)
            : base(displayText, insertionText, description, iconMoniker, iconAutomationText, attributeIcons, suffix)
        {
        }
    }
}
