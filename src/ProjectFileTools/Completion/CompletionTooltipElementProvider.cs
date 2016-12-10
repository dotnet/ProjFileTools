using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using PackageFeedManager;

namespace ProjectFileTools
{
    [Export(typeof(IUIElementProvider<Completion, ICompletionSession>))]
    [Name("Package Information Completion Tooltip")]
    [ContentType("XML")]
    internal class CompletionTooltipElementProvider : IUIElementProvider<Completion, ICompletionSession>
    {
        private readonly IPackageSearchManager _searcher;

        [ImportingConstructor]
        public CompletionTooltipElementProvider(IPackageSearchManager searcher)
        {
            _searcher = searcher;
        }

        public UIElement GetUIElement(Completion itemToRender, ICompletionSession context, UIElementType elementType)
        {
            if (elementType == UIElementType.Tooltip)
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
