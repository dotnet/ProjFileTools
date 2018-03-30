using System;
using System.Collections.Generic;
using System.Net.Http;
using EnvDTE;
using FarTestProvider;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using ProjectFileTools.Completion;
using ProjectFileTools.MSBuild;
using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools
{
    internal class GotoDefinitionController : IOleCommandTarget
    {
        private readonly Guid _vSStd97CmdIDGuid;
        private readonly IWorkspaceManager _workspaceManager;

        private GotoDefinitionController(IWpfTextView textview, IWorkspaceManager workspaceManager)
        {
            TextView = textview;
            _vSStd97CmdIDGuid = new Guid("5EFC7975-14BC-11CF-9B2B-00AA00573819");
            _workspaceManager = workspaceManager;
        }

        public IWpfTextView TextView { get; }

        public IOleCommandTarget Next { get; private set; }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == _vSStd97CmdIDGuid && cCmds == (uint)VSConstants.VSStd97CmdID.GotoDefn)
            {
                prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
                return VSConstants.S_OK;
            }

            return Next?.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText) ?? (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        private static bool PackageExistsOnNuGet(string packageName, string version, out string url)
        {
            string packageAndVersionUrl = $"https://www.nuget.org/packages/{packageName}/{version}/";
            string packageUrl = $"https://www.nuget.org/packages/{packageName}/";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = null;

                    ThreadHelper.JoinableTaskFactory.Run(async () => response = await client.GetAsync(packageAndVersionUrl));

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        url = packageAndVersionUrl;
                        return true;
                    }
                    else
                    {
                        ThreadHelper.JoinableTaskFactory.Run(async () => response = await client.GetAsync(packageAndVersionUrl));

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            url = packageUrl;
                            return true;
                        }
                    }
                }
            }
            catch { }

            url = null;
            return false;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (pguidCmdGroup == _vSStd97CmdIDGuid && nCmdID == (uint)VSConstants.VSStd97CmdID.GotoDefn)
            {
                TextView.TextBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out ITextDocument textDoc);

                if (PackageCompletionSource.IsInRangeForPackageCompletion(TextView.TextSnapshot, TextView.Caret.Position.BufferPosition.Position, out Span targetSpan, out string packageName, out string packageVersion, out string completionType))
                {
                    if(PackageExistsOnNuGet(packageName, packageVersion, out string url))
                    {
                        System.Diagnostics.Process.Start(url);
                        return VSConstants.S_OK;
                    }
                }

                IWorkspace workspace = _workspaceManager.GetWorkspace(textDoc.FilePath);
                List<Definition> definitions = workspace.ResolveDefinition(textDoc.FilePath, TextView.TextSnapshot.GetText(), TextView.Caret.Position.BufferPosition.Position);

                if (definitions.Count == 1)
                {
                    DTE dte = ServiceUtil.DTE;
                    dte.MainWindow.Activate();

                    using (var state = new NewDocumentStateScope(__VSNEWDOCUMENTSTATE.NDS_Provisional, Guid.Parse(ProjectFileToolsPackage.PackageGuidString)))
                    {
                        Window w = dte.ItemOperations.OpenFile(definitions[0].File, EnvDTE.Constants.vsViewKindTextView);

                        if (definitions[0].Line.HasValue)
                        {
                            ((TextSelection)dte.ActiveDocument.Selection).GotoLine(definitions[0].Line.Value, true);
                        }
                    }

                    return VSConstants.S_OK;
                }

                else if (definitions.Count > 1)
                {
                    IFindAllReferencesService farService = ServiceUtil.GetService<SVsFindAllReferences, IFindAllReferencesService>();
                    FarDataSource dataSource = new FarDataSource(1);
                    dataSource.Snapshots[0] = new FarDataSnapshot(definitions);

                    IFindAllReferencesWindow farWindow = farService.StartSearch(definitions[0].Type);
                    ITableManager _farManager = farWindow.Manager;
                    _farManager.AddSource(dataSource);

                    dataSource.Sink.IsStable = false;
                    dataSource.Sink.AddSnapshot(dataSource.Snapshots[0]);
                    dataSource.Sink.IsStable = true;

                    return VSConstants.S_OK;
                }

            }

            return Next?.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut) ?? (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        internal static GotoDefinitionController CreateAndRegister(IWpfTextView textview, IWorkspaceManager workspaceManager, IVsTextView textViewAdapter)
        {
            GotoDefinitionController gotoDefinition = new GotoDefinitionController(textview, workspaceManager);
            textViewAdapter.AddCommandFilter(gotoDefinition, out IOleCommandTarget gotoDefinitionNext);
            gotoDefinition.Next = gotoDefinitionNext;
            return gotoDefinition;
        }
    }
}
