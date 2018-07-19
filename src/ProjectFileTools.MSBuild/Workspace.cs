using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using Microsoft;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Language.Xml;

namespace ProjectFileTools.MSBuild
{
    /// <summary>
    /// Contains an MSBuild project and logic to extract information from it
    /// </summary>
    public class Workspace : IWorkspace, IDisposableObservable
    {
        private ProjectCollection _collection;
        private readonly HashSet<string> _containedFiles;
        private bool _needsReload;
        private Project _project;
        private List<FileSystemWatcher> _watchers;
        private readonly PropertyInfo _projectItemElementXmlElementProperty = typeof(ProjectElement).GetProperty("XmlElement", BindingFlags.NonPublic | BindingFlags.Instance);

        internal Workspace(string filePath)
        {
            _collection = new ProjectCollection();
            _containedFiles = new HashSet<string>(StringComparer.Ordinal);
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

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
            List<FileSystemWatcher> watchers = Interlocked.Exchange(ref _watchers, null);
            if (watchers != null)
            {
                foreach (FileSystemWatcher watcher in watchers)
                {
                    watcher.Changed -= MarkReload;
                    watcher.Deleted -= MarkReload;
                    watcher.Renamed -= MarkReload;
                    watcher.Dispose();
                }
            }
        }

        public bool EvaluateCondition(string text)
        {
            return _project?.CreateProjectInstance().EvaluateCondition(text) ?? false;
        }

        public string GetEvaluatedPropertyValue(string text)
        {
            return _project?.ExpandString(text) ?? string.Empty;
        }

        public List<Definition> GetItemProvenance(string fileSpec)
        {
            List<Definition> results = new List<Definition>();

            if (_project == null)
            {
                return results;
            }

            List<string> items = GetPathsFromFileSpec(fileSpec);
            XmlDocument doc = new XmlDocument();

            foreach (string item in items)
            {
                foreach (ProjectItem include in _project.GetItemsByEvaluatedInclude(item))
                {
                    foreach (ProvenanceResult record in _project.GetItemProvenance(include))
                    {
                        XmlElement element = (XmlElement)_projectItemElementXmlElementProperty.GetValue(record.ItemElement);
                        XmlElement clone = doc.CreateElement(element.LocalName);

                        foreach (XmlAttribute node in element.Attributes.OfType<XmlAttribute>().ToList())
                        {
                            clone.SetAttribute(node.LocalName, node.Value);
                        }

                        string lineText = clone.OuterXml;
                        results.Add(new Definition(record.ItemElement.ContainingProject.FullPath, include.EvaluatedInclude, record.Operation.ToString(), lineText, record.ItemElement.Location.Line, record.ItemElement.Location.Column));
                    }
                }
            }

            return results;
        }

        public List<Definition> GetItems(string fileSpec)
        {
            if (_project == null)
            {
                return new List<Definition>();
            }

            List<string> items = GetPathsFromFileSpec(fileSpec);
            List<Definition> results = new List<Definition>();
            string projectName = Path.GetFileNameWithoutExtension(_project.FullPath);

            foreach (string item in items)
            {
                bool isIncluded = _project.GetItemsByEvaluatedInclude(item).Any();
                string message = isIncluded ? "Included" : "Not Included";
                results.Add(new Definition(item, projectName, message, "(Glob Match)"));
            }

            return results;
        }

