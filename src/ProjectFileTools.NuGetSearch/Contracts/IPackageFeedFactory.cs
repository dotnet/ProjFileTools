namespace ProjectFileTools.NuGetSearch.Contracts
{

    public interface IPackageFeedFactory
    {
        bool TryHandle(string feed, out IPackageFeed instance);
    }
}
