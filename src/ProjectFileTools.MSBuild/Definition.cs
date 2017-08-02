namespace ProjectFileTools.MSBuild
{
    /// <summary>
    /// Contains the information for the FindAllReferences table and GoToDefinition command for a single MSBuild item.
    /// </summary>
    public class Definition
    {
        /// <summary>
        /// File that the definition is associated with
        /// </summary>
        public string File { get; }

        /// <summary>
        /// Project containing the definition
        /// </summary>
        public string Project { get; }

        /// <summary>
        /// Type of definition
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Text displayed for this definition in the find all references window
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Line associated with this definition
        /// </summary>
        public int? Line { get; }

        /// <summary>
        /// Column associated with this definition
        /// </summary>
        public int? Col { get; }

        internal Definition(string file, string project, string type, string text)
        {
            File = file;
            Project = project;
            Type = type;
            Text = text;
            Line = null;
            Col = null;
        }

        internal Definition(string file, string project, string type, string text, int line, int col)
        {
            File = file;
            Project = project;
            Type = type;
            Text = text;
            Line = line;
            Col = col;
        }
    }
}
