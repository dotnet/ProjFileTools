using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using PackageFeedManager;

namespace ProjPackageIntellisense
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("XML")]
    [TagType(typeof(IntraTextAdornmentTag))]
    internal class PackageGlyphTaggerProvider : IViewTaggerProvider
    {
        private readonly IBufferTagAggregatorFactoryService _bufferTagAggregatorFactoryService;
        private readonly IPackageSearchManager _searchManager;

        [ImportingConstructor]
        public PackageGlyphTaggerProvider(IPackageSearchManager searchManager, IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService)
        {
            _searchManager = searchManager;
            _bufferTagAggregatorFactoryService = bufferTagAggregatorFactoryService;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer textBuffer)
            where T : ITag
        {
            IIntraTextAdornmentFactory<PackageGlyphTag> factory = new PackageGlyphTagFactory(_searchManager);
            return IntraTextAdornmentTagger<PackageGlyphTag>.GetOrCreate(textView, textBuffer, factory) as ITagger<T>;
        }
    }

    internal class IntraTextAdornmentTagger<TTag> : ITagger<IntraTextAdornmentTag>
        where TTag: IntraTextAdornmentTag
    {
        private readonly ITextBuffer _textBuffer;
        private readonly ITextView _textView;
        private readonly Dictionary<NormalizedSnapshotSpanCollection, TTag> _map = new Dictionary<NormalizedSnapshotSpanCollection, TTag>();
        private static readonly string PropertyName = typeof(TTag).Name;
        private readonly IIntraTextAdornmentFactory<TTag> _factory;

        public IntraTextAdornmentTagger(ITextView textView, ITextBuffer textBuffer, IIntraTextAdornmentFactory<TTag> factory)
        {
            _factory = factory;
            _textView = textView;
            _textBuffer = textBuffer;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public static ITagger<ITag> GetOrCreate(ITextView textView, ITextBuffer textBuffer, IIntraTextAdornmentFactory<TTag> factory)
        {
            if(!textBuffer.Properties.TryGetProperty(PropertyName, out IntraTextAdornmentTagger<TTag> existingTagger))
            {
                existingTagger = new IntraTextAdornmentTagger<TTag>(textView, textBuffer, factory);
                textBuffer.Properties.AddProperty(PropertyName, existingTagger);
            }

            return existingTagger;
        }

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if(spans == null || spans.Count == 0)
            {
                yield break;
            }

            ITextSnapshot snapshot = spans[0].Snapshot;
            IEnumerable<SnapshotSpan> translatedSpans = spans.Select(span => span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive));
            NormalizedSnapshotSpanCollection normalizedSpans = new NormalizedSnapshotSpanCollection(translatedSpans);
            // Grab the adornments.
            IEnumerable<TagSpan<TTag>> tagSpans = GetAdornmentTagsOnSnapshot(normalizedSpans);

            foreach (TagSpan<TTag> tagSpan in tagSpans)
            {
                yield return tagSpan;
            }
        }

        private IEnumerable<TagSpan<TTag>> GetAdornmentTagsOnSnapshot(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            ITextSnapshot snapshot = spans[0].Snapshot;
            HashSet<NormalizedSnapshotSpanCollection> updates = new HashSet<NormalizedSnapshotSpanCollection>();

            foreach(NormalizedSnapshotSpanCollection entry in _map.Keys)
            {
                if (spans.IntersectsWith(entry[0].TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeInclusive)))
                {
                    updates.Add(entry);
                }
            }

            foreach(SnapshotSpan span in spans)
            {
                TTag existingTag = null;
                NormalizedSnapshotSpanCollection existingSpan = null;
                foreach(NormalizedSnapshotSpanCollection update in updates)
                {
                    if(update[0].TranslateTo(span.Snapshot, SpanTrackingMode.EdgeInclusive).IntersectsWith(span))
                    {
                        existingSpan = update;
                        existingTag = _map[update];
                        updates.Remove(update);
                        break;
                    }
                }

                SnapshotSpan calc = span;
                if (existingSpan != null)
                {
                    calc = existingSpan[0];
                    _map.Remove(existingSpan);
                }

                if(_factory.TryCreateOrUpdate(_textView, calc, existingTag, out TagSpan<TTag> targetTag))
                {
                    _map[new NormalizedSnapshotSpanCollection(targetTag.Span)] = targetTag.Tag;
                    targetTag.Tag.Adornment.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    yield return targetTag;
                }
            }

            foreach(NormalizedSnapshotSpanCollection update in updates)
            {
                _map.Remove(update);
            }
        }
    }

    internal interface IIntraTextAdornmentFactory<T>
        where T : IntraTextAdornmentTag
    {
        /// <summary>
        /// Returns true if the tag should exist, false if any existing tag should be destroyed
        /// </summary>
        /// <param name="span">The span the tag would be applicable to</param>
        /// <param name="existingTag">The existing tag intersecting the span</param>
        /// <param name="tag">The tag that should apply to the span</param>
        /// <returns>true if the tag should exist, false otherwise</returns>
        bool TryCreateOrUpdate(ITextView textView, SnapshotSpan span, T existingTag, out TagSpan<T> tag);
    }

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

    internal class PackageGlyphTag : IntraTextAdornmentTag
    {
        public Image PackageIcon => (Image)((Border)Adornment).Child;
        public Border Wrapper => (Border)Adornment;

        public PackageGlyphTag(ITextView textView) 
            : base(new Border { Child = new Image() }, OnRemoved, null)
        {
            Wrapper.DataContext = Wrapper;
            Wrapper.Height = textView.LineHeight;
            Wrapper.SetBinding(FrameworkElement.WidthProperty, "ActualHeight");
        }

        private static void OnRemoved(object tag, UIElement element)
        {
        }

        public PackageGlyphTag(PositionAffinity? affinity, ITextView textView) 
            : base(new Border { Child = new Image() }, OnRemoved, affinity)
        {
            Wrapper.DataContext = Wrapper;
            Wrapper.Height = textView.LineHeight;
            Wrapper.SetBinding(FrameworkElement.WidthProperty, "ActualHeight");
        }

        public PackageGlyphTag(double? topSpace, double? baseline, double? textHeight, double? bottomSpace, PositionAffinity? affinity, IWpfTextView textView) 
            : base(new Border { Child = new Image() }, OnRemoved, topSpace, baseline, textHeight, bottomSpace, affinity)
        {
            Wrapper.DataContext = Wrapper;
            Wrapper.Height = textView.LineHeight;
            Wrapper.SetBinding(FrameworkElement.WidthProperty, "ActualHeight");
        }
    }
}
