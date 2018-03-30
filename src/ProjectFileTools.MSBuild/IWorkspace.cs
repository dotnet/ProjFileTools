using System.Collections.Generic;

namespace ProjectFileTools.MSBuild
{
    public interface IWorkspace
    {
        List<Definition> GetItems(string fileSpec);

        List<Definition> GetItemProvenance(string fileSpec);

        List<Definition> ResolveDefinition(string filePath, string sourceText, int position);

        string GetEvaluatedPropertyValue(string text);

        bool EvaluateCondition(string text);
    }
}
