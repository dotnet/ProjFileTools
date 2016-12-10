using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace PackageFeedManager
{
    [Export(typeof(IPackageSearchManager))]
    [Name("Default Package Search Manager")]
    internal class PackageSearchManager : IPackageSearchManager
    {
        private readonly IPackageFeedFactorySelector _factorySelector;
        private readonly IPackageFeedRegistryProvider _feedRegistry;

        [ImportingConstructor]
        public PackageSearchManager(IPackageFeedRegistryProvider feedRegistry, IPackageFeedFactorySelector factorySelector)
        {
            _feedRegistry = feedRegistry;
            _factorySelector = factorySelector;
        }

        public IPackageFeedSearchJob<Tuple<string, SourceKind>> SearchPackageNames(string prefix, string tfm)
        {
            IPackageQueryConfiguration config = new PackageQueryConfiguration(tfm);
            List<Tuple<string, Task<IReadOnlyList<Tuple<string, SourceKind>>>>> searchTasks = new List<Tuple<string, Task<IReadOnlyList<Tuple<string, SourceKind>>>>>();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            foreach(string feedSource in _feedRegistry.ConfiguredFeeds)
            {
                IPackageFeed feed = _factorySelector.GetFeed(feedSource);

                if (feed != null)
                {
                    searchTasks.Add(new Tuple<string, Task<IReadOnlyList<Tuple<string, SourceKind>>>>(feed.DisplayName, feed.GetPackageNamesAsync(prefix, config, cancellationToken).ContinueWith(TransformToPackageInfo)));
                }
            }

            return new PackageFeedSearchJob<Tuple<string, SourceKind>>(searchTasks, cancellationTokenSource);
        }

        private IReadOnlyList<Tuple<string, SourceKind>> TransformToPackageInfo(Task<IPackageNameSearchResult> arg)
        {
            if (arg.IsFaulted)
            {
                throw arg.Exception;
            }

            if (arg.IsCanceled)
            {
                throw new TaskCanceledException();
            }

            List<Tuple<string, SourceKind>> packages = new List<Tuple<string, SourceKind>>();

            if (arg.Result?.Success ?? false)
            {
                foreach (string name in arg.Result.Names)
                {
                    Tuple<string, SourceKind> result = Tuple.Create(name, arg.Result.SourceKind);
                    packages.Add(result);
                }
            }

            return packages;
        }

        public IPackageFeedSearchJob<Tuple<string, SourceKind>> SearchPackageVersions(string packageName, string tfm)
        {
            IPackageQueryConfiguration config = new PackageQueryConfiguration(tfm);
            List<Tuple<string, Task<IReadOnlyList<Tuple<string, SourceKind>>>>> searchTasks = new List<Tuple<string, Task<IReadOnlyList<Tuple<string, SourceKind>>>>>();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            foreach (string feedSource in _feedRegistry.ConfiguredFeeds)
            {
                IPackageFeed feed = _factorySelector.GetFeed(feedSource);

                if (feed != null)
                {
                    searchTasks.Add(new Tuple<string, Task<IReadOnlyList<Tuple<string, SourceKind>>>>(feed.DisplayName, feed.GetPackageVersionsAsync(packageName, config, cancellationToken).ContinueWith(TransformToPackageVersion)));
                }
            }

            return new PackageFeedSearchJob<Tuple<string, SourceKind>>(searchTasks, cancellationTokenSource);
        }

        private IReadOnlyList<Tuple<string, SourceKind>> TransformToPackageVersion(Task<IPackageVersionSearchResult> arg)
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
                List<Tuple<string, SourceKind>> results = new List<Tuple<string, SourceKind>>();

                foreach(string ver in arg.Result.Versions)
                {
                    results.Add(Tuple.Create(ver, arg.Result.SourceKind));
                }

                return results;
            }

            return new List<Tuple<string, SourceKind>>();
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
                    searchTasks.Add(Tuple.Create(feedSource, feed.GetPackageInfoAsync(packageId, version, config, cancellationToken).ContinueWith(x => (IReadOnlyList<IPackageInfo>)new[] { x.Result })));
                }
            }

            return new PackageFeedSearchJob<IPackageInfo>(searchTasks, cancellationTokenSource);
        }

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IPackageFeedSearchJob<IPackageInfo>>> PackageInfoLookup = new ConcurrentDictionary<string, ConcurrentDictionary<string, IPackageFeedSearchJob<IPackageInfo>>>(StringComparer.OrdinalIgnoreCase);
    }
}
