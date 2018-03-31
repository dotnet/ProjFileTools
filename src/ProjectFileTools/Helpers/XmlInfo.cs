using System.Xml.Linq;

namespace ProjectFileTools.Helpers
{
    public class XmlInfo
    {
        public XmlInfo(string originalText, string elementText, int start, int end, int startQuote, int endQuote, bool isModified, int realEnd, string elementName, string attributeName)
        {
            OriginalText = originalText;
            ElementText = elementText;
            TagStart = start;
            TagEnd = end;
            AttributeQuoteStart = startQuote;
            AttributeQuoteEnd = endQuote;
            IsModified = isModified;
            EndInActualDocument = realEnd;
            TagName = elementName;
            AttributeName = attributeName;
        }

        public bool TryGetElement(out XElement element)
        {
            try
            {
                element = XElement.Parse(ElementText);
            }
            catch
            {
                element = null;
                return false;
            }

            return true;
        }

        public int TagStart { get; }

        public int TagEnd { get; }

        public int AttributeQuoteStart { get; }

        public int AttributeQuoteEnd { get; }

        public int EndInActualDocument { get; }

        public bool IsModified { get; }

        public string OriginalText { get; }

        public string ElementText { get; }

        public string TagName { get; }

        public string AttributeName { get; }

        public int AttributeValueStart => AttributeQuoteStart + 1;

        public int AttributeValueLength => AttributeQuoteEnd - AttributeQuoteStart - 1;

        public string AttributeValue => ElementText.Substring(AttributeValueStart - TagStart, AttributeValueLength);

        public int RealDocumentLength => EndInActualDocument - TagStart + 1;

        public void Flatten(out string documentText, out int start, out int end, out int startQuote, out int endQuote, out int realEnd, out bool isHealingRequired, out string healedXml)
        {
            documentText = OriginalText;
            start = TagStart;
            end = TagEnd;
            startQuote = AttributeQuoteStart;
            endQuote = AttributeQuoteEnd;
            realEnd = EndInActualDocument;
            isHealingRequired = IsModified;
            healedXml = ElementText;
        }
    }
}
