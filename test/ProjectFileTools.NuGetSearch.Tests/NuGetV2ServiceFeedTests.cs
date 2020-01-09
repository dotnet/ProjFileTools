using FluentAssertions;
using Moq;
using ProjectFileTools.NuGetSearch.Feeds;
using ProjectFileTools.NuGetSearch.Feeds.Web;
using ProjectFileTools.NuGetSearch.IO;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProjectFileTools.NuGetSearch.Tests
{
    /// <summary>
    /// The tests below only work when signing is disabled.
    /// When signing enabled, no test will be found as a result of `ProjectFileTools.NuGetSearch` failing to load with signing key validation error
    /// </summary>
    public class NuGetV2ServiceFeedTests
    {
       
        public class TheDisplayNameProperty
        {

            [Theory]
            [InlineData("http://localhost/nuget")]
            public void GivenFeed_ReturnDisplayName(string feed)
            {
                var webRequestFactory = Mock.Of<IWebRequestFactory>();
                var sut = new NuGetV2ServiceFeed(feed, webRequestFactory);
                sut.DisplayName.Should().Be($"{feed} (NuGet v2)");
            }
        }

        public class TheGetPackageNamesAsyncMethod
        {
            [Theory]
            [InlineData("http://localhost/nuget", "GetPackageNames.CommonLogging.xml")]
            public async Task GivenPackagesFound_ReturnListOfIds(string feed, string testFile)
            {
                var webRequestFactory = Mock.Of<IWebRequestFactory>();

                Mock.Get(webRequestFactory)
                    .Setup(f => f.GetStringAsync("http://localhost/nuget/Search()?searchTerm='Common.Logging'&targetFramework=netcoreapp2.0&includePrerelease=False&semVerLevel=2.0.0", It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(GetXmlFromTestFile(testFile)));

                var sut = new NuGetV2ServiceFeed(feed, webRequestFactory);

                var packageNameResults = await sut.GetPackageNamesAsync("Common.Logging", new PackageQueryConfiguration("netcoreapp2.0", false), new CancellationToken());
                packageNameResults.Names.Count.Should().Be(5);
            }
        }

        public class TheGetPackageVersionsAsyncMethod
        {
            [Theory]
            [InlineData("http://localhost/nuget", "GetPackageVersions.CommonLogging.xml")]
            public async Task GivenPackagesFound_ReturnListOfVersions(string feed, string testFile)
            {
                var webRequestFactory = Mock.Of<IWebRequestFactory>();

                Mock.Get(webRequestFactory)
                    .Setup(f => f.GetStringAsync("http://localhost/nuget/FindPackagesById()?id='Acme.Common.Logging.AspNetCore'", It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(GetXmlFromTestFile(testFile)));

                var sut = new NuGetV2ServiceFeed(feed, webRequestFactory);

                var packageNameResults = await sut.GetPackageVersionsAsync("Acme.Common.Logging.AspNetCore", new PackageQueryConfiguration("netcoreapp2.0", false), new CancellationToken());
                packageNameResults.Versions.Count.Should().Be(8);
                Assert.Collection(packageNameResults.Versions,
                    v => v.Should().Be("1.6.0.5"),
                    v => v.Should().Be("1.6.1"),
                    v => v.Should().Be("1.6.2"),
                    v => v.Should().Be("1.7.0"),
                    v => v.Should().Be("1.7.1"),
                    v => v.Should().Be("1.8.0"),
                    v => v.Should().Be("1.9.0"),
                    v => v.Should().Be("1.9.1"));
            }
        }

        public class TheGetPackageInfoAsyncMethod
        {
            [Theory]
            [InlineData("http://localhost/nuget", "GetPackageInfo.CommonLogging.xml")]
            public async Task GivenPackageFound_ReturnPackageInfo(string feed, string testFile)
            {
                var webRequestFactory = Mock.Of<IWebRequestFactory>();

                Mock.Get(webRequestFactory)
                    .Setup(f => f.GetStringAsync("http://localhost/nuget/Packages(Id='Acme.Common.Logging.AspNetCore',Version='1.8.0')", It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(GetXmlFromTestFile(testFile)));

                var sut = new NuGetV2ServiceFeed(feed, webRequestFactory);

                var pkgInfo = await sut.GetPackageInfoAsync("Acme.Common.Logging.AspNetCore", "1.8.0", new PackageQueryConfiguration("netcoreapp2.0", false), new CancellationToken());

                pkgInfo.Id.Should().Be("Acme.Common.Logging.AspNetCore"); 
                pkgInfo.Title.Should().Be("Common Logging AspNetCore");
                pkgInfo.Summary.Should().BeNullOrEmpty();
                pkgInfo.Description.Should().Be("Common Logging integration within Aspnet core services");
                pkgInfo.Authors.Should().Be("Patrick Assuied");
                pkgInfo.Version.Should().Be("1.8.0");
                pkgInfo.ProjectUrl.Should().Be("https://bitbucket.acme.com/projects/Acme/repos/Acme-common-logging");
                pkgInfo.LicenseUrl.Should().BeNullOrEmpty();
                pkgInfo.Tags.Should().Be(" common logging aspnetcore ");


            }
        }

        public static string GetXmlFromTestFile(string filename)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), $"../../test/ProjectFileTools.NuGetSearch.Tests/TestFiles/{filename}");
            return File.ReadAllText(path);
        }
    }
}
