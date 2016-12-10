using System.Collections.Generic;

namespace PackageFeedManager
{

    public interface IPackageNameSearchResult
    {
        bool Success { get; }

        SourceKind SourceKind { get; }

        IReadOnlyList<string> Names { get; }
    }
}
