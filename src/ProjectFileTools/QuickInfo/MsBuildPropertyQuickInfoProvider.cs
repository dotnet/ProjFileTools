using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.Completion;
using ProjectFileTools.Helpers;
using ProjectFileTools.MSBuild;

namespace ProjectFileTools.QuickInfo
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Project file MSBuild Quick Info Controller")]
    [ContentType("XML")]
    internal class MsBuildPropertyQuickInfoProvider : IAsyncQuickInfoSourceProvider
    {
        private readonly IWorkspaceManager _workspaceManager;

        [ImportingConstructor]
        public MsBuildPropertyQuickInfoProvider(IWorkspaceManager workspaceManager)
        {
            _workspaceManager = workspaceManager;
        }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new MsBuildPropertyQuickInfoSource(_workspaceManager);
        }
    }

    internal class MsBuildPropertyQuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly IWorkspaceManager _workspaceManager;

        public MsBuildPropertyQuickInfoSource(IWorkspaceManager workspaceManager)
        {
            _workspaceManager = workspaceManager;
        }

        public void Dispose()
        {            
        }

        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            if (!session.TextView.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDoc))
            {
                return Task.FromResult<QuickInfoItem>(null);
            }

            SnapshotPoint? triggerPoint = session.GetTriggerPoint(session.TextView.TextSnapshot);

            if (triggerPoint == null)
            {
                return Task.FromResult<QuickInfoItem>(null);
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
                            return Task.FromResult(new QuickInfoItem(target, evaluatedValue));
                        }
                        catch (Exception ex)
                        {
                            Debug.Fail(ex.ToString());
                        }
                    }
                    else
                    {
                        evaluatedValue = $"Value(s):\n    {string.Join("\n    ", evaluatedValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))}";
                        return Task.FromResult(new QuickInfoItem(target, evaluatedValue));
                    }
                }
            }

            return Task.FromResult<QuickInfoItem>(null);
        }
    }
}
