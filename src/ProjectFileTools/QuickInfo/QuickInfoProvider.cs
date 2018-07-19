using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.Completion;
using ProjectFileTools.Helpers;
using ProjectFileTools.MSBuild;
using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools.QuickInfo
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Project file tools Quick Info Controller")]
    [ContentType("XML")]
    internal class QuickInfoProvider : IAsyncQuickInfoSourceProvider
    {
        private readonly IPackageSearchManager _searchManager;
        private readonly IWorkspaceManager _workspaceManager;

        [ImportingConstructor]
        public QuickInfoProvider(IWorkspaceManager workspaceManager, IPackageSearchManager searchManager)
        {
            _searchManager = searchManager;
            _workspaceManager = workspaceManager;
        }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new MsBuildPropertyQuickInfoSource(_workspaceManager, _searchManager);
        }
    }

    internal class MsBuildPropertyQuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly IPackageSearchManager _searchManager;
        private readonly IWorkspaceManager _workspaceManager;

        public MsBuildPropertyQuickInfoSource(IWorkspaceManager workspaceManager, IPackageSearchManager searchManager)
        {
            _searchManager = searchManager;
            _workspaceManager = workspaceManager;
        }

        public void Dispose()
        {            
        }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            if (!session.TextView.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDoc))
            {
                return null;
            }

            SnapshotPoint? triggerPoint = session.GetTriggerPoint(session.TextView.TextSnapshot);

            if (triggerPoint == null)
            {
                return null;
            }

            int pos = triggerPoint.Value.Position;

            if (!PackageCompletionSource.IsInRangeForPackageCompletion(session.TextView.TextSnapshot, pos, out Span s, out string packageId, out string packageVersion, out string type))
            {
                XmlInfo info = XmlTools.GetXmlInfo(session.TextView.TextSnapshot, pos);

                if (info != null)
                {
                    IWorkspace workspace = workspace = _workspaceManager.GetWorkspace(textDoc.FilePath);
                    string evaluatedValue = workspace.GetEvaluatedPropertyValue(info.AttributeValue);
                    ITrackingSpan target = session.TextView.TextSnapshot.CreateTrackingSpan(new Span(info.AttributeValueStart, info.AttributeValueLength), SpanTrackingMode.EdgeNegative);

                    if (info.AttributeName == "Condition")
                    {
                        try
                        {
                            bool isTrue = workspace.EvaluateCondition(info.AttributeValue);
                            evaluatedValue = $"Expanded value: {evaluatedValue}\nEvaluation result: {isTrue}";
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            return new QuickInfoItem(target, evaluatedValue);
                        }
                        catch (Exception ex)
                        {
                            Debug.Fail(ex.ToString());
                        }
                    }
                    else
                    {
                        evaluatedValue = $"Value(s):\n    {string.Join("\n    ", evaluatedValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))}";
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        return new QuickInfoItem(target, evaluatedValue);
                    }
                }
            }
            else
            {
                string text = session.TextView.TextBuffer.CurrentSnapshot.GetText();
                int targetFrameworkElementStartIndex = text.IndexOf("<TargetFramework>", StringComparison.OrdinalIgnoreCase);
                int targetFrameworksElementStartIndex = text.IndexOf("<TargetFrameworks>", StringComparison.OrdinalIgnoreCase);
                string tfm = "netcoreapp1.0";

                if (targetFrameworksElementStartIndex > -1)
                {
                    int closeTfms = text.IndexOf("</TargetFrameworks>", targetFrameworksElementStartIndex);
                    int realStart = targetFrameworksElementStartIndex + "<TargetFrameworks>".Length;
                    string allTfms = text.Substring(realStart, closeTfms - realStart);
                    tfm = allTfms.Split(';')[0];
                }
                else if (targetFrameworkElementStartIndex > -1)
                {
                    int closeTfm = text.IndexOf("</TargetFramework>", targetFrameworkElementStartIndex);
                    int realStart = targetFrameworkElementStartIndex + "<TargetFramework>".Length;
                    tfm = text.Substring(realStart, closeTfm - realStart);
                }

                ITrackingSpan applicableToSpan = session.TextView.TextBuffer.CurrentSnapshot.CreateTrackingSpan(s, SpanTrackingMode.EdgeInclusive);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return new QuickInfoItem(applicableToSpan, new PackageInfoControl(packageId, packageVersion, tfm, _searchManager));
            }

            return null;
        }
    }
}
