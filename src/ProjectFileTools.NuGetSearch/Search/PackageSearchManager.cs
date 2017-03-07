using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectFileTools.NuGetSearch.Contracts;
using ProjectFileTools.NuGetSearch.Feeds;

namespace ProjectFileTools.NuGetSearch.Search
{
    public class PackageSearchManager : IPackageSearchManager
    {
        private readonly IPackageFeedFactorySelector _factorySelector;
        private readonly IPackageFeedRegistryProvider _feedRegistry;
        private readonly ConcurrentDictionary<PackageQueryConfiguration, ConcurrentDictionary<PackageNameQuery, IPackageFeedSearchJob<Tuple<string, FeedKind>>>> _cachedNameSearches;
        private readonly ConcurrentDictionary<PackageQueryConfiguration, ConcurrentDictionary<PackageVersionQuery, IPackageFeedSearchJob<Tuple<string, FeedKind>>>> _cachedVersionSearches;

        public PackageSearchManager(IPackageFeedRegistryProvider feedRegistry, IPackageFeedFactorySelector factorySelector)
        {
            _feedRegistry = feedRegistry;
            _factorySelector = factorySelector;
            _cachedNameSearches = new ConcurrentDictionary<PackageQueryConfiguration, ConcurrentDictionary<PackageNameQuery, IPackageFeedSearchJob<Tuple<string, FeedKind>>>>();
            _cachedVersionSearches = new ConcurrentDictionary<PackageQueryConfiguration, ConcurrentDictionary<PackageVersionQuery, IPackageFeedSearchJob<Tuple<string, FeedKind>>>>();
        }

        private class PackageNameQuery
        {
            private readonly int _hashCode;
            private readonly string _prefix;

            public PackageNameQuery(string prefix, string tfm)
            {
                _hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(prefix ?? "") ^ (tfm?.GetHashCode() ?? 0);
                _prefix = prefix;
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            public override bool Equals(object obj)
            {
                PackageNameQuery q = obj as PackageNameQuery;

                return q != null && q._hashCode == _hashCode && string.Equals(_prefix, q._prefix, StringComparison.Ordinal);
            }
        }

        private class PackageVersionQuery
        {
            private readonly int _hashCode;
            private readonly string _packageName;

            public PackageVersionQuery(string packageName, string tfm)
            {
                _hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(packageName ?? "") ^ (tfm?.GetHashCode() ?? 0);
                _packageName = packageName;
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            public override bool Equals(object obj)
            {
                PackageVersionQuery q = obj as PackageVersionQuery;

                return q != null && q._hashCode == _hashCode && string.Equals(_packageName, q._packageName, StringComparison.Ordinal);
            }
        }

        public IPackageFeedSearchJob<Tuple<string, FeedKind>> SearchPackageNames(string prefix, string tfm)
        {
            PackageQueryConfiguration config = new PackageQueryConfiguration(tfm);

            var bag = _cachedNameSearches.GetOrAdd(config, x => new ConcurrentDictionary<PackageNameQuery, IPackageFeedSearchJob<Tuple<string, FeedKind>>>());
            return bag.AddOrUpdate(new PackageNameQuery(prefix, tfm), x => SearchPackageNamesInternal(prefix, tfm, config), (x, e) =>
            {
                if (e.IsCancelled)
                {
                    return SearchPackageNamesInternal(prefix, tfm, config);
                }

                return e;
            });
        }

        private IPackageFeedSearchJob<Tuple<string, FeedKind>> SearchPackageNamesInternal(string prefix, string tfm, IPackageQueryConfiguration config)
        {
            List<Tuple<string, Task<IReadOnlyList<Tuple<string, FeedKind>>>>> searchTasks = new List<Tuple<string, Task<IReadOnlyList<Tuple<string, FeedKind>>>>>();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            foreach (string feedSource in _feedRegistry.ConfiguredFeeds)
            {
                IPackageFeed feed = _factorySelector.GetFeed(feedSource);

                if (feed != null)
                {
                    searchTasks.Add(new Tuple<string, Task<IReadOnlyList<Tuple<string, FeedKind>>>>(feed.DisplayName, feed.GetPackageNamesAsync(prefix, config, cancellationToken).ContinueWith(TransformToPackageInfo)));
                }
            }

            return new PackageFeedSearchJob<Tuple<string, FeedKind>>(searchTasks, cancellationTokenSource);
        }

        private IReadOnlyList<Tuple<string, FeedKind>> TransformToPackageInfo(Task<IPackageNameSearchResult> arg)
        {
            if (arg.IsFaulted)
            {
                throw arg.Exception;
            }

            if (arg.IsCanceled)
            {
                throw new TaskCanceledException();
            }

            List<Tuple<string, FeedKind>> packages = new List<Tuple<string, FeedKind>>();

            if (arg.Result?.Success ?? false)
            {
                foreach (string name in arg.Result.Names)
                {
                    Tuple<string, FeedKind> result = Tuple.Create(name, arg.Result.SourceKind);
                    packages.Add(result);
                }
            }

            return packages;
        }

