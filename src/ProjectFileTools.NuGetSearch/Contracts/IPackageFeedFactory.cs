namespace PackageFeedManager
{

    public interface IPackageFeedFactory
    {
        bool TryHandle(string feed, out IPackageFeed instance);
    }
}
