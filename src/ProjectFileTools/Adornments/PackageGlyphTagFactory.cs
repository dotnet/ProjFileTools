using System;
using System.Linq;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using PackageFeedManager;

namespace ProjectFileTools
{

    internal class PackageGlyphTagFactory : IIntraTextAdornmentFactory<PackageGlyphTag>
    {
        private readonly IPackageSearchManager _searchManager;

        public PackageGlyphTagFactory(IPackageSearchManager searchManager)
        {
            _searchManager = searchManager;
        }

        public bool TryCreateOrUpdate(ITextView textView, SnapshotSpan span, PackageGlyphTag existingTag, out TagSpan<PackageGlyphTag> tag)
        {
            if(!PackageCompletionSource.IsInRangeForPackageCompletion(span.Snapshot, span.Start, out Span s, out string name, out string ver, out string type))
            {
                tag = null;
                return false;
            }

            PackageGlyphTag t = existingTag ?? new PackageGlyphTag(PositionAffinity.Predecessor, textView);
            t.PackageIcon.Source = WpfUtil.MonikerToBitmap(KnownMonikers.NuGet, (int)textView.LineHeight);
            IssueIconQuery(name, ver, "netcoreapp1.0", t);
            t.Wrapper.Height = textView.LineHeight;
            tag = new TagSpan<PackageGlyphTag>(new SnapshotSpan(span.Snapshot, Span.FromBounds(s.Start, s.Start)), t);
            return true;
        }

        private void IssueIconQuery(string name, string ver, string tfm, PackageGlyphTag t)
        {
            IPackageFeedSearchJob<IPackageInfo> job = _searchManager.SearchPackageInfo(name, ver, tfm);
            bool firstRun = true;
            EventHandler handler = null;

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
                    t.PackageIcon.Dispatcher.Invoke(() =>
                    {
                        if (!firstRun && !t.PackageIcon.IsVisible)
                        {
                            return;
                        }

                        firstRun = false;
                        if (!string.IsNullOrEmpty(package.IconUrl) && Uri.TryCreate(package.IconUrl, UriKind.Absolute, out Uri iconUri))
                        {
                            t.PackageIcon.Source = new BitmapImage(iconUri);
                        }
                    });
                }
            };

            job.Updated += handler;
            handler(null, EventArgs.Empty);
        }
    }
}