        public IPackageFeedSearchJob<Tuple<string, FeedKind>> SearchPackageVersions(string packageName, string tfm)
        {
            PackageQueryConfiguration config = new PackageQueryConfiguration(tfm);

            var bag = _cachedVersionSearches.GetOrAdd(config, x => new ConcurrentDictionary<PackageVersionQuery, IPackageFeedSearchJob<Tuple<string, FeedKind>>>());
            return bag.AddOrUpdate(new PackageVersionQuery(packageName, tfm), x => SearchPackageVersionsInternal(packageName, tfm, config), (x, e) =>
            {
                if (e.IsCancelled)
                {
                    return SearchPackageVersionsInternal(packageName, tfm, config);
                }

                return e;
            });
        }

        public IPackageFeedSearchJob<Tuple<string, FeedKind>> SearchPackageVersionsInternal(string packageName, string tfm, IPackageQueryConfiguration config)
        {
            List<Tuple<string, Task<IReadOnlyList<Tuple<string, FeedKind>>>>> searchTasks = new List<Tuple<string, Task<IReadOnlyList<Tuple<string, FeedKind>>>>>();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            foreach (string feedSource in _feedRegistry.ConfiguredFeeds)
            {
                IPackageFeed feed = _factorySelector.GetFeed(feedSource);

                if (feed != null)
                {
                    searchTasks.Add(new Tuple<string, Task<IReadOnlyList<Tuple<string, FeedKind>>>>(feed.DisplayName, feed.GetPackageVersionsAsync(packageName, config, cancellationToken).ContinueWith(TransformToPackageVersion)));
                }
            }

            return new PackageFeedSearchJob<Tuple<string, FeedKind>>(searchTasks, cancellationTokenSource);
        }

        private IReadOnlyList<Tuple<string, FeedKind>> TransformToPackageVersion(Task<IPackageVersionSearchResult> arg)
        {
            if (arg.IsFaulted)
            {
                throw arg.Exception;
            }

            if (arg.IsCanceled)
            {
                throw new TaskCanceledException();
            }

            if (arg.Result?.Success ?? false)
            {
                List<Tuple<string, FeedKind>> results = new List<Tuple<string, FeedKind>>();

                foreach(string ver in arg.Result.Versions)
                {
                    results.Add(Tuple.Create(ver, arg.Result.SourceKind));
                }

                return results;
            }

            return new List<Tuple<string, FeedKind>>();
        }

        public IPackageFeedSearchJob<IPackageInfo> SearchPackageInfo(string packageId, string version, string tfm)
        {
            ConcurrentDictionary<string, IPackageFeedSearchJob<IPackageInfo>> lookup = PackageInfoLookup.GetOrAdd(packageId, id => new ConcurrentDictionary<string, IPackageFeedSearchJob<IPackageInfo>>(StringComparer.OrdinalIgnoreCase));
            return lookup.AddOrUpdate(version ?? string.Empty, ver => Compute(packageId, version, tfm), (ver, e) =>
            {
                if (e.IsCancelled)
                {
                    return Compute(packageId, version, tfm);
                }

                return e;
            });
        }

        private IPackageFeedSearchJob<IPackageInfo> Compute(string packageId, string version, string tfm)
        {
            IPackageQueryConfiguration config = new PackageQueryConfiguration(tfm);
            List<Tuple<string, Task<IReadOnlyList<IPackageInfo>>>> searchTasks = new List<Tuple<string, Task<IReadOnlyList<IPackageInfo>>>>();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            foreach (string feedSource in _feedRegistry.ConfiguredFeeds)
            {
                IPackageFeed feed = _factorySelector.GetFeed(feedSource);

                if (feed != null)
                {
                    searchTasks.Add(Tuple.Create(feedSource, feed.GetPackageInfoAsync(packageId, version, config, cancellationToken).ContinueWith(x =>
                    {
                        if (x == null || x.IsFaulted || x.IsCanceled)
                        {
                            return new IPackageInfo[0];
                        }

                        return (IReadOnlyList<IPackageInfo>)new[] { x.Result };
                    })));
                }
            }

            return new PackageFeedSearchJob<IPackageInfo>(searchTasks, cancellationTokenSource);
        }

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IPackageFeedSearchJob<IPackageInfo>>> PackageInfoLookup = new ConcurrentDictionary<string, ConcurrentDictionary<string, IPackageFeedSearchJob<IPackageInfo>>>(StringComparer.OrdinalIgnoreCase);
    }
}
