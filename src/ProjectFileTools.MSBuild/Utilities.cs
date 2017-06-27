using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectFileTools.MSBuild
{
    internal static class Utilities
    {
        internal static int GetLine(string text, int position)
        {
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
            while (position > 0 && text[position - 1] != '\n')
            {
                position--;
            }
            return position;
        }
    }
}
