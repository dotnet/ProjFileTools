using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Frameworks;
using PackageFeedManager;

namespace PackageFeedManagerTests
{
    public class MockWebRequestFactory : IWebRequestFactory
    {
        public Task<string> GetStringAsync(string endpoint, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class MockFileSystem : IFileSystem
    {
        public bool DirectoryExists(string path)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> EnumerateDirectories(string path, string pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> EnumerateFiles(string path, string pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(string path)
        {
            throw new NotImplementedException();
        }

        public string GetDirectoryName(string path)
        {
            throw new NotImplementedException();
        }

        public string GetDirectoryNameOnly(string path)
        {
            throw new NotImplementedException();
        }

        public string ReadAllText(string path)
        {
            throw new NotImplementedException();
        }
    }

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            IFileSystem fileSystem = new MockFileSystem();
            IPackageFeedFactory diskFeed = new NuGetDiskFeedFactory(fileSystem);
            PackageFeedFactorySelector factory = new PackageFeedFactorySelector(new[] { diskFeed });
            IPackageFeed feed = factory.GetFeed(@"C:\Users\mlorbe\.nuget");
            var config = new PackageQueryConfiguration(new NuGetFramework(".NETFramework", new Version(4, 5, 2, 0)).ToString(), includePreRelease: true);
            var ids = feed.GetPackageNamesAsync("cli.ut", config, CancellationToken.None).Result;
            var vers = feed.GetPackageVersionsAsync(ids.Names[0], config, CancellationToken.None).Result;
        }
    }
}
