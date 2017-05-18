using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;

namespace ProjectFileTools.Adornments
{
    /// <summary>
    /// Finds and updates the correct tags for the highlighted text.
    /// </summary>
    internal class HighlightWordTagger : ITagger<HighlightWordTag>
    {
        ITextView View { get; set; }

        ITextBuffer SourceBuffer { get; set; }

        ITextSearchService TextSearchService { get; set; }

        /// <summary>
        /// Contains Snapshots for each string that matches the highlighted text
        /// </summary>
        NormalizedSnapshotSpanCollection WordSpans { get; set; }

        /// <summary>
        /// Last highlighted text
        /// </summary>
        string CurrentWord { get; set; }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public HighlightWordTagger(ITextView view, ITextBuffer sourceBuffer, ITextSearchService textSearchService)
        {
            this.View = view;
            this.SourceBuffer = sourceBuffer;
            this.TextSearchService = textSearchService;
            this.WordSpans = new NormalizedSnapshotSpanCollection();
            this.CurrentWord = "";
            this.View.Selection.SelectionChanged += ViewSelectionChanged;
        }

        private void ViewSelectionChanged(object sender, EventArgs e)
        {
            UpdateWordAdornnents(this.View.Selection.StreamSelectionSpan.GetText());
        }

        private void UpdateWordAdornnents(string newSelection)
        {
            // If the new string is equal to the old one, we do not need to update the tags.
            if (this.CurrentWord.Equals(newSelection))
            {
                return;
            }

            List<SnapshotSpan> wordSpans = new List<SnapshotSpan>();

            // If the user only selected whitespace, do not create any snapshots.
            if (newSelection.Any((c => !char.IsWhiteSpace(c))))
            {
                // Finds exact matches (does not match with substrings of words).
                FindData findData = new FindData(newSelection, this.View.TextSnapshot);
                findData.FindOptions = FindOptions.WholeWord | FindOptions.MatchCase;
                wordSpans.AddRange(TextSearchService.FindAll(findData));
            } 
            this.Update(new NormalizedSnapshotSpanCollection(wordSpans), newSelection);
        }

        private void Update(NormalizedSnapshotSpanCollection normalizedSnapshotSpanCollection, string currentWord)
        {
            this.WordSpans = normalizedSnapshotSpanCollection;
            this.CurrentWord = currentWord;
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length)));
        }

        public IEnumerable<ITagSpan<HighlightWordTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (SnapshotSpan span in WordSpans)
            {
                yield return new TagSpan<HighlightWordTag>(span, new HighlightWordTag());
            }
        }
    }
}
