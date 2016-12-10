using System.Collections.Generic;
using System.Threading.Tasks;

namespace PackageFeedManager
{

    internal class PackageNameSearchResult : IPackageNameSearchResult
    {
        public static IPackageNameSearchResult Cancelled { get; } = new PackageNameSearchResult();

        public static IPackageNameSearchResult Failure { get; } = new PackageNameSearchResult();

        public static Task<IPackageNameSearchResult> CancelledTask { get; } = Task.FromResult(Cancelled);

        public static Task<IPackageNameSearchResult> FailureTask { get; } = Task.FromResult(Failure);

        public bool Success { get; }

        public IReadOnlyList<string> Names { get; }

        public SourceKind SourceKind { get; }

        private PackageNameSearchResult()
        {
        }

        public PackageNameSearchResult(IReadOnlyList<string> names, SourceKind sourceKind)
        {
            Success = true;
            Names = names;
            SourceKind = sourceKind;
        }
    }
}
