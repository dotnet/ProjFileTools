using Microsoft;

namespace ProjectFileTools.MSBuild
{
    internal static class Utilities
    {
        internal static int GetLine(string text, int position)
        {
            Requires.NotNullOrEmpty(text, nameof(text));
            Requires.Range(position > -1 && position < text.Length, nameof(position), "Position must be positive and less than text.Length");

            int line = 0;
            for (int ind = 0; ind < position; ind++)
            {
                if (text[ind] == '\n')
                {
                    line++;
                }
            }

            return line;
        }

        internal static int GetStartOfLine(string text, int position)
        {
            Requires.NotNullOrEmpty(text, nameof(text));
            Requires.Range(position > -1 && position <= text.Length, nameof(position), "Position must be positive and less than or equal to text.Length");

            while (position > 0 && text[position - 1] != '\n')
            {
                position--;
            }

            return position;
        }

        internal static bool IsProperty(string text, int position, out string propertyName)
        {
            Requires.NotNull(text, nameof(text));
            Requires.Range(position > -1 && position < text.Length, nameof(position), "Position must be positive and less than text.Length");

            propertyName = null;
            if (text[position] == ')' && position > 1)
            {
                position--;
            }

            int propStart = position;
            int propEnd = position + 1;

            while (text[propStart] != '(' && propStart > 1)
            {
                if (!char.IsLetterOrDigit(text[propStart]))
                {
                    return false;
                }

                propStart--;
            }

            if (!(text[propStart] == '(' && text[propStart - 1] == '$'))
            {
                return false;
            }

            while (propEnd < text.Length - 1 && text[propEnd] != '.' && text[propEnd] != ')')
            {
                if (!char.IsLetterOrDigit(text[propEnd]))
                {
                    return false;
                }

                propEnd++;
            }
            if (!(text[propEnd] == '.' || text[propEnd] == ')'))
            {
                return false;
            }

            propertyName = text.Substring(propStart + 1, propEnd - propStart - 1);
            return true;
        }
    }
}
