using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace ProjectFileTools.Completion
{

    internal class PackageCompletionSet : CompletionSet2
    {
        public PackageCompletionSet(string moniker, string displayName, ITrackingSpan applicableTo)
            : base(moniker, displayName, applicableTo, new Microsoft.VisualStudio.Language.Intellisense.Completion[0], new Microsoft.VisualStudio.Language.Intellisense.Completion[0], new IIntellisenseFilter[0])
        {
            AccessibleCompletions = new BulkObservableCollection<Microsoft.VisualStudio.Language.Intellisense.Completion>();
        }

        public override IList<Microsoft.VisualStudio.Language.Intellisense.Completion> Completions => AccessibleCompletions;

        public BulkObservableCollection<Microsoft.VisualStudio.Language.Intellisense.Completion> AccessibleCompletions { get; }
    }
}
