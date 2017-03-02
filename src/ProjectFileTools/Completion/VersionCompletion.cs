using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;

namespace ProjectFileTools.Completion
{
    public class VersionCompletion : Completion3
    {
        public VersionCompletion(string displayText, string insertionText, string description, ImageMoniker iconMoniker, string iconAutomationText) 
            : base(displayText, insertionText, description, iconMoniker, iconAutomationText)
        {
        }
    }
}
