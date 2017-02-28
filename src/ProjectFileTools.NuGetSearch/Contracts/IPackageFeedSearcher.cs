using System.Threading.Tasks;

namespace ProjectFileTools.NuGetSearch.Contracts
{

    public interface IPackageFeedSearcher
    {
        Task<IPackageNameSearchResult> SearchPackagesAsync(string prefix, params string[] feeds);

        Task<IPackageVersionSearchResult> SearchVersionsAsync(string prefix, params string[] feeds);
    }
}
