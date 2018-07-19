using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.Completion;
using ProjectFileTools.MSBuild;

namespace ProjectFileTools
{
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("Xml Package Intellisense Controller")]
    [ContentType("XML")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class TextViewCreationListener : IVsTextViewCreationListener
    {
        private readonly IVsEditorAdaptersFactoryService _adaptersFactory;
        private readonly ICompletionBroker _completionBroker;
        private readonly IWorkspaceManager _workspaceManager;

        //[Export(typeof(AdornmentLayerDefinition))]
        //[Name("TextAdornment")]
        //[Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        //private AdornmentLayerDefinition editorAdornmentLayer;

        [ImportingConstructor]
        public TextViewCreationListener(ICompletionBroker completionBroker, IVsEditorAdaptersFactoryService adaptersFactory, IWorkspaceManager workspaceManager)
        {
            _completionBroker = completionBroker;
            _adaptersFactory = adaptersFactory;
            _workspaceManager = workspaceManager;
        }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = _adaptersFactory.GetWpfTextView(textViewAdapter);

            CompletionController completion = new CompletionController(view, _completionBroker);
            textViewAdapter.AddCommandFilter(completion, out IOleCommandTarget completionNext);
            completion.Next = completionNext;

            // Command Filter for GoToDefinition in .csproj files
            GotoDefinitionController gotoDefinition = GotoDefinitionController.CreateAndRegister(view, _workspaceManager, textViewAdapter);
        }
    }
}
