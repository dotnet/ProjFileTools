namespace ProjectFileTools.MSBuild
{
    /// <summary>
    /// Provides the correct workspace
    /// </summary>
    public class WorkspaceManager
    {
        // TODO: Support multiple workspaces simultaneously
        private Workspace _workspace;

        /// <summary>
        /// Returns a Workspace that contains the filePath, or creates a new one using the filePath
        /// </summary>
        public Workspace GetWorkspace(string filePath)
        {
            if (_workspace != null && _workspace.ContainsProject(filePath))
            {
                return _workspace;
            }

            _workspace = new Workspace(filePath);
            return _workspace;
        }
    }
}
