using System.Collections.Generic;

namespace ProjectFileTools.MSBuild
{
    public interface IWorkspace
    {
        bool EvaluateCondition(string text);

        string GetEvaluatedPropertyValue(string text);

        List<Definition> GetItemProvenance(string fileSpec);

        List<Definition> GetItems(string fileSpec);

        List<Definition> ResolveDefinition(string filePath, string sourceText, int position);
    }
}
