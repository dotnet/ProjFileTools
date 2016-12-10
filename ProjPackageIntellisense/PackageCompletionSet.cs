using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace ProjPackageIntellisense
{

    internal class PackageCompletionSet : CompletionSet2
    {
        public PackageCompletionSet(string moniker, string displayName, ITrackingSpan applicableTo)
            : base(moniker, displayName, applicableTo, new Completion[0], new Completion[0], new IIntellisenseFilter[0])
        {
            AccessibleCompletions = new BulkObservableCollection<Completion>();
        }

        public override IList<Completion> Completions => AccessibleCompletions;

        public BulkObservableCollection<Completion> AccessibleCompletions { get; }
    }
}
