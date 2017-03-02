using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools.Completion
{
    [Export(typeof(IUIElementProvider<Microsoft.VisualStudio.Language.Intellisense.Completion, ICompletionSession>))]
    [Name("Package Information Completion Tooltip")]
    [ContentType("XML")]
    internal class CompletionTooltipElementProvider : IUIElementProvider<Microsoft.VisualStudio.Language.Intellisense.Completion, ICompletionSession>
    {
        private readonly IPackageSearchManager _searcher;

        [ImportingConstructor]
        public CompletionTooltipElementProvider(IPackageSearchManager searcher)
        {
            _searcher = searcher;
        }

        public UIElement GetUIElement(Microsoft.VisualStudio.Language.Intellisense.Completion itemToRender, ICompletionSession context, UIElementType elementType)
        {
            if (itemToRender is PackageCompletion && elementType == UIElementType.Tooltip)
            {
                return new PackageInfoControl(itemToRender.DisplayText, null, null, _searcher);
            }
            else
            {
                return null;
            }
        }
    }
}
