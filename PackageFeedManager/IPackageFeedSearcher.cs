using System.Threading.Tasks;

namespace PackageFeedManager
{

    public interface IPackageFeedSearcher
    {
        Task<IPackageNameSearchResult> SearchPackagesAsync(string prefix, params string[] feeds);

        Task<IPackageVersionSearchResult> SearchVersionsAsync(string prefix, params string[] feeds);
    }
}
