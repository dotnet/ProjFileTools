using Microsoft.VisualStudio.Text;

namespace ProjectFileTools.Helpers
{
    internal static class XmlTools
    {
        public static XmlInfo GetXmlInfo(ITextSnapshot snapshot, int pos)
        {
            if (pos < 1)
            {
                return null;
            }

            string documentText = snapshot.GetText();
            return GetXmlInfo(documentText, pos);
        }

        public static XmlInfo GetXmlInfo(string documentText, int pos)
        {
            if (pos < 1)
            {
                return null;
            }

            int start = pos < documentText.Length ? documentText.LastIndexOf('<', pos) : documentText.LastIndexOf('<');
            int end = pos < documentText.Length ? documentText.IndexOf('>', pos) : -1;
            int startQuote = pos <= documentText.Length ? documentText.LastIndexOf('"', pos - 1) : -1;
            int endQuote = pos < documentText.Length ? documentText.IndexOf('"', pos) : -1;
            int realEnd = end;
            bool isHealed = false;

            if (startQuote > -1 && startQuote < start || end > -1 && endQuote > end)
            {
                //We could be inside of an element here
                int closeTag = (pos < documentText.Length ? documentText.LastIndexOf('>', pos) : documentText.LastIndexOf('>'));
                bool isInsideTag = closeTag > start;

                if (isInsideTag && closeTag > 1)
                {
                    if (documentText[closeTag - 1] == '/')
                    {
                        return null;
                    }

                    int nextOpen = documentText.IndexOf('<', closeTag);

                    if (nextOpen < 0)
                    {
                        nextOpen = documentText.Length;
                    }

                    int elementNameEnd = documentText.IndexOf(' ', start);

                    if (elementNameEnd < 0 || elementNameEnd > closeTag)
                    {
                        elementNameEnd = closeTag;
                    }

                    string elementName = documentText.Substring(start + 1, elementNameEnd - start - 1);
                    string text = documentText.Substring(closeTag + 1, nextOpen - closeTag - 1);
                    return new XmlInfo(text, text, closeTag + 1, nextOpen - 1, closeTag, nextOpen, false, nextOpen - 1, elementName, null);
                }

                return null;
            }

            string fragmentText = null;
            //If we managed to find a close...
            if (end > start)
            {
                int nextStart = start < documentText.Length - 1 ? documentText.IndexOf('<', start + 1) : -1;

                //If we found another start before the close...
                //      <PackageReference Include="
                //  </ItemGroup>
                if (nextStart > -1 && nextStart < end)
                {
                    //Heal
                    fragmentText = documentText.Substring(start, nextStart - start).Trim();
                    realEnd = fragmentText.Length + start - 1;

                    switch (fragmentText[fragmentText.Length - 1])
                    {
                        case '\"':
                            if (endQuote > nextStart || endQuote < 0)
                            {
                                endQuote = fragmentText.Length + start;
                                fragmentText += "\"";
                            }
                            break;
                        case '>':
                            return null;
                        default:
                            //If there's a start quote in play, we're just looking at an unclosed attribute value
                            if (startQuote > start)
                            {
                                endQuote = fragmentText.Length + start;
                                fragmentText += "\"";
                            }
                            else
                            {
                                return null;
                            }
                            break;
                    }

                    end = fragmentText.Length + 2 + start;
                    fragmentText += " />";
                    isHealed = true;
                }
                //We didn't find that, so we're "good"... we might have a non-self closed tag though
                else
                {
                    isHealed = false;

                    //Unless of course the closing quote was after the end...
                    if (endQuote < 0 || endQuote > end)
                    {
                        fragmentText = documentText.Substring(start, end - start);

                        if (fragmentText.EndsWith("/"))
                        {
                            fragmentText = fragmentText.Substring(0, fragmentText.Length - 1);
                        }

                        fragmentText = fragmentText.TrimEnd() + "\" />";

                        endQuote = fragmentText.Length - 4 + start;
                        end = fragmentText.Length - 1 + start;
                    }
                    else
                    {
                        fragmentText = documentText.Substring(start, end - start + 1);
                    }
                }
            }
            //If we didn't find a close, if we found an end quote, that's good enough
            else if (endQuote > start)
            {
                realEnd = endQuote;
                fragmentText = documentText.Substring(start, endQuote - start + 1) + " />";
                end = fragmentText.Length - 1 + start;
                isHealed = true;
            }
            //If we didn't find an end quote even, if we found an open quote, that might be good enough
            else if (startQuote > start)
            {
                //If we find a close before the cursor that's after the start quote, we're outside of the element
                if (pos > documentText.Length || documentText.LastIndexOf('>', pos - 1) > startQuote)
                {
                    return null;
                }

                //Otherwise, we're presumably just after the start quote (and maybe some text) at the end of the document
                //  we already know there's no closing quote or end, see if there's another start we can run up to
                int nextStart = pos < documentText.Length ? documentText.IndexOf('<', pos) : -1;

                //If there isn't, run off to the end of the document
                if (nextStart < 0)
                {
                    fragmentText = documentText.Substring(start, documentText.Length - start).TrimEnd();
                }
                else
                {
                    fragmentText = documentText.Substring(start, nextStart - start + 1);
                    fragmentText = fragmentText.Trim();
                }

                realEnd = start + fragmentText.Length - 1;
                fragmentText += "\" />";
                endQuote = fragmentText.Length - 4 + start;
                end = fragmentText.Length - 1 + start;
                isHealed = true;
            }
            //All we've got to go on is the start, that's not good enough
            else
            {
                return null;
            }

            string healedXml = fragmentText;

            int indexAfterTagNameEnd = healedXml.FindFirstWhitespaceAtOrAfter(1);
            string tagName = healedXml.Substring(1, indexAfterTagNameEnd - 1);
            int equalsIndex = startQuote >= start && (startQuote - start) < healedXml.Length ? healedXml.LastIndexOf('=', startQuote - start) - 1 : -1;
            int afterAttributeIndex = healedXml.FindFirstNonWhitespaceAtOrBefore(equalsIndex, false);
            int beforeAttributeIndex = healedXml.FindFirstNonWhitespaceAtOrBefore(afterAttributeIndex, true);
            string attributeName = healedXml.Substring(beforeAttributeIndex + 1, afterAttributeIndex - beforeAttributeIndex);
            string orignalText = documentText.Substring(start, realEnd - start + 1);

            return new XmlInfo(orignalText, healedXml, start, end, startQuote, endQuote, isHealed, realEnd, tagName, attributeName);
        }

        private static int FindFirstWhitespaceAtOrAfter(this string text, int startAt)
        {
            for (int i = startAt; i < text.Length; ++i)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindFirstNonWhitespaceAtOrBefore(this string text, int startAt, bool invert)
        {
            for (int i = startAt; i > -1; --i)
            {
                if (invert ^ !char.IsWhiteSpace(text[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
