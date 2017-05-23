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
        private readonly ITextView _view;

        private readonly ITextBuffer _sourceBuffer;

        private readonly ITextSearchService _textSearchService;

        /// <summary>
        /// Contains Snapshots for each string that matches the highlighted text
        /// </summary>
        private NormalizedSnapshotSpanCollection _wordSpans;

        /// <summary>
        /// Last highlighted text
        /// </summary>
        private string _currentWord;

        private readonly HighlightWordTag _highlightWordTag;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public HighlightWordTagger(ITextView view, ITextBuffer sourceBuffer, ITextSearchService textSearchService)
        {
            _view = view;
            _sourceBuffer = sourceBuffer;
            _textSearchService = textSearchService;
            _wordSpans = new NormalizedSnapshotSpanCollection();
            _currentWord = "";
            _highlightWordTag = new HighlightWordTag();
            _view.Selection.SelectionChanged += ViewSelectionChanged;
        }

        private void ViewSelectionChanged(object sender, EventArgs e)
        {
            UpdateWordAdornnents(_view.Selection.StreamSelectionSpan.GetText());
        }

        private void UpdateWordAdornnents(string newSelection)
        {
            // If the new string is equal to the old one, we do not need to update the tags.
            if (_currentWord.Equals(newSelection))
            {
                return;
            }

            List<SnapshotSpan> wordSpans = new List<SnapshotSpan>();

            // If the user only selected whitespace, do not create any snapshots.
            if (!newSelection.All(char.IsWhiteSpace))
            {
                // Finds exact matches (does not match with substrings of words).
                FindData findData = new FindData(newSelection, _view.TextSnapshot)
                {
                    FindOptions = FindOptions.WholeWord | FindOptions.MatchCase
                };
                wordSpans.AddRange(_textSearchService.FindAll(findData));
            }

            _wordSpans = new NormalizedSnapshotSpanCollection(wordSpans);
            Update(newSelection);
        }

        private void Update(string currentWord)
        {
            _currentWord = currentWord;
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(_sourceBuffer.CurrentSnapshot, 0, _sourceBuffer.CurrentSnapshot.Length)));
        }

        public IEnumerable<ITagSpan<HighlightWordTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (SnapshotSpan span in _wordSpans)
            {
                yield return new TagSpan<HighlightWordTag>(span, _highlightWordTag);
            }
        }
    }
}
