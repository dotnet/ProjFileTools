using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Language.Xml;

namespace ProjectFileTools.MSBuild
{
    public class MSBuildWorkspace
    {
        private ProjectCollection _collection;
        private Project _project;
        private HashSet<string> _containedFiles;
        private List<FileSystemWatcher> _watchers;
        private bool _needsReload;

        internal MSBuildWorkspace(string filePath)
        {
            _collection = new ProjectCollection();
            _containedFiles = new HashSet<string>();
            _watchers = new List<FileSystemWatcher>();
            _needsReload = false;

            try
            {
                _project = _collection.LoadProject(filePath);
                UpdateContainedFiles();
            }
            // TODO: Propagate error to the errors list
            catch
            {
                _project = null;
            }

        }

        public string ResolveDefinition(string filePath, string sourceText, int position)
        {
            String file = null;

            if (_project != null)
            {
                XmlDocumentSyntax root = Parser.ParseText(sourceText);
                SyntaxNode syntaxNode = SyntaxLocator.FindNode(root, position);

                if (syntaxNode.ParentElement.Name.Equals("Import"))
                {
                    while (syntaxNode.Parent.ParentElement == syntaxNode.ParentElement)
                    {
                        syntaxNode = syntaxNode.Parent;
                    }

                    int nodeStart = syntaxNode.Parent.Span.Start;
                    int col = nodeStart - Utilities.GetStartOfLine(sourceText, nodeStart) + 1;
                    int line = Utilities.GetLine(sourceText, nodeStart) + 1;

                    foreach (ResolvedImport import in _project.Imports)
                    {
                        ElementLocation location = import.ImportingElement.Location;
                        if (location.File == filePath && col == location.Column && line == location.Line)
                        {
                            file = import.ImportedProject.FullPath;
                            break;
                        }
                    }
                }
            }
                return file;
        }

        public bool ContainsProject(string filePath)
        {
            ReloadIfNescessary();
            return _containedFiles.Contains(filePath);
        }

        private void ReloadIfNescessary()
        {
            if (_needsReload)
            {
                string tempPath = _project.FullPath;
                try
                {
                    ProjectCollection tempCollection = new ProjectCollection();
                    Project tempProject = tempCollection.LoadProject(tempPath);
                    _collection.UnloadAllProjects();
                    _collection = tempCollection;
                    _project = tempProject;
                    UpdateContainedFiles();
                }
                // TODO: Propagate error to the errors list
                catch
                {

                }

                _needsReload = false;
            }
        }

        private void UpdateContainedFiles()
        {
            _containedFiles.Clear();

            foreach(FileSystemWatcher watcher in _watchers)
            {
                watcher.Changed -= MarkReload;
                watcher.Deleted -= MarkReload;
                watcher.Renamed -= MarkReload;
                watcher.Dispose();
            }

            _watchers.Clear();

            foreach (ResolvedImport import in _project.Imports)
            {
                _containedFiles.Add(import.ImportingElement.Location.File);
                _containedFiles.Add(import.ImportedProject.FullPath);
            }

            foreach(string path in _containedFiles)
            {
                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.Path = Path.GetDirectoryName(path);
                watcher.Filter = Path.GetFileName(path);
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.EnableRaisingEvents = true;
                watcher.Changed += MarkReload;
                watcher.Deleted += MarkReload;
                watcher.Renamed += MarkReload;
                _watchers.Add(watcher);
            }
        }

        private void MarkReload(object sender, FileSystemEventArgs e)
        {
            _needsReload = true;
        }
    }
}
