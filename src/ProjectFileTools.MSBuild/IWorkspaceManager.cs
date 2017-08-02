namespace ProjectFileTools.MSBuild
{
    public interface IWorkspaceManager
    {
        IWorkspace GetWorkspace(string filePath);
    }
}
