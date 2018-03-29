using System;
using System.Linq;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using ProjectFileTools.Completion;
using ProjectFileTools.NuGetSearch;
using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools.Adornments
{
    internal class PackageGlyphTagFactory : IIntraTextAdornmentFactory<PackageGlyphTag>
    {
        private readonly IPackageSearchManager _searchManager;

        public PackageGlyphTagFactory(IPackageSearchManager searchManager)
        {
            _searchManager = searchManager;
        }

        public bool TryCreateOrUpdate(ITextView textView, SnapshotSpan span, PackageGlyphTag existingTag, out TagSpan<PackageGlyphTag> tag, out Span valueSpan)
        {
            if (!PackageCompletionSource.IsInRangeForPackageCompletion(span.Snapshot, span.Start, out valueSpan, out string name, out string ver, out string type))
            {
                tag = null;
                return false;
            }

            double? lineHeight;
            try
            {
                lineHeight = textView.LineHeight;
            }
            catch { lineHeight = null; }

            string text = span.Snapshot.GetText();
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

            PackageGlyphTag t = existingTag ?? new PackageGlyphTag(PositionAffinity.Predecessor, textView);
            t.PackageIcon.Source = WpfUtil.MonikerToBitmap(KnownMonikers.NuGet, (int)(lineHeight ?? 16));
            IssueIconQuery(name, ver, tfm, t);
            if (lineHeight.HasValue)
            {
                t.Wrapper.Height = lineHeight.Value;
            }
            tag = new TagSpan<PackageGlyphTag>(new SnapshotSpan(span.Snapshot, Span.FromBounds(valueSpan.Start, valueSpan.Start)), t);
            return true;
        }

        private void IssueIconQuery(string name, string ver, string tfm, PackageGlyphTag t)
        {
            IPackageFeedSearchJob<IPackageInfo> job = _searchManager.SearchPackageInfo(name, ver, tfm);
            EventHandler handler = null;
            string currentIcon = null;

            handler = (o, e) =>
            {
                if (job.IsCancelled)
                {
                    job.Updated -= handler;
                    job = _searchManager.SearchPackageInfo(name, ver, tfm);
                    job.Updated += handler;
                    handler(o, e);
                    return;
                }

                IPackageInfo package = job.Results.OrderByDescending(x => SemanticVersion.Parse(x.Version)).FirstOrDefault();

                if (package != null)
                {
                    if (string.IsNullOrEmpty(package.IconUrl) || package.IconUrl == currentIcon)
                    {
                        return;
                    }

                    if (Uri.TryCreate(package.IconUrl, UriKind.Absolute, out Uri iconUri))
                    {
                        string proposedIcon = package.IconUrl;
                        ThreadHelper.JoinableTaskFactory.Run(async () =>
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            if(currentIcon == proposedIcon)
                            {
                                return;
                            }

                            currentIcon = proposedIcon;
                            BitmapImage img = new BitmapImage(iconUri);
                            t.PackageIcon.Source = img;
                        });
                    }
                }
            };

            job.Updated += handler;
            handler(null, EventArgs.Empty);
        }
    }
}
