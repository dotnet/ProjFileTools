namespace ProjectFileTools.MSBuild
{
    public interface IWorkspace
    {
        string ResolveDefinition(string filePath, string sourceText, int position);
    }
}
