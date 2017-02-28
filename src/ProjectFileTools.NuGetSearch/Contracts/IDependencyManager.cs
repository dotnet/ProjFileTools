using System.Collections.Generic;

namespace ProjectFileTools.NuGetSearch.Contracts
{

    public interface IDependencyManager
    {
        T GetComponent<T>();

        IReadOnlyList<T> GetComponents<T>();
    }
}
