namespace ProjectFileTools.MSBuild
{
    /// <summary>
    /// Provides the correct workspace
    /// </summary>
    public class WorkspaceManager : IWorkspaceManager
    {
        // TODO: Support multiple workspaces simultaneously
        private Workspace _workspace;

        /// <summary>
        /// Returns a Workspace that contains the filePath, or creates a new one using the filePath
        /// </summary>
        public IWorkspace GetWorkspace(string filePath)
        {
            if (_workspace != null && !_workspace.IsDisposed && _workspace.ContainsProject(filePath))
            {
                return _workspace;
            }

            _workspace?.Dispose();

            _workspace = new Workspace(filePath);
            return _workspace;
        }
    }
}
