using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace ProjectFileTools
{

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
}
