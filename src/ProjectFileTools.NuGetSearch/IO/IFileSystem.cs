using System.Collections.Generic;
using System.IO;

namespace ProjectFileTools.NuGetSearch.IO
{
    public interface IFileSystem
    {
        IEnumerable<string> EnumerateFiles(string path, string pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        IEnumerable<string> EnumerateDirectories(string path, string pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        string ReadAllText(string path);

        bool DirectoryExists(string path);

        bool FileExists(string path);

        string GetDirectoryName(string path);

        string GetDirectoryNameOnly(string path);
    }
}
