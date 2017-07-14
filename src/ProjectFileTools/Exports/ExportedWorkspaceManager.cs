using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.MSBuild;

namespace ProjectFileTools.Exports
{
    [Export(typeof(IWorkspaceManager))]
    [Name("Default WorkspaceManager")]
    public class ExportedWorkspaceManager : WorkspaceManager
    {
    }
}
