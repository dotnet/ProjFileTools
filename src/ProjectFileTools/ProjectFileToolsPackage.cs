using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace ProjectFileTools
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [Guid(PackageGuidString)]
    public sealed class ProjectFileToolsPackage : AsyncPackage
    {
        public const string PackageGuidString = "60347b36-f766-4480-8038-ff1b70212235";
    }
}
