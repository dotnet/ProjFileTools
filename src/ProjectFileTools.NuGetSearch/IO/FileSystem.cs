using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjectFileTools.NuGetSearch.IO
{

    public class FileSystem : IFileSystem
    {
        public bool DirectoryExists(string path)
        {
            return path.IndexOfAny(Path.GetInvalidPathChars()) < 0 && Directory.Exists(path);
        }

        public IEnumerable<string> EnumerateDirectories(string path, string pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!DirectoryExists(path))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateDirectories(path, pattern, searchOption);
        }

        public IEnumerable<string> EnumerateFiles(string path, string pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!DirectoryExists(path))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateFiles(path, pattern, searchOption);
        }

        public bool FileExists(string path)
        {
            return path.IndexOfAny(Path.GetInvalidPathChars()) < 0 && File.Exists(path);
        }

        public string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public string GetDirectoryNameOnly(string path)
        {
            return new DirectoryInfo(path).Name;
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }
    }

}
