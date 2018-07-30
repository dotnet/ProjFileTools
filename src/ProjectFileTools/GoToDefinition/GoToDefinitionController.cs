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

namespace ProjectFileTools
{
    internal class GotoDefinitionController : IOleCommandTarget
    {
        private const long EditProjectFileCommandId = 1632;
        private readonly Guid _vSStd2kCmdIDGuid;
        private readonly Guid _vSStd97CmdIDGuid;
        private readonly IWorkspaceManager _workspaceManager;
        private readonly IReadOnlyDictionary<string, Func<XmlInfo, ITextDocument, ITextView, IWorkspaceManager, int?>> GoToDefinitionAttributeHandlers = new Dictionary<string, Func<XmlInfo, ITextDocument, ITextView, IWorkspaceManager, int?>>(StringComparer.Ordinal)
        {
            { "ProjectReference", HandleGoToDefinitionOnProjectReference },
            { "PackageReference", HandleGoToDefinitionOnNuGetPackage },
            { "DotNetCliToolReference", HandleGoToDefinitionOnNuGetPackage }
        };

        private GotoDefinitionController(IWpfTextView textview, IWorkspaceManager workspaceManager)
        {
            TextView = textview;
            _vSStd97CmdIDGuid = new Guid(VSConstants.CMDSETID.StandardCommandSet97_string);
            _vSStd2kCmdIDGuid = new Guid(VSConstants.CMDSETID.StandardCommandSet2K_string);
            _workspaceManager = workspaceManager;
        }

        public IOleCommandTarget Next { get; private set; }

        public IWpfTextView TextView { get; }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (pguidCmdGroup == _vSStd97CmdIDGuid && nCmdID == (uint)VSConstants.VSStd97CmdID.FindReferences)
            {
                int? result = HandleFindAllReferences();

                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            else if (pguidCmdGroup == _vSStd97CmdIDGuid && nCmdID == (uint)VSConstants.VSStd97CmdID.GotoDefn)
            {
                int? result = HandleGoToDefinition();

                if (result.HasValue)
                {
                    return result.Value;
                }
            }

            return Next?.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut) ?? (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }

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
                if (info?.AttributeName != null)
                {
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                    return VSConstants.S_OK;
                }
            }

            return Next?.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText) ?? (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        internal static GotoDefinitionController CreateAndRegister(IWpfTextView textview, IWorkspaceManager workspaceManager, IVsTextView textViewAdapter)
        {
            GotoDefinitionController gotoDefinition = new GotoDefinitionController(textview, workspaceManager);
            textViewAdapter.AddCommandFilter(gotoDefinition, out IOleCommandTarget gotoDefinitionNext);
            gotoDefinition.Next = gotoDefinitionNext;
            return gotoDefinition;
        }

        private static int? FallbackAttributeCompletionHandler(XmlInfo info, ITextDocument textDoc, ITextView textView, IWorkspaceManager workspaceManager)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if ((info.AttributeName == "Include" || info.AttributeName == "Update" || info.AttributeName == "Exclude" || info.AttributeName == "Remove"))
            {
                string relativePath = info.AttributeValue;
                IWorkspace workspace = workspaceManager.GetWorkspace(textDoc.FilePath);
                List<Definition> matchedItems = workspace.GetItemProvenance(relativePath);

                if (matchedItems.Count == 1)
                {
                    string absolutePath = Path.Combine(Path.GetDirectoryName(textDoc.FilePath), matchedItems[0].File);
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

            return null;
        }

        private static int? HandleGoToDefinitionOnNuGetPackage(XmlInfo info, ITextDocument textDoc, ITextView textView, IWorkspaceManager workspaceManager)
        {
            if (PackageCompletionSource.TryGetPackageInfoFromXml(info, out string packageName, out string packageVersion) && PackageExistsOnNuGet(packageName, packageVersion, out string url))
            {
                System.Diagnostics.Process.Start(url);
                return VSConstants.S_OK;
            }

            return null;
        }

        private static int? HandleGoToDefinitionOnProjectReference(XmlInfo info, ITextDocument textDoc, ITextView textView, IWorkspaceManager workspaceManager)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (info.AttributeName == "Include")
            {
                string relativePath = info.AttributeValue;
                IWorkspace workspace = workspaceManager.GetWorkspace(textDoc.FilePath);
                List<Definition> matchedItems = workspace.GetItems(relativePath);

                if (matchedItems.Count == 1)
                {
                    string absolutePath = Path.Combine(Path.GetDirectoryName(textDoc.FilePath), matchedItems[0].File);
                    FileInfo fileInfo = new FileInfo(absolutePath);
                    if (fileInfo.Exists)
                    {
                        ServiceUtil.DTE.ItemOperations.OpenFile(fileInfo.FullName);
                        return VSConstants.S_OK;
                        //ServiceUtil.DTE.Commands.Raise(VSConstants.CMDSETID.StandardCommandSet2K_string, (int)EditProjectFileCommandId, ref unusedArgs, ref unusedArgs);
                    }
                }
            }

            return null;
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

        private static int ShowInFar(string title, List<Definition> definitions)
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

        private int? HandleAttributeCompletionResult(ITextDocument textDoc, XmlInfo info)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (GoToDefinitionAttributeHandlers.TryGetValue(info.TagName, out Func<XmlInfo, ITextDocument, ITextView, IWorkspaceManager, int?> handler))
            {
                return handler(info, textDoc, TextView, _workspaceManager);
            }

            return FallbackAttributeCompletionHandler(info, textDoc, TextView, _workspaceManager);
        }

        private int? HandleFindAllReferences()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!TextView.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDoc))
            {
                return null;
            }

            XmlInfo info = XmlTools.GetXmlInfo(TextView.TextSnapshot, TextView.Caret.Position.BufferPosition.Position);

            if (info != null)
            {
                if (info.AttributeName == "Include" || info.AttributeName == "Update" || info.AttributeName == "Exclude" || info.AttributeName == "Remove")
                {
                    string relativePath = info.AttributeValue;
                    IWorkspace workspace = _workspaceManager.GetWorkspace(textDoc.FilePath);
                    List<Definition> matchedItems = workspace.GetItems(relativePath);

                    if (matchedItems.Count == 1)
                    {
                        string absolutePath = Path.Combine(Path.GetDirectoryName(textDoc.FilePath), matchedItems[0].File);
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

            return null;
        }

        private int? HandleGoToDefinition()
        {
            TextView.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDoc);

            XmlInfo info = XmlTools.GetXmlInfo(TextView.TextSnapshot, TextView.Caret.Position.BufferPosition.Position);

            if (info != null)
            {
                int? attributeCompletionResult = HandleAttributeCompletionResult(textDoc, info);

                if (attributeCompletionResult.HasValue)
                {
                    return attributeCompletionResult.Value;
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
                return ShowInFar("Symbol Definition", definitions);
            }

            return null;
        }
    }
}
