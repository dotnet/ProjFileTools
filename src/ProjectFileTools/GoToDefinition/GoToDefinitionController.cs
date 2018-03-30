using System;
using System.Collections.Generic;
using System.IO;
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
using ProjectFileTools.Helpers;
using ProjectFileTools.MSBuild;
using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools
{
    internal class GotoDefinitionController : IOleCommandTarget
    {
        private readonly Guid _vSStd97CmdIDGuid;
        private readonly Guid _vSStd2kCmdIDGuid;
        private readonly IWorkspaceManager _workspaceManager;
        private const long EditProjectFileCommandId = 1632;

        private GotoDefinitionController(IWpfTextView textview, IWorkspaceManager workspaceManager)
        {
            TextView = textview;
            _vSStd97CmdIDGuid = new Guid(VSConstants.CMDSETID.StandardCommandSet97_string);
            _vSStd2kCmdIDGuid = new Guid(VSConstants.CMDSETID.StandardCommandSet2K_string);
            _workspaceManager = workspaceManager;
        }

        public IWpfTextView TextView { get; }

        public IOleCommandTarget Next { get; private set; }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == _vSStd97CmdIDGuid && cCmds > 0 && prgCmds[0].cmdID == (uint)VSConstants.VSStd97CmdID.GotoDefn)
            {
                prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                return VSConstants.S_OK;
            }

            if (pguidCmdGroup == _vSStd97CmdIDGuid && cCmds > 0 && prgCmds[0].cmdID == (uint)VSConstants.VSStd97CmdID.FindReferences)
            {
                TextView.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDoc);
                XmlInfo info = XmlTools.GetXmlInfo(TextView.TextSnapshot, TextView.Caret.Position.BufferPosition.Position);
                if (info != null)
                {
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                    return VSConstants.S_OK;
                }
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
            if (pguidCmdGroup == _vSStd97CmdIDGuid && nCmdID == (uint)VSConstants.VSStd97CmdID.FindReferences)
            {
                TextView.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDoc);

                XmlInfo info = XmlTools.GetXmlInfo(TextView.TextSnapshot, TextView.Caret.Position.BufferPosition.Position);
                IVsTextBuffer buffer;
                string thisFileName;
                IPersistFileFormat persistFile;
                IWorkspace workspace;

                if (info != null)
                {
                    if ((info.AttributeName == "Include" || info.AttributeName == "Update" || info.AttributeName == "Exclude" || info.AttributeName == "Remove")
                        && TextView.TextBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out buffer)
                        && (persistFile = buffer as IPersistFileFormat) != null
                        && VSConstants.S_OK == persistFile.GetCurFile(out thisFileName, out _))
                    {
                        string relativePath = info.AttributeValue;
                        workspace = _workspaceManager.GetWorkspace(textDoc.FilePath);
                        List<Definition> matchedItems = workspace.GetItems(relativePath);

                        if (matchedItems.Count == 1)
                        {
                            string absolutePath = Path.Combine(Path.GetDirectoryName(thisFileName), matchedItems[0].File);
                            FileInfo fileInfo = new FileInfo(absolutePath);

                            if (fileInfo.Exists)
                            {
                                ServiceUtil.DTE.ItemOperations.OpenFile(fileInfo.FullName);
                                return VSConstants.S_OK;
                            }
                        }

                        return ShowInFar("Files Matching Glob", matchedItems);
                    }
                }

            }
            else if (pguidCmdGroup == _vSStd97CmdIDGuid && nCmdID == (uint)VSConstants.VSStd97CmdID.GotoDefn)
            {
                TextView.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDoc);

                XmlInfo info = XmlTools.GetXmlInfo(TextView.TextSnapshot, TextView.Caret.Position.BufferPosition.Position);
                IVsTextBuffer buffer;
                string thisFileName;
                IPersistFileFormat persistFile;
                IWorkspace workspace;

                if (info != null)
                {
                    switch (info.TagName)
                    {
                        case "ProjectReference":
                            if (info.AttributeName == "Include"
                                && TextView.TextBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out buffer)
                                && (persistFile = buffer as IPersistFileFormat) != null
                                && VSConstants.S_OK == persistFile.GetCurFile(out thisFileName, out _))
                            {
                                string relativePath = info.AttributeValue;
                                workspace = _workspaceManager.GetWorkspace(textDoc.FilePath);
                                List<Definition> matchedItems = workspace.GetItems(relativePath);

                                if (matchedItems.Count == 1)
                                {
                                    string absolutePath = Path.Combine(Path.GetDirectoryName(thisFileName), matchedItems[0].File);
                                    FileInfo fileInfo = new FileInfo(absolutePath);
                                    if (fileInfo.Exists)
                                    {
                                        ServiceUtil.DTE.ItemOperations.OpenFile(fileInfo.FullName);
                                        return VSConstants.S_OK;
                                        //ServiceUtil.DTE.Commands.Raise(VSConstants.CMDSETID.StandardCommandSet2K_string, (int)EditProjectFileCommandId, ref unusedArgs, ref unusedArgs);
                                    }
                                }
                            }

                            break;
                        case "PackageReference":
                        case "DotNetCliToolReference":
                            if (PackageCompletionSource.TryGetPackageInfoFromXml(info, out string packageName, out string packageVersion) && PackageExistsOnNuGet(packageName, packageVersion, out string url))
                            {
                                System.Diagnostics.Process.Start(url);
                                return VSConstants.S_OK;
                            }
                            break;
                        default:
                            if ((info.AttributeName == "Include" || info.AttributeName == "Update" || info.AttributeName == "Exclude" || info.AttributeName == "Remove")
                                && TextView.TextBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out buffer)
                                && (persistFile = buffer as IPersistFileFormat) != null
                                && VSConstants.S_OK == persistFile.GetCurFile(out thisFileName, out _))
                            {
                                string relativePath = info.AttributeValue;
                                workspace = _workspaceManager.GetWorkspace(textDoc.FilePath);
                                List<Definition> matchedItems = workspace.GetItemProvenance(relativePath);

                                if (matchedItems.Count == 1)
                                {
                                    string absolutePath = Path.Combine(Path.GetDirectoryName(thisFileName), matchedItems[0].File);
                                    FileInfo fileInfo = new FileInfo(absolutePath);

                                    if (fileInfo.Exists)
                                    {
                                        ServiceUtil.DTE.ItemOperations.OpenFile(fileInfo.FullName);
                                        return VSConstants.S_OK;
                                    }
                                }

                                //Don't return the result of show in find all references, the caret may also be in a symbol,
                                //  where we'd also want to go to that definition
                                ShowInFar("Item Provenance", matchedItems);
                            }

                            break;

                    }
                }

                workspace = _workspaceManager.GetWorkspace(textDoc.FilePath);
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
                    return ShowInFar("Symbol Definition", definitions);
                }

            }

            return Next?.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut) ?? (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        private int ShowInFar(string title, List<Definition> definitions)
        {
            IFindAllReferencesService farService = ServiceUtil.GetService<SVsFindAllReferences, IFindAllReferencesService>();
            FarDataSource dataSource = new FarDataSource(1);
            dataSource.Snapshots[0] = new FarDataSnapshot(definitions);

            IFindAllReferencesWindow farWindow = farService.StartSearch(title);
            ITableManager _farManager = farWindow.Manager;
            _farManager.AddSource(dataSource);

            dataSource.Sink.IsStable = false;
            dataSource.Sink.AddSnapshot(dataSource.Snapshots[0]);
            dataSource.Sink.IsStable = true;

            return VSConstants.S_OK;
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
