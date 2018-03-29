using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using ProjectFileTools.NuGetSearch;
using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools
{
    /// <summary>
    /// Interaction logic for PackageInfoControl.xaml
    /// </summary>
    public partial class PackageInfoControl : UserControl
    {
        private readonly IPackageFeedSearchJob<IPackageInfo> _job;
        private static readonly Dictionary<string, ImageSource> SourceLookup = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        private bool _firstRun;

        public PackageInfoControl(string packageId, string version, string tfm, IPackageSearchManager searcher)
        {
            InitializeComponent();
            this.ShouldBeThemed();
            PackageId.Content = packageId;
            Glyph.Source = WpfUtil.MonikerToBitmap(KnownMonikers.NuGet, 32);
            Glyph.ImageFailed += OnImageFailed;
            _job = searcher.SearchPackageInfo(packageId, version, tfm);
            _job.Updated += JobUpdated;
            _firstRun = true;
            JobUpdated(null, EventArgs.Empty);
        }

        private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Glyph.ImageFailed -= OnImageFailed;
            Glyph.Source = WpfUtil.MonikerToBitmap(KnownMonikers.NuGet, 32);
        }

        private void JobUpdated(object sender, EventArgs e)
        {
            IPackageInfo package = _job.Results.OrderByDescending(x => SemanticVersion.Parse(x.Version)).FirstOrDefault();

            if (package != null)
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    if (!_firstRun && !IsVisible)
                    {
                        return;
                    }

                    _firstRun = false;
                    if (!string.IsNullOrEmpty(package.IconUrl) && Uri.TryCreate(package.IconUrl, UriKind.Absolute, out Uri iconUri))
                    {
                        try
                        {
                            if (!SourceLookup.TryGetValue(package.IconUrl, out ImageSource source))
                            {
                                source = SourceLookup[package.IconUrl] = new BitmapImage(iconUri);
                            }

                            Glyph.Source = source;
                        }
                        catch
                        {
                        }
                    }

                    Description.Text = package.Description;
                });
            }
        }
    }
}
