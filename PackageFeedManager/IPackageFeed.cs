using System.Threading;
using System.Threading.Tasks;

namespace PackageFeedManager
{

    public interface IPackageFeed
    {
        string DisplayName { get; }

        Task<IPackageNameSearchResult> GetPackageNamesAsync(string prefix, IPackageQueryConfiguration queryConfiguration, CancellationToken cancellationToken);

        Task<IPackageVersionSearchResult> GetPackageVersionsAsync(string id, IPackageQueryConfiguration queryConfiguration, CancellationToken cancellationToken);

        Task<IPackageInfo> GetPackageInfoAsync(string id, string version, IPackageQueryConfiguration queryConfiguration, CancellationToken cancellationToken);
    }
}
