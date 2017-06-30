using System;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using ProjectFileTools.MSBuild;

namespace ProjectFileTools
{
    internal class GotoDefinitionController : IOleCommandTarget
    {
        private readonly WorkspaceManager _workspaceManager;

        internal GotoDefinitionController(IWpfTextView textview, WorkspaceManager workspaceManager)
        {
            TextView = textview;
            _workspaceManager = workspaceManager;
        }

        public IWpfTextView TextView { get; private set; }

        public IOleCommandTarget Next { get; set; }
        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                if (cCmds == (uint)VSConstants.VSStd97CmdID.GotoDefn)
                {
                    prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
                    return VSConstants.S_OK;
                }
            }

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                if (nCmdID == (uint)VSConstants.VSStd97CmdID.GotoDefn)
                {
                    TextView.TextBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out ITextDocument textDoc);
                    Workspace workspace = _workspaceManager.GetWorkspace(textDoc.FilePath);
                    string importedPath = workspace.ResolveDefinition(textDoc.FilePath, TextView.TextSnapshot.GetText(), TextView.Caret.Position.BufferPosition.Position);

                    if (importedPath != null)
                    {
                        DTE dte = (DTE)ServiceProvider.GlobalProvider.GetService(typeof(DTE));
                        dte.MainWindow.Activate();

                        using (NewDocumentStateScope state = new NewDocumentStateScope(__VSNEWDOCUMENTSTATE.NDS_Provisional, Guid.Parse(ProjectFileToolsPackage.PackageGuidString)))
                        {
                            EnvDTE.Window w = dte.ItemOperations.OpenFile(importedPath, EnvDTE.Constants.vsViewKindTextView);
                        }
                        return VSConstants.S_OK;
                    }
                }
            }
            return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
    }
}