        /// <summary>
        /// Returns the URL of the file that contains the definition of the item at the current position 
        /// </summary>
        /// <param name="filePath">Current file</param>
        /// <param name="sourceText">Text in the current file</param>
        /// <param name="position">Position of item that is to be resolved</param>
        /// <returns></returns>
        public List<Definition> ResolveDefinition(string filePath, string sourceText, int position)
        {
            Verify.NotDisposed(this);
            List<Definition> definitions = new List<Definition>();

            if (_project != null)
            {
                XmlDocumentSyntax root = Parser.ParseText(sourceText);
                SyntaxNode syntaxNode = root.FindNode(position);

                // Resolves Definition for properties e.g. $(foo)
                if (syntaxNode.Kind == SyntaxKind.XmlTextLiteralToken && Utilities.IsProperty(sourceText.Substring(syntaxNode.Span.Start, syntaxNode.FullWidth), position - syntaxNode.Span.Start, out string propertyName))
                {
                    foreach (ProjectProperty property in _project.Properties)
                    {
                        if (property.Name == propertyName)
                        {
                            ProjectProperty currentProperty = property;

                            while (currentProperty.Predecessor != null)
                            {
                                if (currentProperty.Xml?.Location != null)
                                {
                                    ElementLocation location = currentProperty.Xml.Location;
                                    definitions.Add(new Definition(location.File, Path.GetFileNameWithoutExtension(_project.Xml.Location.File), currentProperty.Name + " Definitions", currentProperty.EvaluatedValue, location.Line, location.Column));
                                }

                                currentProperty = currentProperty.Predecessor;
                            }

                            if (currentProperty.Xml?.Location != null)
                            {
                                ElementLocation lastLocation = currentProperty.Xml.Location;
                                definitions.Add(new Definition(lastLocation.File, Path.GetFileNameWithoutExtension(_project.Xml.Location.File), currentProperty.Name + " Definitions", currentProperty.EvaluatedValue, lastLocation.Line, lastLocation.Column));
                            }

                            break;
                        }
                    }
                }

                // Resolves Definition for regular imports
                else if (syntaxNode.ParentElement != null && syntaxNode.ParentElement.Name.Equals(SyntaxNames.Import))
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
                            definitions.Add(new Definition(import.ImportedProject.FullPath, Path.GetFileNameWithoutExtension(_project.Xml.Location.File), "Imported Files", Path.GetFileName(import.ImportedProject.FullPath)));
                        }
                    }
                }

                // Resolves Definition for the project's sdk
                else if (syntaxNode.ParentElement != null && syntaxNode.ParentElement.Name.Equals(SyntaxNames.Project))
                {
                    bool foundSdk = false;

                    for (int i = 0; i < 3; i++)
                    {
                        if (sourceText.Substring(syntaxNode.Start, 3).Equals(SyntaxNames.Sdk))
                        {
                            foundSdk = true;
                            break;
                        }

                        syntaxNode = syntaxNode.Parent;
                    }

                    if (foundSdk)
                    {
                        foreach (ResolvedImport import in _project.Imports)
                        {
                            ElementLocation location = import.ImportingElement.Location;

                            if (location.File == filePath && 0 == location.Column && 0 == location.Line)
                            {
                                definitions.Add(new Definition(import.ImportedProject.FullPath, Path.GetFileNameWithoutExtension(_project.Xml.Location.File), "Sdk Imports", Path.GetFileName(import.ImportedProject.FullPath)));
                            }
                        }
                    }
                }
            }

            return definitions;
        }

        internal bool ContainsProject(string filePath)
        {
            Verify.NotDisposed(this);
            ReloadIfNecessary();
            return _containedFiles.Contains(filePath);
        }

        private List<string> GetPathsFromFileSpec(string fileSpec)
        {
            if (_project == null)
            {
                return new List<string>();
            }

            IList<ProjectItem> items = _project.AddItem("___TEMPORARY___", fileSpec);
            _project.RemoveItems(items);
            List<string> files = new List<string>();

            foreach (ProjectItem item in items)
            {
                files.Add(item.EvaluatedInclude);
            }

            return files;
        }

        private void MarkReload(object sender, FileSystemEventArgs e)
        {
            _needsReload = true;
        }

        private void ReloadIfNecessary()
        {
            if (_needsReload && _project != null)
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

            foreach (FileSystemWatcher watcher in _watchers)
            {
                watcher.Changed -= MarkReload;
                watcher.Deleted -= MarkReload;
                watcher.Renamed -= MarkReload;
                watcher.Dispose();
            }

            _watchers.Clear();

            if (_project != null)
            {
                foreach (ResolvedImport import in _project.Imports)
                {
                    _containedFiles.Add(import.ImportedProject.FullPath);
                }

                _containedFiles.Add(_project.FullPath);

                foreach (string path in _containedFiles)
                {
                    FileSystemWatcher watcher = new FileSystemWatcher
                    {
                        Path = Path.GetDirectoryName(path),
                        Filter = Path.GetFileName(path),
                        NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                        EnableRaisingEvents = true
                    };

                    watcher.Changed += MarkReload;
                    watcher.Deleted += MarkReload;
                    watcher.Renamed += MarkReload;
                    _watchers.Add(watcher);
                }
            }
        }
    }
}
