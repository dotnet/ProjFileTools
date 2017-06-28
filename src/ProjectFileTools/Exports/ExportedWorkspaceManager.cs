using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.MSBuild;

namespace ProjectFileTools.Exports
{
    [Export(typeof(WorkspaceManager))]
    [Name("Default WorkspaceManager")]
    public class ExportedWorkspaceManager : WorkspaceManager
    {

    }
}
