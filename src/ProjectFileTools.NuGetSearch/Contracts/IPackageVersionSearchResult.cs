using System.Collections.Generic;

namespace PackageFeedManager
{

    public interface IPackageVersionSearchResult
    {
        bool Success { get; }

        IReadOnlyList<string> Versions { get; }

        FeedKind SourceKind { get; }
    }
}
