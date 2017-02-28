using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace ProjectFileTools.Adornments
{
    internal class IntraTextAdornmentTagger<TTag> : ITagger<IntraTextAdornmentTag>
        where TTag : IntraTextAdornmentTagBase
    {
        private readonly ITextBuffer _textBuffer;
        private readonly ITextView _textView;
        private readonly Dictionary<SnapshotSpan, TTag> _map = new Dictionary<SnapshotSpan, TTag>();
        private static readonly string PropertyName = typeof(TTag).Name;
        private readonly IIntraTextAdornmentFactory<TTag> _factory;
        private readonly string _tagName;

        public IntraTextAdornmentTagger(ITextView textView, ITextBuffer textBuffer, IIntraTextAdornmentFactory<TTag> factory, string tagName)
        {
            _factory = factory;
            _textView = textView;
            _textBuffer = textBuffer;
            _tagName = tagName;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public static ITagger<ITag> GetOrCreate(ITextView textView, ITextBuffer textBuffer, IIntraTextAdornmentFactory<TTag> factory, string tagName)
        {
            if (!textBuffer.Properties.TryGetProperty(PropertyName + "_" + tagName, out IntraTextAdornmentTagger<TTag> existingTagger))
            {
                existingTagger = new IntraTextAdornmentTagger<TTag>(textView, textBuffer, factory, tagName);
                textBuffer.Properties.AddProperty(PropertyName + "_" + tagName, existingTagger);
            }

            return existingTagger;
        }

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0)
            {
                yield break;
            }

            ITextSnapshot snapshot = spans[0].Snapshot;
            IEnumerable<SnapshotSpan> translatedSpans = spans.Select(span => span.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive));
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
            ITextSnapshot snapshot = spans[0].Snapshot;
            string spanText = snapshot.GetText();
            int lastOpen = spanText.IndexOf($"<{_tagName}", StringComparison.Ordinal);

            while (lastOpen > -1)
            {
                int include = spanText.IndexOf("Include", lastOpen, StringComparison.Ordinal);

                if (include > -1)
                {
                    int quote = spanText.IndexOf('"', lastOpen);

                    if (quote > -1)
                    {
                        SnapshotSpan s = new SnapshotSpan(snapshot, new Span(quote + 1, 1));
                        KeyValuePair<SnapshotSpan, TTag> existingTag = _map.FirstOrDefault(x => x.Key.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive).IntersectsWith(s));
                        if (_factory.TryCreateOrUpdate(_textView, s, existingTag.Value, out TagSpan<TTag> targetTag, out Span valueSpan))
                        {
                            if (existingTag.Key != null)
                            {
                                if (!existingTag.Key.Equals(targetTag.Span))
                                {
                                    _map.Remove(existingTag.Key);
                                    _map[targetTag.Span] = targetTag.Tag;
                                }
                            }
                            else
                            {
                                _map[targetTag.Span] = targetTag.Tag;
                            }

                            if (spans.Any(x => x.OverlapsWith(valueSpan)))
                            {
                                targetTag.Tag.UpdateLayout();
                                yield return targetTag;
                            }
                        }
                    }
                }

                if (lastOpen + 1 >= spanText.Length)
                {
                    break;
                }

                lastOpen = spanText.IndexOf($"<{_tagName}", lastOpen + 1, StringComparison.Ordinal);
            }
        }
    }
}
