using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.MSBuild;

namespace ProjectFileTools.Exports
{
    [Export(typeof(MSBuildWorkspaceManager))]
    [Name("Default MSBuildWorkspaceManager")]
    public class ExportedMSBuildWorkspaceManager : MSBuildWorkspaceManager
    {

    }
}
