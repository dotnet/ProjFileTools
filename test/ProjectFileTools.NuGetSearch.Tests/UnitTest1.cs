using System;
using System.Threading;
// using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Frameworks;
using ProjectFileTools.NuGetSearch.Contracts;
using ProjectFileTools.NuGetSearch.Feeds;
using ProjectFileTools.NuGetSearch.Feeds.Disk;
using ProjectFileTools.NuGetSearch.IO;
using Xunit;

namespace PackageFeedManagerTests
{
    public class UnitTest1
    {
        [Fact(Skip="Just for testing locally")]
        public void TestMethod1()
        {
            //IFileSystem fileSystem = new MockFileSystem();
            //IPackageFeedFactory diskFeed = new NuGetDiskFeedFactory(fileSystem);
            //PackageFeedFactorySelector factory = new PackageFeedFactorySelector(new[] { diskFeed });
            //IPackageFeed feed = factory.GetFeed(@"C:\Users\mlorbe\.nuget");
            //var config = new PackageQueryConfiguration(new NuGetFramework(".NETFramework", new Version(4, 5, 2, 0)).ToString(), includePreRelease: true);
            //var ids = feed.GetPackageNamesAsync("cli.ut", config, CancellationToken.None).Result;
            //var vers = feed.GetPackageVersionsAsync(ids.Names[0], config, CancellationToken.None).Result;
        }
    }
}
