using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.Completion;
using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools.QuickInfo
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Xml Package Quick Info Controller")]
    [ContentType("XML")]
    internal class PackageQuickInfoProvider : IAsyncQuickInfoSourceProvider
    {
        private readonly IPackageSearchManager _searchManager;

        [ImportingConstructor]
        public PackageQuickInfoProvider(IPackageSearchManager searchManager)
        {
            _searchManager = searchManager;
        }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            string text = textBuffer.CurrentSnapshot.GetText();
            bool isCore = text.IndexOf("Microsoft.Net.Sdk", StringComparison.OrdinalIgnoreCase) > -1;

            if (isCore)
            {
                return new PackageQuickInfoSource(_searchManager);
            }

            return null;
        }
    }

    internal class PackageQuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly IPackageSearchManager _searchManager;

        public PackageQuickInfoSource(IPackageSearchManager searchManager)
        {
            _searchManager = searchManager;
        }

        public void Dispose()
        {
        }

        public void AugmentQuickInfoSession(IAsyncQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
        {
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(session.TextView.TextSnapshot);

            if (!triggerPoint.HasValue)
            {
                applicableToSpan = null;
                return;
            }

            int pos = triggerPoint.Value.Position;
            if (PackageCompletionSource.IsInRangeForPackageCompletion(session.TextView.TextSnapshot, pos, out Span s, out string packageId, out string packageVersion, out string type))
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

                applicableToSpan = session.TextView.TextBuffer.CurrentSnapshot.CreateTrackingSpan(s, SpanTrackingMode.EdgeInclusive);
                quickInfoContent.Add(new PackageInfoControl(packageId, packageVersion, tfm, _searchManager));
                return;
            }

            applicableToSpan = null;
        }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(session.TextView.TextSnapshot);

            if (!triggerPoint.HasValue)
            {
                return null;
            }

            int pos = triggerPoint.Value.Position;
            if (PackageCompletionSource.IsInRangeForPackageCompletion(session.TextView.TextSnapshot, pos, out Span s, out string packageId, out string packageVersion, out string type))
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
                return new QuickInfoItem(applicableToSpan, new PackageInfoControl(packageId, packageVersion, tfm, _searchManager));
            }

            return null;
        }
    }
}
