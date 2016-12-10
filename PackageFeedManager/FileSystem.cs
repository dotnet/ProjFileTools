using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace PackageFeedManager
{

    [Export(typeof(IFileSystem))]
    [Name("Default File System Implementation")]
    internal class FileSystem : IFileSystem
    {
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public IEnumerable<string> EnumerateDirectories(string path, string pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!Directory.Exists(path))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateDirectories(path, pattern, searchOption);
        }

        public IEnumerable<string> EnumerateFiles(string path, string pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!Directory.Exists(path))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateFiles(path, pattern, searchOption);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
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
