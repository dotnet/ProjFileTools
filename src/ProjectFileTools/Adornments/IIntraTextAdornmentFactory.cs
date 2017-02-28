using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace ProjectFileTools.Adornments
{

    public interface IIntraTextAdornmentFactory<T>
        where T : IntraTextAdornmentTag
    {
        /// <summary>
        /// Returns true if the tag should exist, false if any existing tag should be destroyed
        /// </summary>
        /// <param name="span">The span the tag would be applicable to</param>
        /// <param name="existingTag">The existing tag intersecting the span</param>
        /// <param name="tag">The tag that should apply to the span</param>
        /// <returns>true if the tag should exist, false otherwise</returns>
        bool TryCreateOrUpdate(ITextView textView, SnapshotSpan span, T existingTag, out TagSpan<T> tag, out Span valueSpan);
    }
}
