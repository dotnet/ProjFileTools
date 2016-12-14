using System;
using System.Linq;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using PackageFeedManager;
using ProjectFileTools.Status;

namespace ProjectFileTools
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

            PackageGlyphTag t = existingTag ?? new PackageGlyphTag(PositionAffinity.Predecessor, textView);
            t.PackageIcon.Source = WpfUtil.MonikerToBitmap(KnownMonikers.NuGet, (int)(lineHeight ?? 16));
            IssueIconQuery(name, ver, "netcoreapp1.0", t);
            if (lineHeight.HasValue)
            {
                t.Wrapper.Height = lineHeight.Value;
            }
            tag = new TagSpan<PackageGlyphTag>(new SnapshotSpan(span.Snapshot, Span.FromBounds(valueSpan.Start, valueSpan.Start)), t);
            return true;
        }

        private void IssueIconQuery(string name, string ver, string tfm, PackageGlyphTag t)
        {
            IPackageFeedSearchJob<IPackageInfo> job = _searchManager.SearchPackageInfo(name, ver, tfm, StatusManager.Instance);
            EventHandler handler = null;
            string currentIcon = null;

            handler = (o, e) =>
            {
                if (job.IsCancelled)
                {
                    job.Updated -= handler;
                    job = _searchManager.SearchPackageInfo(name, ver, tfm, StatusManager.Instance);
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
                        t.PackageIcon.Dispatcher.Invoke(() =>
                        {
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
