using System.Collections.Generic;

namespace ProjectFileTools.MSBuild
{
    public interface IWorkspace
    {
        List<Definition> ResolveDefinition(string filePath, string sourceText, int position);
    }
}
