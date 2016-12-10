using System.Collections.Generic;
using System.Threading.Tasks;

namespace PackageFeedManager
{

    internal class PackageVersionSearchResult : IPackageVersionSearchResult
    {
        public static IPackageVersionSearchResult Cancelled { get; } = new PackageVersionSearchResult();

        public static Task<IPackageVersionSearchResult> CancelledTask { get; } = Task.FromResult(Cancelled);

        public static IPackageVersionSearchResult Failure { get; } = new PackageVersionSearchResult();

        public static Task<IPackageVersionSearchResult> FailureTask { get; } = Task.FromResult(Failure);

        public bool Success { get; }

        public IReadOnlyList<string> Versions { get; }

        public SourceKind SourceKind { get; }

        private PackageVersionSearchResult()
        {
        }

        public PackageVersionSearchResult(IReadOnlyList<string> versions, SourceKind kind)
        {
            Versions = versions;
            Success = true;
            SourceKind = kind;
        }
    }
}
