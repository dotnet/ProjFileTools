using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Requires.Range(position > -1 && position < text.Length, nameof(position), "Position must be positive and less than text.Length");

            while (position > 0 && text[position - 1] != '\n')
            {
                position--;
            }

            return position;
        }
    }
}
