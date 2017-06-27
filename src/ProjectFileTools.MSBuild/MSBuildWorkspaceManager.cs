namespace ProjectFileTools.MSBuild
{
    public class MSBuildWorkspaceManager
    {
        // TODO: Support multiple workspaces simultaneously
        private MSBuildWorkspace _workspace;
        public MSBuildWorkspace GetWorkspace(string filePath)
        {
            if (_workspace != null && _workspace.ContainsProject(filePath))
            {
                return _workspace;
            }

            _workspace = new MSBuildWorkspace(filePath);
            return _workspace;
        }
    }
}
